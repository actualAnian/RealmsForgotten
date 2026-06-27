using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticBaitAndPounce : TacticComponent
{
    private enum BaitAndPounceState
    {
        SetBait,
        DrawIn,
        Pounce
    }

    private int _cachedAiControlledFormationCount;
    private BaitAndPounceState? _lastState;

    public TacticBaitAndPounce(Team team)
        : base(team)
    {
    }

    protected override void ManageFormationCounts()
    {
        ManageFormationCounts(1, 1, 2, 1);

        _mainInfantry = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower).FirstOrDefault();

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

        if ((_leftCavalry == null || _leftCavalry.CountOfUnits <= 0)
            && (_rightCavalry == null || _rightCavalry.CountOfUnits <= 0)
            && (_rangedCavalry == null || _rangedCavalry.CountOfUnits <= 0))
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

        BaitAndPounceState state = EvaluateState();
        if (_lastState != state)
        {
            float distance = (float)System.Math.Sqrt(GetEngagementDistanceSquared());
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticBaitAndPounce), state.ToString(), $"distance={distance:F1} power={base.Team.QuerySystem.RemainingPowerRatio:F2}");
            _lastState = state;
        }

        switch (state)
        {
            case BaitAndPounceState.SetBait:
                ApplySetBait();
                break;
            case BaitAndPounceState.DrawIn:
                ApplyDrawIn();
                break;
            default:
                ApplyPounce();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(base.Team);
        float score = 0.14f;

        if (composition.InfantryRatio > 0.3f)
        {
            score += 0.18f;
        }

        if (composition.MountedRatio > 0.12f)
        {
            score += 0.24f;
        }

        if (composition.RangedRatio > 0.08f)
        {
            score += 0.12f;
        }

        if (base.Team.Side == BattleSideEnum.Defender)
        {
            score += 0.16f;
        }

        if (base.Team.QuerySystem.RemainingPowerRatio <= 1.15f)
        {
            score += 0.1f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticBaitAndPounce), score);
    }

    private BaitAndPounceState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();

        if (distanceSquared > 4900f)
        {
            return BaitAndPounceState.SetBait;
        }

        if (distanceSquared > 1600f && base.Team.QuerySystem.RemainingPowerRatio >= 0.75f)
        {
            return BaitAndPounceState.DrawIn;
        }

        return BaitAndPounceState.Pounce;
    }

    private float GetEngagementDistanceSquared()
    {
        Formation? anchor = _mainInfantry ?? _archers ?? _leftCavalry ?? _rightCavalry ?? _rangedCavalry;
        if (anchor?.CachedClosestEnemyFormation == null)
        {
            return float.MaxValue;
        }

        return anchor.CachedMedianPosition.AsVec2.DistanceSquared(anchor.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
    }

    private WorldPosition GetBaitPosition(Formation formation)
    {
        Vec2 enemyPosition = formation.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Team.QuerySystem.AverageEnemyPosition;
        return BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            formation,
            formation.CachedMedianPosition.AsVec2,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 14f);
    }

    private void ApplySetBait()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.35f).DefensePosition = GetBaitPosition(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.25f);
        }

        ApplyArchers(screenedWeight: 1.1f, skirmishWeight: 0.25f);
        ApplyCavalry(flankWeight: 0.3f, chargeWeight: 0.15f, protectWeight: 1.1f);
    }

    private void ApplyDrawIn()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).DefensePosition = GetBaitPosition(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(0.65f).AnchorFormation = _mainInfantry;
        }

        ApplyArchers(screenedWeight: 1f, skirmishWeight: 0.75f);
        ApplyCavalry(flankWeight: 0.85f, chargeWeight: 0.35f, protectWeight: 0.8f);
    }

    private void ApplyPounce()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.15f);
        }

        ApplyArchers(screenedWeight: 0.85f, skirmishWeight: 1f);
        ApplyCavalry(flankWeight: 1.15f, chargeWeight: 1.25f, protectWeight: 0.25f);
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

    private void ApplyCavalry(float flankWeight, float chargeWeight, float protectWeight)
    {
        ApplyCavalryWing(_leftCavalry, FormationAI.BehaviorSide.Left, flankWeight, chargeWeight, protectWeight);
        ApplyCavalryWing(_rightCavalry, FormationAI.BehaviorSide.Right, flankWeight, chargeWeight, protectWeight);
    }

    private static void ApplyCavalryWing(Formation? cavalry, FormationAI.BehaviorSide side, float flankWeight, float chargeWeight, float protectWeight)
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
