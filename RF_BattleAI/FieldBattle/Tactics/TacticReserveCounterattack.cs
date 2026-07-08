using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticReserveCounterattack : TacticComponent
{
    private Formation? _reserveInfantry;

    private int _cachedAiControlledFormationCount;
    private bool? _lastCommittedState;
    private readonly HysteresisGate _commitPowerGate = HysteresisGate.FallsBelow(0.85f);

    public TacticReserveCounterattack(Team team)
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
        _reserveInfantry = infantryFormations.Skip(1).FirstOrDefault();
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

        if (_reserveInfantry != null)
        {
            _reserveInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
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

        if (_reserveInfantry != null && (_reserveInfantry.CountOfUnits <= 0 || !_reserveInfantry.QuerySystem.IsInfantryFormation))
        {
            return true;
        }

        return false;
    }

    private bool ShouldCommitReserve()
    {
        if (_mainInfantry?.CachedClosestEnemyFormation == null)
        {
            return false;
        }

        float distanceSquared = _mainInfantry.CachedMedianPosition.AsVec2.DistanceSquared(
            _mainInfantry.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);

        return distanceSquared < 1600f || _commitPowerGate.Evaluate(base.Team.QuerySystem.RemainingPowerRatio);
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

    private void ApplyHoldingPattern()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.5f).DefensePosition = GetDefensivePosition(_mainInfantry);
        }

        if (_reserveInfantry != null)
        {
            _reserveInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_reserveInfantry);
            BehaviorMaintainReserve reserveBehavior = _reserveInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(2f);
            reserveBehavior.AnchorFormation = _mainInfantry;
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
        }

        if (_leftCavalry != null)
        {
            _leftCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_leftCavalry);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rightCavalry);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
        }
    }

    private void ApplyCounterattackPattern()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.25f);
        }

        if (_reserveInfantry != null)
        {
            _reserveInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_reserveInfantry);
            _reserveInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _reserveInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.5f);
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
        }

        if (_leftCavalry != null)
        {
            _leftCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_leftCavalry);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rightCavalry);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
        }
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

        bool commitReserve = ShouldCommitReserve();
        if (_lastCommittedState != commitReserve)
        {
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticReserveCounterattack), commitReserve ? "Counterattack" : "HoldReserve");
            _lastCommittedState = commitReserve;
        }

        if (commitReserve)
        {
            ApplyCounterattackPattern();
        }
        else
        {
            ApplyHoldingPattern();
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        int infantryFormationCount = FormationsIncludingEmpty.Count(f => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation);
        float cavalryRatio = base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio;

        float weight = 0.15f;
        if (base.Team.Side == BattleSideEnum.Defender)
        {
            weight += 0.25f;
        }

        if (infantryFormationCount >= 2)
        {
            weight += 0.45f;
        }

        if (cavalryRatio > 0.12f)
        {
            weight += 0.15f;
        }

        if (base.Team.QuerySystem.InfantryRatio > 0.35f)
        {
            weight += 0.1f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticReserveCounterattack), weight);
    }
}
