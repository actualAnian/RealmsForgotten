using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticFeignedRetreat : TacticComponent
{
    private enum FeignedRetreatState
    {
        HarassAndWithdraw,
        LureEnemy,
        TurnAndStrike
    }

    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private FeignedRetreatState? _lastState;
    private readonly HysteresisGate _strongPowerGate = HysteresisGate.RisesAbove(1f);
    private readonly HysteresisGate _strikePowerGate = HysteresisGate.RisesAbove(0.95f);

    public TacticFeignedRetreat(Team team)
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

        if ((_leftCavalry == null || _leftCavalry.CountOfUnits <= 0)
            && (_rightCavalry == null || _rightCavalry.CountOfUnits <= 0)
            && (_rangedCavalry == null || _rangedCavalry.CountOfUnits <= 0))
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

        FeignedRetreatState state = EvaluateState();
        if (_lastState != state)
        {
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticFeignedRetreat), state.ToString());
            _lastState = state;
        }

        switch (state)
        {
            case FeignedRetreatState.HarassAndWithdraw:
                ApplyHarassAndWithdraw();
                break;
            case FeignedRetreatState.LureEnemy:
                ApplyLureEnemy();
                break;
            default:
                ApplyTurnAndStrike();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        float mountedMissileRatio = base.Team.QuerySystem.RangedCavalryRatio;
        float cavalryRatio = base.Team.QuerySystem.CavalryRatio + mountedMissileRatio;
        float score = 0.22f;

        if (mountedMissileRatio > 0.08f)
        {
            score += 0.3f;
        }

        if (cavalryRatio > 0.22f)
        {
            score += 0.2f;
        }

        if (base.Team.QuerySystem.RangedRatio > 0.12f)
        {
            score += 0.1f;
        }

        if (base.Team.Side == BattleSideEnum.Attacker)
        {
            score += 0.1f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticFeignedRetreat), score);
    }

    private FeignedRetreatState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;

        if (distanceSquared > 2500f || !_strongPowerGate.Evaluate(powerRatio))
        {
            return FeignedRetreatState.HarassAndWithdraw;
        }

        if (distanceSquared > 900f)
        {
            return FeignedRetreatState.LureEnemy;
        }

        return _strikePowerGate.Evaluate(powerRatio)
            ? FeignedRetreatState.TurnAndStrike
            : FeignedRetreatState.LureEnemy;
    }

    private float GetEngagementDistanceSquared()
    {
        Formation? anchor = _rangedCavalry ?? _leftCavalry ?? _rightCavalry ?? _archers ?? _mainInfantry ?? _supportInfantry;
        if (anchor?.CachedClosestEnemyFormation == null)
        {
            return float.MaxValue;
        }

        return anchor.CachedMedianPosition.AsVec2.DistanceSquared(anchor.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
    }

    private void ApplyHarassAndWithdraw()
    {
        ApplyInfantryWithdrawal(mainFallbackWeight: 1.5f, supportFallbackWeight: 1.2f);
        ApplyRangedFootSupport(1f, 0.8f);
        ApplyMountedWithdrawal(flankWeight: 0.5f, chargeWeight: 0.2f);
    }

    private void ApplyLureEnemy()
    {
        ApplyInfantryWithdrawal(mainFallbackWeight: 1.2f, supportFallbackWeight: 1f);
        ApplyRangedFootSupport(1f, 1f);
        ApplyMountedWithdrawal(flankWeight: 0.9f, chargeWeight: 0.45f);
    }

    private void ApplyTurnAndStrike()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.1f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.9f);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
        }

        ApplyRangedFootSupport(0.9f, 1f);
        ApplyMountedStrike();
    }

    private void ApplyInfantryWithdrawal(float mainFallbackWeight, float supportFallbackWeight)
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(mainFallbackWeight).AnchorFormation = _mainInfantry;
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(supportFallbackWeight).AnchorFormation = _mainInfantry ?? _supportInfantry;
        }
    }

    private void ApplyRangedFootSupport(float screenedSkirmishWeight, float skirmishWeight)
    {
        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(screenedSkirmishWeight);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(skirmishWeight);
        }
    }

    private void ApplyMountedWithdrawal(float flankWeight, float chargeWeight)
    {
        ApplyMountedWing(_leftCavalry, FormationAI.BehaviorSide.Left, flankWeight, chargeWeight);
        ApplyMountedWing(_rightCavalry, FormationAI.BehaviorSide.Right, flankWeight, chargeWeight);

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1.2f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1.2f);
        }
    }

    private void ApplyMountedStrike()
    {
        ApplyMountedWing(_leftCavalry, FormationAI.BehaviorSide.Left, flankWeight: 1.2f, chargeWeight: 1.1f);
        ApplyMountedWing(_rightCavalry, FormationAI.BehaviorSide.Right, flankWeight: 1.2f, chargeWeight: 1.1f);

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(0.9f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.9f);
        }
    }

    private void ApplyMountedWing(Formation? cavalry, FormationAI.BehaviorSide side, float flankWeight, float chargeWeight)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(cavalry);
        cavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(0.7f).FlankSide = side;
        cavalry.AI.SetBehaviorWeight<BehaviorFlank>(flankWeight);
        cavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(chargeWeight);
    }
}
