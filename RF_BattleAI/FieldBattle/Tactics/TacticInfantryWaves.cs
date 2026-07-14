using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticInfantryWaves : TacticComponent
{
    private enum InfantryWavesState
    {
        FormWaves,
        FirstWave,
        SecondWave,
        FullCommit
    }

    private Formation? _secondWaveInfantry;
    private int _cachedAiControlledFormationCount;
    private InfantryWavesState? _lastState;
    private readonly HysteresisGate _firstWavePowerGate = HysteresisGate.RisesAbove(0.75f);
    private readonly HysteresisGate _secondWavePowerGate = HysteresisGate.RisesAbove(0.65f);

    public TacticInfantryWaves(Team team)
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
        _secondWaveInfantry = infantryFormations.Skip(1).FirstOrDefault();
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

        if (_secondWaveInfantry != null)
        {
            _secondWaveInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
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

        // A missing second wave is a valid (degraded) configuration — the Apply*
        // methods all tolerate it. Only re-manage when a wave we HAD became invalid,
        // otherwise single-infantry armies trigger a reapply loop every tick.
        if (_secondWaveInfantry != null && (_secondWaveInfantry.CountOfUnits <= 0 || !_secondWaveInfantry.QuerySystem.IsInfantryFormation))
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

        InfantryWavesState state = EvaluateState();
        if (_lastState != state)
        {
            float distance = (float)System.Math.Sqrt(GetEngagementDistanceSquared());
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticInfantryWaves), state.ToString(), $"distance={distance:F1} power={base.Team.QuerySystem.RemainingPowerRatio:F2}");
            _lastState = state;
        }

        switch (state)
        {
            case InfantryWavesState.FormWaves:
                ApplyFormWaves();
                break;
            case InfantryWavesState.FirstWave:
                ApplyFirstWave();
                break;
            case InfantryWavesState.SecondWave:
                ApplySecondWave();
                break;
            default:
                ApplyFullCommit();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(base.Team);

        // Waves need a second echelon. With a single infantry formation the tactic
        // still runs (degraded), but auto-selection should prefer single-line
        // tactics; manual override (+100) remains available.
        if (composition.InfantryFormationCount < 2)
        {
            return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticInfantryWaves), 0.05f);
        }

        float score = 0.12f;

        if (composition.InfantryRatio > 0.55f)
        {
            score += 0.32f;
        }

        if (composition.InfantryFormationCount >= 2)
        {
            score += 0.28f;
        }

        if (composition.RangedRatio > 0.08f)
        {
            score += 0.08f;
        }

        if (composition.MountedRatio < 0.18f)
        {
            score += 0.12f;
        }

        if (base.Team.QuerySystem.RemainingPowerRatio >= 0.85f)
        {
            score += 0.08f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticInfantryWaves), score);
    }

    private InfantryWavesState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;

        if (distanceSquared > 6400f)
        {
            return InfantryWavesState.FormWaves;
        }

        // A FAILED power gate means "too weak to commit at this range" — hold at
        // the MORE DEFENSIVE state, never fall through to the more aggressive
        // one (the old `&&` chain made a weak army escalate to FullCommit).
        if (distanceSquared > 2500f)
        {
            return _firstWavePowerGate.Evaluate(powerRatio)
                ? InfantryWavesState.FirstWave
                : InfantryWavesState.FormWaves;
        }

        if (distanceSquared > 900f)
        {
            return _secondWavePowerGate.Evaluate(powerRatio)
                ? InfantryWavesState.SecondWave
                : InfantryWavesState.FirstWave;
        }

        return InfantryWavesState.FullCommit;
    }

    private float GetEngagementDistanceSquared()
    {
        Formation? anchor = _mainInfantry ?? _secondWaveInfantry ?? _archers ?? _leftCavalry ?? _rightCavalry ?? _rangedCavalry;
        if (anchor?.CachedClosestEnemyFormation == null)
        {
            return float.MaxValue;
        }

        return anchor.CachedMedianPosition.AsVec2.DistanceSquared(anchor.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
    }

    private WorldPosition GetForwardDefensivePosition(Formation formation)
    {
        Vec2 enemyPosition = formation.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Team.QuerySystem.AverageEnemyPosition;
        return BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            formation,
            formation.CachedMedianPosition.AsVec2,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 12f);
    }

    private void ApplyFormWaves()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.55f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(0.9f).DefensePosition = GetForwardDefensivePosition(_mainInfantry);
        }

        if (_secondWaveInfantry != null)
        {
            _secondWaveInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_secondWaveInfantry);
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.8f).AnchorFormation = _mainInfantry;
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.25f);
        }

        ApplyRangedSupport(screenedWeight: 1.1f, skirmishWeight: 0.35f);
        ApplyCavalrySupport(protectWeight: 1f, flankWeight: 0.35f, chargeWeight: 0.2f);
    }

    private void ApplyFirstWave()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(0.75f);
        }

        if (_secondWaveInfantry != null)
        {
            _secondWaveInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_secondWaveInfantry);
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.45f).AnchorFormation = _mainInfantry;
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.45f);
        }

        ApplyRangedSupport(screenedWeight: 1.1f, skirmishWeight: 0.55f);
        ApplyCavalrySupport(protectWeight: 0.9f, flankWeight: 0.55f, chargeWeight: 0.45f);
    }

    private void ApplySecondWave()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.1f);
        }

        if (_secondWaveInfantry != null)
        {
            _secondWaveInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_secondWaveInfantry);
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.2f);
        }

        ApplyRangedSupport(screenedWeight: 1f, skirmishWeight: 0.75f);
        ApplyCavalrySupport(protectWeight: 0.65f, flankWeight: 0.85f, chargeWeight: 0.75f);
    }

    private void ApplyFullCommit()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.35f);
        }

        if (_secondWaveInfantry != null)
        {
            _secondWaveInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_secondWaveInfantry);
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _secondWaveInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.35f);
        }

        ApplyRangedSupport(screenedWeight: 0.85f, skirmishWeight: 0.9f);
        ApplyCavalrySupport(protectWeight: 0.3f, flankWeight: 1f, chargeWeight: 1f);
    }

    private void ApplyRangedSupport(float screenedWeight, float skirmishWeight)
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

    private void ApplyCavalrySupport(float protectWeight, float flankWeight, float chargeWeight)
    {
        ApplyCavalrySupport(_leftCavalry, FormationAI.BehaviorSide.Left, protectWeight, flankWeight, chargeWeight);
        ApplyCavalrySupport(_rightCavalry, FormationAI.BehaviorSide.Right, protectWeight, flankWeight, chargeWeight);
    }

    private static void ApplyCavalrySupport(Formation? cavalry, FormationAI.BehaviorSide side, float protectWeight, float flankWeight, float chargeWeight)
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
