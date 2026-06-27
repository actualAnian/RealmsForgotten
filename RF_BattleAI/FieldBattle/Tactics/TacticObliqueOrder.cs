using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticObliqueOrder : TacticComponent
{
    private enum ObliqueBattleState
    {
        RefusedWing,
        WeightedAdvance,
        FullCommit
    }

    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private ObliqueBattleState? _lastState;
    private bool _favorLeftWing;

    public TacticObliqueOrder(Team team)
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

        _favorLeftWing = ShouldFavorLeftWing();
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

        ObliqueBattleState state = EvaluateState();
        if (_lastState != state)
        {
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticObliqueOrder), state.ToString());
            _lastState = state;
        }

        switch (state)
        {
            case ObliqueBattleState.RefusedWing:
                ApplyRefusedWing();
                break;
            case ObliqueBattleState.WeightedAdvance:
                ApplyWeightedAdvance();
                break;
            default:
                ApplyFullCommit();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        float cavalryRatio = base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio;
        float score = 0.18f;

        if (base.Team.QuerySystem.InfantryRatio > 0.35f)
        {
            score += 0.25f;
        }

        if (cavalryRatio > 0.1f)
        {
            score += 0.15f;
        }

        if (_supportInfantry != null)
        {
            score += 0.2f;
        }

        if (base.Team.Side == BattleSideEnum.Attacker)
        {
            score += 0.1f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticObliqueOrder), score);
    }

    private ObliqueBattleState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;

        if (distanceSquared > 3025f)
        {
            return ObliqueBattleState.RefusedWing;
        }

        if (distanceSquared > 1024f && powerRatio >= 0.9f)
        {
            return ObliqueBattleState.WeightedAdvance;
        }

        return ObliqueBattleState.FullCommit;
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

    private bool ShouldFavorLeftWing()
    {
        if (BattleAITacticController.TryGetLockedFlankPreference(base.Team, out bool favorLeft))
        {
            return favorLeft;
        }

        float leftPower = _leftCavalry?.QuerySystem.FormationPower ?? 0f;
        float rightPower = _rightCavalry?.QuerySystem.FormationPower ?? 0f;

        bool computedFavorLeft;
        if (leftPower == 0f && rightPower == 0f)
        {
            computedFavorLeft = true;
        }
        else
        {
            computedFavorLeft = leftPower >= rightPower;
        }

        BattleAITacticController.LockFlankPreference(base.Team, computedFavorLeft);
        return computedFavorLeft;
    }

    private void ApplyRefusedWing()
    {
        ApplyInfantryLine(mainAdvanceWeight: 0.75f, mainDefendWeight: 1.2f, supportAdvanceWeight: 0.4f, supportReserveWeight: 1.4f);
        ApplyRangedSupport(1f, 0.6f);
        ApplyWingPressure(activeFlankWeight: 0.8f, passiveProtectWeight: 1.1f, activeChargeWeight: 0.6f, passiveChargeWeight: 0.3f);
    }

    private void ApplyWeightedAdvance()
    {
        ApplyInfantryLine(mainAdvanceWeight: 0.95f, mainDefendWeight: 0.9f, supportAdvanceWeight: 0.7f, supportReserveWeight: 1.1f);
        ApplyRangedSupport(1f, 0.8f);
        ApplyWingPressure(activeFlankWeight: 1.1f, passiveProtectWeight: 0.9f, activeChargeWeight: 0.9f, passiveChargeWeight: 0.5f);
    }

    private void ApplyFullCommit()
    {
        ApplyInfantryLine(mainAdvanceWeight: 1f, mainDefendWeight: 0.4f, supportAdvanceWeight: 1f, supportReserveWeight: 0.5f);
        ApplyRangedSupport(0.9f, 0.9f);
        ApplyWingPressure(activeFlankWeight: 1.2f, passiveProtectWeight: 0.7f, activeChargeWeight: 1.2f, passiveChargeWeight: 0.9f);
    }

    private void ApplyInfantryLine(float mainAdvanceWeight, float mainDefendWeight, float supportAdvanceWeight, float supportReserveWeight)
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(mainAdvanceWeight);
            if (mainDefendWeight > 0f)
            {
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(mainDefendWeight).DefensePosition = _mainInfantry.CachedMedianPosition;
            }
            else
            {
                _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.1f);
            }
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(supportAdvanceWeight);
            if (_mainInfantry != null && supportReserveWeight > 0f)
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(supportReserveWeight).AnchorFormation = _mainInfantry;
            }
            else
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }
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

    private void ApplyWingPressure(float activeFlankWeight, float passiveProtectWeight, float activeChargeWeight, float passiveChargeWeight)
    {
        ApplyWingBehavior(_favorLeftWing ? _leftCavalry : _rightCavalry, _favorLeftWing ? FormationAI.BehaviorSide.Left : FormationAI.BehaviorSide.Right, protectWeight: 0.6f, flankWeight: activeFlankWeight, chargeWeight: activeChargeWeight);
        ApplyWingBehavior(_favorLeftWing ? _rightCavalry : _leftCavalry, _favorLeftWing ? FormationAI.BehaviorSide.Right : FormationAI.BehaviorSide.Left, protectWeight: passiveProtectWeight, flankWeight: 0.45f, chargeWeight: passiveChargeWeight);
    }

    private void ApplyWingBehavior(Formation? cavalry, FormationAI.BehaviorSide side, float protectWeight, float flankWeight, float chargeWeight)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(cavalry);
        cavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(protectWeight).FlankSide = side;
        cavalry.AI.SetBehaviorWeight<BehaviorFlank>(flankWeight);
        cavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(chargeWeight);
    }
}
