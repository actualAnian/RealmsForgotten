using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticRefusedFlank : TacticComponent
{
    private enum RefusedFlankState
    {
        RefuseAndAnchor,
        HoldAndPressure,
        CounterPush
    }

    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private RefusedFlankState? _lastState;
    private readonly HysteresisGate _counterPowerGate = HysteresisGate.RisesAbove(1f);
    private bool _refuseLeftFlank;

    public TacticRefusedFlank(Team team)
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

        _refuseLeftFlank = ShouldRefuseLeftFlank();
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

        RefusedFlankState state = EvaluateState();
        if (_lastState != state)
        {
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticRefusedFlank), state.ToString());
            _lastState = state;
        }

        switch (state)
        {
            case RefusedFlankState.RefuseAndAnchor:
                ApplyRefuseAndAnchor();
                break;
            case RefusedFlankState.HoldAndPressure:
                ApplyHoldAndPressure();
                break;
            default:
                ApplyCounterPush();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        float score = 0.18f;
        float cavalryRatio = base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio;

        if (base.Team.QuerySystem.InfantryRatio > 0.4f)
        {
            score += 0.25f;
        }

        if (_supportInfantry != null)
        {
            score += 0.2f;
        }

        if (cavalryRatio > 0.08f)
        {
            score += 0.1f;
        }

        if (base.Team.Side == BattleSideEnum.Defender)
        {
            score += 0.15f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticRefusedFlank), score);
    }

    private RefusedFlankState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;

        if (distanceSquared > 3025f)
        {
            return RefusedFlankState.RefuseAndAnchor;
        }

        if (distanceSquared > 1024f || !_counterPowerGate.Evaluate(powerRatio))
        {
            return RefusedFlankState.HoldAndPressure;
        }

        return RefusedFlankState.CounterPush;
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

    private bool ShouldRefuseLeftFlank()
    {
        if (BattleAITacticController.TryGetLockedFlankPreference(base.Team, out bool refuseLeft))
        {
            return refuseLeft;
        }

        float leftPower = (_leftCavalry?.QuerySystem.FormationPower ?? 0f) + ((_mainInfantry?.QuerySystem.FormationPower ?? 0f) * 0.25f);
        float rightPower = (_rightCavalry?.QuerySystem.FormationPower ?? 0f) + ((_supportInfantry?.QuerySystem.FormationPower ?? 0f) * 0.25f);

        bool computedRefuseLeft;
        if (leftPower <= 0f && rightPower <= 0f)
        {
            computedRefuseLeft = true;
        }
        else
        {
            computedRefuseLeft = leftPower <= rightPower;
        }

        BattleAITacticController.LockFlankPreference(base.Team, computedRefuseLeft);
        return computedRefuseLeft;
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

    private void ApplyRefuseAndAnchor()
    {
        ApplyMainLine(advanceWeight: 0.65f, defendWeight: 1.3f, chargeWeight: 0f);
        ApplySupportLine(advanceWeight: 0.35f, reserveWeight: 1.5f, chargeWeight: 0f);
        ApplyRangedSupport(1f, 0.6f);
        ApplyFlankRoles(activeFlankWeight: 0.7f, refusedProtectWeight: 1.2f, activeChargeWeight: 0.5f, refusedChargeWeight: 0.2f);
    }

    private void ApplyHoldAndPressure()
    {
        ApplyMainLine(advanceWeight: 0.85f, defendWeight: 1f, chargeWeight: 0f);
        ApplySupportLine(advanceWeight: 0.55f, reserveWeight: 1.2f, chargeWeight: 0f);
        ApplyRangedSupport(1f, 0.8f);
        ApplyFlankRoles(activeFlankWeight: 0.95f, refusedProtectWeight: 1f, activeChargeWeight: 0.7f, refusedChargeWeight: 0.35f);
    }

    private void ApplyCounterPush()
    {
        ApplyMainLine(advanceWeight: 1f, defendWeight: 0f, chargeWeight: 1.15f);
        ApplySupportLine(advanceWeight: 0.95f, reserveWeight: 0f, chargeWeight: 1f);
        ApplyRangedSupport(0.9f, 0.9f);
        ApplyFlankRoles(activeFlankWeight: 1.15f, refusedProtectWeight: 0.75f, activeChargeWeight: 1.1f, refusedChargeWeight: 0.75f);
    }

    private void ApplyMainLine(float advanceWeight, float defendWeight, float chargeWeight)
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

    private void ApplySupportLine(float advanceWeight, float reserveWeight, float chargeWeight)
    {
        if (_supportInfantry == null)
        {
            return;
        }

        _supportInfantry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(_supportInfantry);
        _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(advanceWeight);

        if (_mainInfantry != null && reserveWeight > 0f)
        {
            _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(reserveWeight).AnchorFormation = _mainInfantry;
        }

        if (chargeWeight > 0f)
        {
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(chargeWeight);
        }
    }

    private void ApplyRangedSupport(float screenedSkirmishWeight, float skirmishWeight)
    {
        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(screenedSkirmishWeight);
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

    private void ApplyFlankRoles(float activeFlankWeight, float refusedProtectWeight, float activeChargeWeight, float refusedChargeWeight)
    {
        ApplyFlankBehavior(_refuseLeftFlank ? _rightCavalry : _leftCavalry, _refuseLeftFlank ? FormationAI.BehaviorSide.Right : FormationAI.BehaviorSide.Left, protectWeight: 0.7f, flankWeight: activeFlankWeight, chargeWeight: activeChargeWeight);
        ApplyFlankBehavior(_refuseLeftFlank ? _leftCavalry : _rightCavalry, _refuseLeftFlank ? FormationAI.BehaviorSide.Left : FormationAI.BehaviorSide.Right, protectWeight: refusedProtectWeight, flankWeight: 0.35f, chargeWeight: refusedChargeWeight);
    }

    private void ApplyFlankBehavior(Formation? cavalry, FormationAI.BehaviorSide side, float protectWeight, float flankWeight, float chargeWeight)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(cavalry);
        cavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(protectWeight).FlankSide = side;
        cavalry.AI.SetBehaviorWeight<BehaviorFlank>(flankWeight);
        if (chargeWeight > 0f)
        {
            cavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(chargeWeight);
        }
    }
}
