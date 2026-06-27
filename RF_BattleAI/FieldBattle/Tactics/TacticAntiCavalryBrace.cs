using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticAntiCavalryBrace : TacticComponent
{
    private enum AntiCavalryState
    {
        Brace,
        Hold,
        Counter
    }

    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private AntiCavalryState? _lastState;
    private bool _hasAppliedInfantryFilter;

    public TacticAntiCavalryBrace(Team team)
        : base(team)
    {
    }

    protected override void ManageFormationCounts()
    {
        ManageFormationCounts(2, 1, 2, 1);

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

        ApplySpearFrontFilter();

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
            _hasAppliedInfantryFilter = false;
            IsTacticReapplyNeeded = true;
            return true;
        }

        if (_mainInfantry == null || _mainInfantry.CountOfUnits <= 0 || !_mainInfantry.QuerySystem.IsInfantryFormation)
        {
            _hasAppliedInfantryFilter = false;
            return true;
        }

        if (_supportInfantry != null && (_supportInfantry.CountOfUnits <= 0 || !_supportInfantry.QuerySystem.IsInfantryFormation))
        {
            _hasAppliedInfantryFilter = false;
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

        AntiCavalryState state = EvaluateState();
        if (_lastState != state)
        {
            float distance = (float)System.Math.Sqrt(GetEngagementDistanceSquared());
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticAntiCavalryBrace), state.ToString(), $"distance={distance:F1} power={base.Team.QuerySystem.RemainingPowerRatio:F2} enemyMounted={base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio:F2}");
            _lastState = state;
        }

        switch (state)
        {
            case AntiCavalryState.Brace:
                ApplyBrace();
                break;
            case AntiCavalryState.Hold:
                ApplyHold();
                break;
            default:
                ApplyCounter();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        float enemyMountedRatio = base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio;
        float score = 0.16f;

        if (base.Team.QuerySystem.InfantryRatio > 0.35f)
        {
            score += 0.22f;
        }

        if (enemyMountedRatio > 0.22f)
        {
            score += 0.34f;
        }

        if (base.Team.QuerySystem.RangedRatio > 0.08f)
        {
            score += 0.08f;
        }

        if (base.Team.Side == BattleSideEnum.Defender)
        {
            score += 0.16f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticAntiCavalryBrace), score);
    }

    private void ApplySpearFrontFilter()
    {
        if (_hasAppliedInfantryFilter || _mainInfantry == null || _supportInfantry == null)
        {
            return;
        }

        int totalInfantry = _mainInfantry.CountOfUnits + _supportInfantry.CountOfUnits;
        if (totalInfantry < 20)
        {
            return;
        }

        int spearFrontCount = MBMath.ClampInt((int)(totalInfantry * 0.5f), 1, totalInfantry - 1);
        int supportCount = totalInfantry - spearFrontCount;

        List<(Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents)> transferData = new()
        {
            (_mainInfantry, spearFrontCount, TroopTraitsMask.Melee | TroopTraitsMask.Spear | TroopTraitsMask.Shield, new List<Agent>()),
            (_supportInfantry, supportCount, TroopTraitsMask.Melee | TroopTraitsMask.Shield, new List<Agent>())
        };

        base.Team.RearrangeFormationsAccordingToFilter(transferData);
        _hasAppliedInfantryFilter = true;
        BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticAntiCavalryBrace), "SpearFrontAssigned", $"front={spearFrontCount} support={supportCount}");
    }

    private AntiCavalryState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;

        if (distanceSquared > 3025f)
        {
            return AntiCavalryState.Brace;
        }

        if (distanceSquared > 1024f || powerRatio < 1f)
        {
            return AntiCavalryState.Hold;
        }

        return AntiCavalryState.Counter;
    }

    private float GetEngagementDistanceSquared()
    {
        Formation? anchor = _mainInfantry ?? _supportInfantry ?? _archers ?? _leftCavalry ?? _rightCavalry ?? _rangedCavalry;
        if (anchor?.CachedClosestEnemyFormation == null)
        {
            return float.MaxValue;
        }

        return anchor.CachedMedianPosition.AsVec2.DistanceSquared(anchor.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
    }

    private WorldPosition GetDefensivePosition(Formation formation)
    {
        Vec2 enemyPosition = formation.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Team.QuerySystem.AverageEnemyPosition;
        return BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            formation,
            formation.CachedMedianPosition.AsVec2,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 16f);
    }

    private void ApplyBrace()
    {
        ApplyMainInfantry(defendWeight: 1.65f, advanceWeight: 0.15f, chargeWeight: 0f);
        ApplySupportInfantry(defendWeight: 1f, reserveWeight: 1.35f, chargeWeight: 0f);
        ApplyArchers(screenedWeight: 1f, skirmishWeight: 0.45f);
        ApplyCavalry(protectWeight: 1.15f, flankWeight: 0.25f, chargeWeight: 0.15f);
    }

    private void ApplyHold()
    {
        ApplyMainInfantry(defendWeight: 1.25f, advanceWeight: 0.45f, chargeWeight: 0f);
        ApplySupportInfantry(defendWeight: 0.75f, reserveWeight: 1.15f, chargeWeight: 0f);
        ApplyArchers(screenedWeight: 1f, skirmishWeight: 0.75f);
        ApplyCavalry(protectWeight: 1f, flankWeight: 0.45f, chargeWeight: 0.25f);
    }

    private void ApplyCounter()
    {
        ApplyMainInfantry(defendWeight: 0.35f, advanceWeight: 1f, chargeWeight: 1f);
        ApplySupportInfantry(defendWeight: 0f, reserveWeight: 0f, chargeWeight: 0.95f);
        ApplyArchers(screenedWeight: 0.9f, skirmishWeight: 1f);
        ApplyCavalry(protectWeight: 0.55f, flankWeight: 0.85f, chargeWeight: 0.7f);
    }

    private void ApplyMainInfantry(float defendWeight, float advanceWeight, float chargeWeight)
    {
        if (_mainInfantry == null)
        {
            return;
        }

        _mainInfantry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(_mainInfantry);
        _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(advanceWeight);
        if (defendWeight > 0f)
        {
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(defendWeight).DefensePosition = GetDefensivePosition(_mainInfantry);
        }

        if (chargeWeight > 0f)
        {
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(chargeWeight);
        }
    }

    private void ApplySupportInfantry(float defendWeight, float reserveWeight, float chargeWeight)
    {
        if (_supportInfantry == null)
        {
            return;
        }

        _supportInfantry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(_supportInfantry);
        if (defendWeight > 0f)
        {
            _supportInfantry.AI.SetBehaviorWeight<BehaviorDefend>(defendWeight).DefensePosition = GetDefensivePosition(_supportInfantry);
        }

        if (_mainInfantry != null && reserveWeight > 0f)
        {
            _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(reserveWeight).AnchorFormation = _mainInfantry;
        }

        if (chargeWeight > 0f)
        {
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(chargeWeight);
        }
    }

    private void ApplyArchers(float screenedWeight, float skirmishWeight)
    {
        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(screenedWeight);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(skirmishWeight);
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(0.9f);
        }
    }

    private void ApplyCavalry(float protectWeight, float flankWeight, float chargeWeight)
    {
        ApplyCavalryWing(_leftCavalry, FormationAI.BehaviorSide.Left, protectWeight, flankWeight, chargeWeight);
        ApplyCavalryWing(_rightCavalry, FormationAI.BehaviorSide.Right, protectWeight, flankWeight, chargeWeight);
    }

    private static void ApplyCavalryWing(Formation? cavalry, FormationAI.BehaviorSide side, float protectWeight, float flankWeight, float chargeWeight)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        TacticComponent.SetDefaultBehaviorWeights(cavalry);
        cavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(protectWeight).FlankSide = side;
        cavalry.AI.SetBehaviorWeight<BehaviorFlank>(flankWeight);
        cavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(chargeWeight);
    }
}
