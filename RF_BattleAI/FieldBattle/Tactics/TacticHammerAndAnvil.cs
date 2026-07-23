using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticHammerAndAnvil : TacticComponent
{
    private enum HammerBattleState
    {
        SetAnvil,
        CavalryReposition,
        FixEnemy,
        SwingFlanks
    }

    private const float SetAnvilDistanceSquared = 32400f;
    private const float HammerCommitDistanceSquared = 12100f;
    private const float ManualReleaseArcherPressureDistanceSquared = 32400f;
    private const float CavalryOuterArcLateralDistance = 86f;
    private const float CavalryOuterArcRearOffset = 22f;
    private const float CavalryWingForwardOffset = 48f;
    private const float CavalryWingLateralDistance = 102f;
    private const float CavalryAssemblyLateralDistance = 96f;
    private const float CavalryAssemblyRearOffset = 28f;
    private const float CavalryAssemblyRadius = 40f;
    private const float CavalryGoodEnoughRadius = 68f;
    private const float CavalryBreakthroughForwardDistance = 58f;
    private const float CavalryBreakthroughLateralClearance = 24f;

    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private HammerBattleState? _lastState;
    private readonly HysteresisGate _commitPowerGate = HysteresisGate.RisesAbove(0.95f);
    private readonly HysteresisGate _weakPowerGate = HysteresisGate.FallsBelow(0.8f);
    private bool _deploymentPhaseFinished;

    public TacticHammerAndAnvil(Team team)
        : base(team)
    {
    }

    protected override void ManageFormationCounts()
    {
        ManageFormationCounts(1, 1, 2, 1);

        List<Formation> infantryFormations = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower);

        _mainInfantry = infantryFormations.FirstOrDefault();
        _supportInfantry = infantryFormations.Skip(1).FirstOrDefault();
        _archers = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower).FirstOrDefault();

        List<Formation> cavalryFormations = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsCavalryFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower);

        _leftCavalry = cavalryFormations.Count > 0 ? cavalryFormations[0] : null;
        _rightCavalry = cavalryFormations.Count > 1 ? cavalryFormations[1] : null;
        _rangedCavalry = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsRangedCavalryFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower).FirstOrDefault();

        if (_mainInfantry != null)
        {
            _mainInfantry.AI.IsMainFormation = true;
            _mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
        }

        if (_leftCavalry != null)
        {
            _leftCavalry.AI.Side = FormationAI.BehaviorSide.Left;
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.Side = FormationAI.BehaviorSide.Right;
        }
    }

    protected override bool CheckAndSetAvailableFormationsChanged()
    {
        int currentAiFormationCount = base.Team.GetAIControlledFormationCount();
        if (currentAiFormationCount != _cachedAiControlledFormationCount)
        {
            _cachedAiControlledFormationCount = currentAiFormationCount;
            IsTacticReapplyNeeded = true;
            return true;
        }

        if (_mainInfantry == null || _mainInfantry.CountOfUnits <= 0 || !_mainInfantry.QuerySystem.IsInfantryFormation)
        {
            return true;
        }

        if ((_leftCavalry == null || _leftCavalry.CountOfUnits <= 0) && (_rightCavalry == null || _rightCavalry.CountOfUnits <= 0))
        {
            return true;
        }

        return false;
    }

    public override void TickOccasionally()
    {
        if (!AreFormationsCreated)
        {
            return;
        }

        if (CheckAndSetAvailableFormationsChanged() || IsTacticReapplyNeeded)
        {
            ManageFormationCounts();
            IsTacticReapplyNeeded = false;
        }

        HammerBattleState state = EvaluateState();
        if (_lastState != state)
        {
            float distance = (float)Math.Sqrt(GetEngagementDistanceSquared());
            float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;
            float cavalryRatio = base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio;
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticHammerAndAnvil), state.ToString(), $"distance={distance:F1} power={powerRatio:F2} cavalry={cavalryRatio:F2}");
            _lastState = state;
        }

        switch (state)
        {
            case HammerBattleState.SetAnvil:
                ApplySetAnvil();
                break;
            case HammerBattleState.FixEnemy:
                ApplyFixEnemy();
                break;
            case HammerBattleState.CavalryReposition:
                ApplyCavalryReposition();
                break;
            default:
                ApplySwingFlanks();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(base.Team);
        float cavalryRatio = composition.MountedRatio;
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;
        float score = 0.2f;

        if (composition.InfantryRatio > 0.28f)
        {
            score += 0.2f;
        }

        if (cavalryRatio > 0.12f)
        {
            score += 0.25f;
        }

        if (cavalryRatio > 0.2f)
        {
            score += 0.12f;
        }

        if (composition.RangedRatio > 0.1f)
        {
            score += 0.1f;
        }

        if (powerRatio >= 0.9f)
        {
            score += 0.08f;
        }

        if (base.Team.Side == BattleSideEnum.Attacker)
        {
            score += 0.08f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticHammerAndAnvil), score);
    }

    private HammerBattleState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;
        bool hammerReady = IsHammerReadyToStrike();
        bool anyCavalryReady = IsAnyCavalryReady();
        bool manualHammerOverride = BattleAITacticController.HasManualDoctrineOverride(base.Team, nameof(TacticHammerAndAnvil));

        // Full disengagement (~1.5x the anvil distance, ≈220m): reopen the
        // deployment gate so the anvil can re-anchor instead of the one-way
        // flag keeping SetAnvil unreachable for the rest of the battle.
        if (_deploymentPhaseFinished && distanceSquared > SetAnvilDistanceSquared * 1.5f)
        {
            _deploymentPhaseFinished = false;
        }

        if (!_deploymentPhaseFinished)
        {
            if (distanceSquared > SetAnvilDistanceSquared)
            {
                return HammerBattleState.SetAnvil;
            }

            _deploymentPhaseFinished = true;
        }

        // The hammer only swings once the anvil has actually PINNED the enemy
        // (melee contact range) — releasing at 90m let the enemy face the
        // cavalry freely. BUT the wait is scaled by the ENEMY's cavalry share:
        // against a horse-heavy foe, staged wings waiting for contact are
        // sitting ducks for the enemy horse, so the release comes early enough
        // to meet them (cav-heavy ≥35%: 90m; mixed ≥15%: 50m; infantry foe: 25m).
        float enemyCavalryRatio = base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio;
        float releaseDistanceSquared = enemyCavalryRatio >= 0.35f ? 8100f : (enemyCavalryRatio >= 0.15f ? 2500f : 625f);
        float readyShortcutSquared = enemyCavalryRatio >= 0.35f ? 19600f : (enemyCavalryRatio >= 0.15f ? 10000f : 1600f);
        if (distanceSquared <= releaseDistanceSquared)
        {
            return HammerBattleState.SwingFlanks;
        }

        if (manualHammerOverride && ShouldForceManualRelease(distanceSquared))
        {
            return HammerBattleState.SwingFlanks;
        }

        if (anyCavalryReady && distanceSquared <= readyShortcutSquared)
        {
            return HammerBattleState.SwingFlanks;
        }

        if (!hammerReady)
        {
            return HammerBattleState.CavalryReposition;
        }

        if (_commitPowerGate.Evaluate(powerRatio) && distanceSquared <= HammerCommitDistanceSquared)
        {
            return HammerBattleState.SwingFlanks;
        }

        if (_weakPowerGate.Evaluate(powerRatio) && distanceSquared > 2500f)
        {
            return HammerBattleState.FixEnemy;
        }

        if (distanceSquared > 1600f)
        {
            return HammerBattleState.FixEnemy;
        }

        return HammerBattleState.SwingFlanks;
    }

    private float GetEngagementDistanceSquared()
    {
        float bestDistanceSquared = float.MaxValue;
        UpdateBestDistanceSquared(_mainInfantry, ref bestDistanceSquared);
        UpdateBestDistanceSquared(_supportInfantry, ref bestDistanceSquared);
        UpdateBestDistanceSquared(_archers, ref bestDistanceSquared);

        if (bestDistanceSquared < float.MaxValue)
        {
            return bestDistanceSquared;
        }

        UpdateBestDistanceSquared(_leftCavalry, ref bestDistanceSquared);
        UpdateBestDistanceSquared(_rightCavalry, ref bestDistanceSquared);
        UpdateBestDistanceSquared(_rangedCavalry, ref bestDistanceSquared);
        return bestDistanceSquared;
    }

    private void ApplySetAnvil()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.72f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.7f);
        }

        ApplyRangedSupport(screenedSkirmishWeight: 1.3f, skirmishWeight: 0.25f);
        ApplyCavalryAssembly();
    }

    private void ApplyFixEnemy()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.82f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.78f);
        }

        ApplyRangedSupport(screenedSkirmishWeight: 1.25f, skirmishWeight: 0.35f);
        ApplyCavalryAssembly();
    }

    private void ApplySwingFlanks()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.15f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.1f);
        }

        ApplyRangedSupport(screenedSkirmishWeight: 0.95f, skirmishWeight: 0.35f);
        ApplyCavalryStrike();
    }

    private void ApplyCavalryReposition()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.8f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.76f);
        }

        ApplyRangedSupport(screenedSkirmishWeight: 1.25f, skirmishWeight: 0.25f);
        ApplyCavalryAssembly();
    }

    private void ApplyRangedSupport(float screenedSkirmishWeight, float skirmishWeight)
    {
        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(screenedSkirmishWeight);
            if (skirmishWeight > 0f)
            {
                _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(skirmishWeight);
            }
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorCharge>(0f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(0f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.15f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1.35f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1.45f);
        }
    }

    private void ApplyCavalryStrike()
    {
        ApplyCavalryStrike(_leftCavalry, FormationAI.BehaviorSide.Left);
        ApplyCavalryStrike(_rightCavalry, FormationAI.BehaviorSide.Right);
    }

    private void ApplyCavalryAssembly()
    {
        ApplyCavalryAssembly(_leftCavalry, FormationAI.BehaviorSide.Left);
        ApplyCavalryAssembly(_rightCavalry, FormationAI.BehaviorSide.Right);
    }

    private void ApplyCavalryAssembly(Formation? cavalry, FormationAI.BehaviorSide side)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(cavalry);
        BehaviorCavalryReposition reposition = cavalry.AI.SetBehaviorWeight<BehaviorCavalryReposition>(2f);
        reposition.AnchorFormation = _mainInfantry ?? _supportInfantry ?? cavalry;
        reposition.FlankSide = side;
        reposition.OuterArcLateralDistance = CavalryOuterArcLateralDistance;
        reposition.OuterArcRearOffset = CavalryOuterArcRearOffset;
        if (base.Team.Side == BattleSideEnum.Attacker)
        {
            reposition.ForwardOffset = CavalryWingForwardOffset;
            reposition.LateralDistance = CavalryWingLateralDistance;
            reposition.RearOffset = 0f;
        }
        else
        {
            reposition.ForwardOffset = 0f;
            reposition.LateralDistance = CavalryAssemblyLateralDistance;
            reposition.RearOffset = CavalryAssemblyRearOffset;
        }
    }

    private void ApplyCavalryStrike(Formation? cavalry, FormationAI.BehaviorSide side)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(cavalry);

        Formation? anchor = _mainInfantry ?? _supportInfantry ?? cavalry;
        Formation? enemyFormation = anchor.CachedClosestEnemyFormation?.Formation ?? cavalry.CachedClosestEnemyFormation?.Formation;
        bool useBreakthrough = ShouldUseBreakthrough(cavalry, side, enemyFormation);

        cavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(0.05f).FlankSide = side;
        cavalry.AI.SetBehaviorWeight<BehaviorFlank>(useBreakthrough ? 1.05f : 1.9f);
        cavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(useBreakthrough ? 1.2f : 1.75f);

        if (useBreakthrough && enemyFormation != null)
        {
            BehaviorCavalryBreakthrough breakthrough = cavalry.AI.SetBehaviorWeight<BehaviorCavalryBreakthrough>(2.05f);
            breakthrough.AnchorFormation = anchor;
            breakthrough.TargetFormation = enemyFormation;
            breakthrough.FlankSide = side;
            breakthrough.ForwardDistance = CavalryBreakthroughForwardDistance;
            breakthrough.LateralClearance = CavalryBreakthroughLateralClearance;
        }
    }

    private bool ShouldUseBreakthrough(Formation cavalry, FormationAI.BehaviorSide side, Formation? enemyFormation)
    {
        if (enemyFormation == null || enemyFormation.CountOfUnits <= 0)
        {
            return false;
        }

        if (!IsCavalryGoodEnough(cavalry, side))
        {
            return false;
        }

        return enemyFormation.QuerySystem.IsInfantryFormation || enemyFormation.QuerySystem.IsRangedFormation;
    }

    private bool IsHammerReadyToStrike()
    {
        int cavalryCount = 0;
        int readyCount = 0;

        if (_leftCavalry != null && _leftCavalry.CountOfUnits > 0)
        {
            cavalryCount++;
            if (IsCavalryReady(_leftCavalry, FormationAI.BehaviorSide.Left))
            {
                readyCount++;
            }
        }

        if (_rightCavalry != null && _rightCavalry.CountOfUnits > 0)
        {
            cavalryCount++;
            if (IsCavalryReady(_rightCavalry, FormationAI.BehaviorSide.Right))
            {
                readyCount++;
            }
        }

        return cavalryCount > 0 && readyCount == cavalryCount;
    }

    private bool IsAnyCavalryReady()
    {
        if (_leftCavalry != null && _leftCavalry.CountOfUnits > 0 && IsCavalryReady(_leftCavalry, FormationAI.BehaviorSide.Left))
        {
            return true;
        }

        return _rightCavalry != null
            && _rightCavalry.CountOfUnits > 0
            && IsCavalryReady(_rightCavalry, FormationAI.BehaviorSide.Right);
    }

    private bool IsCavalryReady(Formation cavalry, FormationAI.BehaviorSide side)
    {
        Formation? anchor = _mainInfantry ?? _supportInfantry ?? cavalry;
        Formation? enemyFormation = anchor?.CachedClosestEnemyFormation?.Formation ?? cavalry.CachedClosestEnemyFormation?.Formation;
        if (anchor == null || enemyFormation == null)
        {
            return false;
        }

        Vec2 targetPoint = GetCavalryStagePoint(anchor, enemyFormation, side);
        return cavalry.CachedMedianPosition.AsVec2.DistanceSquared(targetPoint) <= CavalryAssemblyRadius * CavalryAssemblyRadius;
    }

    private bool ShouldForceManualRelease(float distanceSquared)
    {
        if (!IsAnyCavalryGoodEnoughForManualRelease())
        {
            return false;
        }

        // Manual doctrine means "fight THIS way", not "skip the pin". The old
        // unconditional 160m release fired long before the anvil arrived in
        // every manual battle, making the composition ladder below dead code.
        // Manual now follows the same enemy-cavalry-scaled ladder as the auto
        // gate, ~30% looser to honor the player's intent (117m / 65m / 32m).
        float enemyCavalryRatio = base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio;
        float manualReleaseDistanceSquared = enemyCavalryRatio >= 0.35f ? 13689f : (enemyCavalryRatio >= 0.15f ? 4225f : 1024f);
        if (distanceSquared <= manualReleaseDistanceSquared)
        {
            return true;
        }

        // Survival valve: wings bleeding under MASSED archery are released
        // rather than left parked on the arc. Gated on the enemy actually
        // being ranged-heavy — a 20% archer contingent must not trigger a
        // full early release.
        if (base.Team.QuerySystem.EnemyRangedRatio >= 0.35f
            && distanceSquared <= ManualReleaseArcherPressureDistanceSquared
            && IsCavalryUnderRangedPressure())
        {
            return true;
        }

        return false;
    }

    private bool IsAnyCavalryGoodEnoughForManualRelease()
    {
        if (_leftCavalry != null && _leftCavalry.CountOfUnits > 0 && IsCavalryGoodEnough(_leftCavalry, FormationAI.BehaviorSide.Left))
        {
            return true;
        }

        return _rightCavalry != null
            && _rightCavalry.CountOfUnits > 0
            && IsCavalryGoodEnough(_rightCavalry, FormationAI.BehaviorSide.Right);
    }

    private bool IsCavalryGoodEnough(Formation cavalry, FormationAI.BehaviorSide side)
    {
        Formation? anchor = _mainInfantry ?? _supportInfantry ?? cavalry;
        Formation? enemyFormation = anchor?.CachedClosestEnemyFormation?.Formation ?? cavalry.CachedClosestEnemyFormation?.Formation;
        if (anchor == null || enemyFormation == null)
        {
            return false;
        }

        Vec2 targetPoint = GetCavalryStagePoint(anchor, enemyFormation, side);
        float distanceSquared = cavalry.CachedMedianPosition.AsVec2.DistanceSquared(targetPoint);
        if (distanceSquared <= CavalryGoodEnoughRadius * CavalryGoodEnoughRadius)
        {
            return true;
        }

        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = enemyFormation.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - anchorPosition).Normalized() : anchor.Direction;
        if (!directionToEnemy.IsValid)
        {
            return false;
        }

        Vec2 lateralDirection = side == FormationAI.BehaviorSide.Left
            ? new Vec2(-directionToEnemy.y, directionToEnemy.x)
            : new Vec2(directionToEnemy.y, -directionToEnemy.x);
        float lateralDistance = MathF.Abs(Vec2.DotProduct(cavalry.CachedMedianPosition.AsVec2 - anchorPosition, lateralDirection));
        return lateralDistance >= GetRequiredLateralDistance() * 0.72f && distanceSquared <= (CavalryGoodEnoughRadius + 18f) * (CavalryGoodEnoughRadius + 18f);
    }

    private bool IsCavalryUnderRangedPressure()
    {
        return IsFormationUnderRangedPressure(_leftCavalry)
            || IsFormationUnderRangedPressure(_rightCavalry);
    }

    private static bool IsFormationUnderRangedPressure(Formation? formation)
    {
        return formation != null
            && formation.CountOfUnits > 0
            && (formation.QuerySystem.IsUnderRangedAttack || formation.QuerySystem.UnderRangedAttackRatio > 0.12f);
    }

    private Vec2 GetCavalryStagePoint(Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide side)
    {
        if (base.Team.Side == BattleSideEnum.Attacker)
        {
            return BehaviorCavalryReposition.ComputeWingPoint(anchor, enemyFormation, side, CavalryWingForwardOffset, CavalryWingLateralDistance);
        }

        return BehaviorCavalryReposition.ComputeAssemblyPoint(anchor, enemyFormation, side, CavalryAssemblyLateralDistance, CavalryAssemblyRearOffset);
    }

    private float GetRequiredLateralDistance()
    {
        return base.Team.Side == BattleSideEnum.Attacker ? CavalryWingLateralDistance : CavalryAssemblyLateralDistance;
    }

    private static void UpdateBestDistanceSquared(Formation? formation, ref float bestDistanceSquared)
    {
        if (formation == null)
        {
            return;
        }

        Formation activeFormation = formation;
        if (activeFormation.CountOfUnits <= 0)
        {
            return;
        }

        Formation? enemyFormation = activeFormation.CachedClosestEnemyFormation?.Formation;
        if (enemyFormation == null)
        {
            return;
        }

        float distanceSquared = activeFormation.CachedMedianPosition.AsVec2.DistanceSquared(
            enemyFormation.CachedMedianPosition.AsVec2);
        if (distanceSquared < bestDistanceSquared)
        {
            bestDistanceSquared = distanceSquared;
        }
    }
}
