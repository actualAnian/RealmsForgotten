using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticCannaeEnvelopment : TacticComponent
{
    private enum CannaeBattleState
    {
        DrawCenter,
        HoldPocket,
        CloseTrap
    }

    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private CannaeBattleState? _lastState;

    public TacticCannaeEnvelopment(Team team)
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

        if (_supportInfantry == null || _supportInfantry.CountOfUnits <= 0 || !_supportInfantry.QuerySystem.IsInfantryFormation)
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

        CannaeBattleState state = EvaluateState();
        if (_lastState != state)
        {
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticCannaeEnvelopment), state.ToString());
            _lastState = state;
        }

        switch (state)
        {
            case CannaeBattleState.DrawCenter:
                ApplyDrawCenter();
                break;
            case CannaeBattleState.HoldPocket:
                ApplyHoldPocket();
                break;
            default:
                ApplyCloseTrap();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        float cavalryRatio = base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio;
        float score = 0.2f;

        if (CountInfantryFormations(base.Team) >= 2)
        {
            score += 0.3f;
        }

        if (cavalryRatio > 0.14f)
        {
            score += 0.2f;
        }

        if (base.Team.QuerySystem.InfantryRatio > 0.4f)
        {
            score += 0.15f;
        }

        if (base.Team.Side == BattleSideEnum.Defender)
        {
            score += 0.1f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticCannaeEnvelopment), score);
    }

    private CannaeBattleState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;

        if (distanceSquared > 2500f)
        {
            return CannaeBattleState.DrawCenter;
        }

        if (distanceSquared > 784f || powerRatio < 0.95f)
        {
            return CannaeBattleState.HoldPocket;
        }

        return CannaeBattleState.CloseTrap;
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

    private void ApplyDrawCenter()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(1.35f).AnchorFormation = _mainInfantry;
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.2f).DefensePosition = _supportInfantry.CachedMedianPosition;
            if (_mainInfantry != null)
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1f).AnchorFormation = _mainInfantry;
            }
        }

        ApplyRangedSupport(1f, 0.7f);
        ApplyFlankClosure(protectWeight: 1f, flankWeight: 0.8f, chargeWeight: 0.5f);
    }

    private void ApplyHoldPocket()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.1f).DefensePosition = _mainInfantry.CachedMedianPosition;
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.6f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.9f);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(0.8f);
        }

        ApplyRangedSupport(1f, 0.9f);
        ApplyFlankClosure(protectWeight: 0.8f, flankWeight: 1f, chargeWeight: 0.8f);
    }

    private void ApplyCloseTrap()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.9f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.15f);
        }

        ApplyRangedSupport(0.9f, 0.9f);
        ApplyFlankClosure(protectWeight: 0.45f, flankWeight: 1.2f, chargeWeight: 1.1f);
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
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(0.8f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.7f);
        }
    }

    private void ApplyFlankClosure(float protectWeight, float flankWeight, float chargeWeight)
    {
        ApplyWingBehavior(_leftCavalry, FormationAI.BehaviorSide.Left, protectWeight, flankWeight, chargeWeight);
        ApplyWingBehavior(_rightCavalry, FormationAI.BehaviorSide.Right, protectWeight, flankWeight, chargeWeight);
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

    private static int CountInfantryFormations(Team team)
    {
        int count = 0;
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits > 0 && formation.QuerySystem.IsInfantryFormation)
            {
                count++;
            }
        }

        return count;
    }
}
