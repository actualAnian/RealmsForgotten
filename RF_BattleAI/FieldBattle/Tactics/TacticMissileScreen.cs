using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticMissileScreen : TacticComponent
{
    private enum MissileScreenState
    {
        SetLine,
        FireAndFallBack,
        CloseDefense
    }

    private Formation? _supportInfantry;
    private Formation? _supportArchers;
    private int _cachedAiControlledFormationCount;
    private MissileScreenState? _lastState;

    public TacticMissileScreen(Team team)
        : base(team)
    {
    }

    protected override void ManageFormationCounts()
    {
        ManageFormationCounts(2, 2, 1, 1);

        List<Formation> infantryFormations = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower);

        _mainInfantry = infantryFormations.FirstOrDefault();
        _supportInfantry = infantryFormations.Skip(1).FirstOrDefault();

        List<Formation> archerFormations = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower);

        _archers = archerFormations.FirstOrDefault();
        _supportArchers = archerFormations.Skip(1).FirstOrDefault();

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

        if (_archers == null || _archers.CountOfUnits <= 0 || !_archers.QuerySystem.IsRangedFormation)
        {
            return true;
        }

        if (_supportInfantry != null && (_supportInfantry.CountOfUnits <= 0 || !_supportInfantry.QuerySystem.IsInfantryFormation))
        {
            return true;
        }

        if (_supportArchers != null && (_supportArchers.CountOfUnits <= 0 || !_supportArchers.QuerySystem.IsRangedFormation))
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

        MissileScreenState state = EvaluateState();
        if (_lastState != state)
        {
            float distance = (float)System.Math.Sqrt(GetEngagementDistanceSquared());
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticMissileScreen), state.ToString(), $"distance={distance:F1} power={base.Team.QuerySystem.RemainingPowerRatio:F2}");
            _lastState = state;
        }

        switch (state)
        {
            case MissileScreenState.SetLine:
                ApplySetLine();
                break;
            case MissileScreenState.FireAndFallBack:
                ApplyFireAndFallBack();
                break;
            default:
                ApplyCloseDefense();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(base.Team);
        float score = 0.12f;

        if (composition.RangedRatio > 0.22f)
        {
            score += 0.34f;
        }

        if (composition.InfantryRatio > 0.24f)
        {
            score += 0.18f;
        }

        if (composition.MountedRatio < 0.24f)
        {
            score += 0.1f;
        }

        if (base.Team.Side == BattleSideEnum.Defender)
        {
            score += 0.12f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticMissileScreen), score);
    }

    private MissileScreenState EvaluateState()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;

        if (distanceSquared > 4900f)
        {
            return MissileScreenState.SetLine;
        }

        if (distanceSquared > 1600f && powerRatio >= 0.75f)
        {
            return MissileScreenState.FireAndFallBack;
        }

        return MissileScreenState.CloseDefense;
    }

    private float GetEngagementDistanceSquared()
    {
        Formation? anchor = _archers ?? _supportArchers ?? _mainInfantry ?? _supportInfantry ?? _leftCavalry ?? _rightCavalry ?? _rangedCavalry;
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

    private void ApplySetLine()
    {
        ApplyInfantryScreen(defendWeight: 1.45f, fallbackWeight: 0.15f);
        ApplyArchers(screenedWeight: 1.2f, skirmishWeight: 0.35f);
        ApplyCavalry(protectWeight: 1f, flankWeight: 0.25f, chargeWeight: 0.1f);
    }

    private void ApplyFireAndFallBack()
    {
        ApplyInfantryScreen(defendWeight: 1f, fallbackWeight: 0.65f);
        ApplyArchers(screenedWeight: 1.1f, skirmishWeight: 0.95f);
        ApplyCavalry(protectWeight: 0.9f, flankWeight: 0.55f, chargeWeight: 0.25f);
    }

    private void ApplyCloseDefense()
    {
        ApplyInfantryScreen(defendWeight: 0.55f, fallbackWeight: 0f);
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(0.9f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(0.75f);
        }

        ApplyArchers(screenedWeight: 1f, skirmishWeight: 1.1f);
        ApplyCavalry(protectWeight: 0.45f, flankWeight: 0.8f, chargeWeight: 0.75f);
    }

    private void ApplyInfantryScreen(float defendWeight, float fallbackWeight)
    {
        ApplyInfantryFormation(_mainInfantry, defendWeight, fallbackWeight);

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            if (_archers != null)
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorScreenArchers>(1.6f).ProtectedFormation = _archers;
            }
            else
            {
                ApplyInfantryFormation(_supportInfantry, defendWeight, fallbackWeight);
            }
        }
    }

    private void ApplyInfantryFormation(Formation? infantry, float defendWeight, float fallbackWeight)
    {
        if (infantry == null)
        {
            return;
        }

        infantry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(infantry);
        infantry.AI.SetBehaviorWeight<BehaviorDefend>(defendWeight).DefensePosition = GetDefensivePosition(infantry);
        if (fallbackWeight > 0f)
        {
            infantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(fallbackWeight).AnchorFormation = infantry;
        }
    }

    private void ApplyArchers(float screenedWeight, float skirmishWeight)
    {
        ApplyArcherFormation(_archers, screenedWeight, skirmishWeight);
        ApplyArcherFormation(_supportArchers, screenedWeight * 0.9f, skirmishWeight);

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
        }
    }

    private void ApplyArcherFormation(Formation? archers, float screenedWeight, float skirmishWeight)
    {
        if (archers == null)
        {
            return;
        }

        archers.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(archers);
        archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(screenedWeight);
        archers.AI.SetBehaviorWeight<BehaviorSkirmish>(skirmishWeight);
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
