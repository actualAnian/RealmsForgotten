using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticShieldwallAdvance : TacticComponent
{
    private enum ShieldwallBattleState
    {
        EmergencyBrace,
        FormShieldwall,
        PressAdvance,
        FinalPush
    }

    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private ShieldwallBattleState? _lastState;
    private readonly HysteresisGate _pressPowerGate = HysteresisGate.RisesAbove(0.9f);

    public TacticShieldwallAdvance(Team team)
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

        if (_archers != null && (_archers.CountOfUnits <= 0 || !_archers.QuerySystem.IsRangedFormation))
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

        ShieldwallBattleState state = EvaluateState();
        if (_lastState != state)
        {
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticShieldwallAdvance), state.ToString());
            _lastState = state;
        }

        switch (state)
        {
            case ShieldwallBattleState.EmergencyBrace:
                ApplyEmergencyBrace();
                break;
            case ShieldwallBattleState.FormShieldwall:
                ApplyFormShieldwall();
                break;
            case ShieldwallBattleState.PressAdvance:
                ApplyPressAdvance();
                break;
            default:
                ApplyFinalPush();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        float score = 0.15f;

        if (base.Team.QuerySystem.InfantryRatio > 0.45f)
        {
            score += 0.35f;
        }

        if (base.Team.QuerySystem.RangedRatio > 0.12f)
        {
            score += 0.15f;
        }

        if (base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio < 0.25f)
        {
            score += 0.15f;
        }

        if (base.Team.Side == BattleSideEnum.Defender)
        {
            score += 0.1f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticShieldwallAdvance), score);
    }

    private ShieldwallBattleState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;
        if (ShouldEmergencyBrace(distanceSquared, powerRatio))
        {
            return ShieldwallBattleState.EmergencyBrace;
        }

        if (distanceSquared > 3600f)
        {
            return ShieldwallBattleState.FormShieldwall;
        }

        if (distanceSquared > 1225f && _pressPowerGate.Evaluate(powerRatio))
        {
            return ShieldwallBattleState.PressAdvance;
        }

        return ShieldwallBattleState.FinalPush;
    }

    private float GetEngagementDistanceSquared()
    {
        Formation? anchor = _mainInfantry ?? _archers ?? _supportInfantry ?? _leftCavalry ?? _rightCavalry ?? _rangedCavalry;
        if (anchor?.CachedClosestEnemyFormation == null)
        {
            return float.MaxValue;
        }

        return anchor.CachedMedianPosition.AsVec2.DistanceSquared(anchor.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
    }

    private bool ShouldEmergencyBrace(float distanceSquared, float powerRatio)
    {
        if (_mainInfantry == null)
        {
            return false;
        }

        float enemyMountedRatio = base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio;
        if (enemyMountedRatio < 0.22f)
        {
            return false;
        }

        Formation? nearestEnemyCavalry = FindNearestEnemyCavalry(_mainInfantry);
        if (nearestEnemyCavalry == null)
        {
            return false;
        }

        float cavalryDistance = nearestEnemyCavalry.CachedMedianPosition.AsVec2.Distance(_mainInfantry.CachedMedianPosition.AsVec2);
        float enemySpeed = MathF.Max(nearestEnemyCavalry.CachedMovementSpeed, nearestEnemyCavalry.QuerySystem.MovementSpeedMaximum);
        float ownSpeed = MathF.Max(_mainInfantry.QuerySystem.MovementSpeedMaximum, 0.1f);
        float enemyImpactTime = cavalryDistance / MathF.Max(enemySpeed, 0.1f);

        WorldPosition defensivePosition = GetDefensivePosition(_mainInfantry, searchRadius: 16f);
        float repositionDistance = _mainInfantry.CachedMedianPosition.AsVec2.Distance(defensivePosition.AsVec2);
        float repositionTime = repositionDistance / ownSpeed;

        float safetyMargin = powerRatio < 0.95f ? 2.75f : 1.75f;
        if (enemyMountedRatio > 0.35f)
        {
            safetyMargin += 0.75f;
        }

        return cavalryDistance <= 60f
            || (powerRatio < 1f && cavalryDistance <= 75f)
            || (distanceSquared <= 3600f && cavalryDistance <= 85f)
            || enemyImpactTime <= repositionTime + safetyMargin;
    }

    private Formation? _cachedNearestEnemyCavalry;
    private float _nearestEnemyCavalryCacheExpiry = float.MinValue;

    private Formation? FindNearestEnemyCavalry(Formation anchor)
    {
        Mission? mission = Mission.Current;
        if (mission == null)
        {
            return null;
        }

        // Scanning every enemy formation each TickOccasionally is O(teams x
        // formations); cavalry doesn't change identity that fast — cache for 2s.
        if (mission.CurrentTime < _nearestEnemyCavalryCacheExpiry
            && (_cachedNearestEnemyCavalry == null || _cachedNearestEnemyCavalry.CountOfUnits > 0))
        {
            return _cachedNearestEnemyCavalry;
        }

        Formation? best = null;
        float bestDistance = float.MaxValue;
        foreach (Team otherTeam in mission.Teams)
        {
            if (otherTeam == null || !otherTeam.IsEnemyOf(base.Team))
            {
                continue;
            }

            foreach (Formation formation in otherTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }

                if (!formation.QuerySystem.IsCavalryFormation && !formation.QuerySystem.IsRangedCavalryFormation)
                {
                    continue;
                }

                float distance = formation.CachedMedianPosition.AsVec2.Distance(anchor.CachedMedianPosition.AsVec2);
                if (distance < bestDistance)
                {
                    best = formation;
                    bestDistance = distance;
                }
            }
        }

        _cachedNearestEnemyCavalry = best;
        _nearestEnemyCavalryCacheExpiry = mission.CurrentTime + 2f;
        return best;
    }

    private WorldPosition GetDefensivePosition(Formation formation)
    {
        return GetDefensivePosition(formation, searchRadius: 16f);
    }

    private WorldPosition GetDefensivePosition(Formation formation, float searchRadius)
    {
        Vec2 enemyPosition = formation.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Team.QuerySystem.AverageEnemyPosition;
        return BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            formation,
            formation.CachedMedianPosition.AsVec2,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius);
    }

    private static void ApplyInfantryArrangement(Formation formation, bool shieldwall)
    {
        ArrangementOrder arrangement = shieldwall && formation.QuerySystem.HasShield
            ? ArrangementOrder.ArrangementOrderShieldWall
            : ArrangementOrder.ArrangementOrderLine;

        if (formation.ArrangementOrder != arrangement)
        {
            formation.SetArrangementOrder(arrangement);
        }
    }

    private void ApplyEmergencyBrace()
    {
        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, shieldwall: true);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.95f).DefensePosition = GetDefensivePosition(_mainInfantry, searchRadius: 6f);
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, shieldwall: true);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            if (_archers != null)
            {
                BehaviorScreenArchers screenBehavior = _supportInfantry.AI.SetBehaviorWeight<BehaviorScreenArchers>(2.1f);
                screenBehavior.ProtectedFormation = _archers;
                screenBehavior.ScreenDistance = 8f;
            }
            else if (_mainInfantry != null)
            {
                BehaviorMaintainReserve reserveBehavior = _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.55f);
                reserveBehavior.AnchorFormation = _mainInfantry;
                reserveBehavior.DesiredDistance = 10f;
            }
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(0.2f);
        }

        ApplyRangedSupport(1f, 0.2f);
        ApplyFlankProtection(1.1f, 0.35f);
    }

    private void ApplyFormShieldwall()
    {
        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, shieldwall: true);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.6f).DefensePosition = GetDefensivePosition(_mainInfantry);
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, shieldwall: _archers == null);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            if (_archers != null)
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorScreenArchers>(1.7f).ProtectedFormation = _archers;
            }
            else if (_mainInfantry != null)
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.4f).AnchorFormation = _mainInfantry;
            }
        }

        ApplyRangedSupport(0.9f, 0.4f);
        ApplyFlankProtection(1f, 0.9f);
    }

    private void ApplyPressAdvance()
    {
        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, shieldwall: false);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.9f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.1f).DefensePosition = GetDefensivePosition(_mainInfantry);
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, shieldwall: false);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            if (_mainInfantry != null)
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.5f).AnchorFormation = _mainInfantry;
            }
            else
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.8f);
            }
        }

        ApplyRangedSupport(1f, 0.7f);
        ApplyFlankProtection(1f, 1f);
    }

    private void ApplyFinalPush()
    {
        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, shieldwall: false);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.2f);
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, shieldwall: false);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.1f);
        }

        ApplyRangedSupport(0.9f, 0.9f);
        ApplyFlankProtection(0.8f, 1f);
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
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(0.8f);
        }
    }

    private void ApplyFlankProtection(float protectFlankWeight, float tacticalChargeWeight)
    {
        if (_leftCavalry != null)
        {
            _leftCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_leftCavalry);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(protectFlankWeight).FlankSide = FormationAI.BehaviorSide.Left;
            if (tacticalChargeWeight > 0f)
            {
                _leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(tacticalChargeWeight);
            }
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rightCavalry);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(protectFlankWeight).FlankSide = FormationAI.BehaviorSide.Right;
            if (tacticalChargeWeight > 0f)
            {
                _rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(tacticalChargeWeight);
            }
        }
    }
}
