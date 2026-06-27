using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticBanditAdaptiveSkirmish : TacticComponent
{
    private enum BanditBattleState
    {
        CautiousSkirmish,
        CorneredStand,
        HarassingAdvance,
        AggressiveRush
    }

    private readonly bool _isBanditTeam;
    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private BanditBattleState? _lastState;
    private bool _favorLeftHarassment;
    private bool _hasAppliedMinorityInfantrySplit;

    public TacticBanditAdaptiveSkirmish(Team team)
        : base(team)
    {
        _isBanditTeam = BattleAICombatantHelper.IsBanditTeam(team);
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

        ApplyMinorityInfantrySplit();
        RefreshInfantryAssignments();
        _favorLeftHarassment = SelectHarassmentSide();
    }

    protected override bool CheckAndSetAvailableFormationsChanged()
    {
        int currentAiFormationCount = base.Team.GetAIControlledFormationCount();
        if (currentAiFormationCount != _cachedAiControlledFormationCount)
        {
            _cachedAiControlledFormationCount = currentAiFormationCount;
            _hasAppliedMinorityInfantrySplit = false;
            IsTacticReapplyNeeded = true;
            return true;
        }

        if (_mainInfantry == null && _archers == null)
        {
            return true;
        }

        if (_mainInfantry != null && (_mainInfantry.CountOfUnits <= 0 || !_mainInfantry.QuerySystem.IsInfantryFormation))
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

        BanditBattleState state = EvaluateState();
        if (_lastState != state)
        {
            BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticBanditAdaptiveSkirmish), state.ToString());
            _lastState = state;
        }

        switch (state)
        {
            case BanditBattleState.CautiousSkirmish:
                ApplyCautiousSkirmish();
                break;
            case BanditBattleState.CorneredStand:
                ApplyCorneredStand();
                break;
            case BanditBattleState.AggressiveRush:
                ApplyAggressiveRush();
                break;
            default:
                ApplyHarassingAdvance();
                break;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        if (!_isBanditTeam)
        {
            return 0f;
        }

        float weight = 0.75f;
        if (base.Team.QuerySystem.RangedRatio + base.Team.QuerySystem.RangedCavalryRatio > 0.15f)
        {
            weight += 0.15f;
        }

        if (base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio > 0.1f)
        {
            weight += 0.1f;
        }

        if (base.Team.QuerySystem.RemainingPowerRatio < 1f)
        {
            weight += 0.1f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticBanditAdaptiveSkirmish), weight);
    }

    private BanditBattleState EvaluateState()
    {
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;
        float engagementDistanceSquared = GetEngagementDistanceSquared();
        bool isCornered = engagementDistanceSquared < 625f || (engagementDistanceSquared < 1225f && powerRatio < 1.05f);

        if (isCornered)
        {
            return BanditBattleState.CorneredStand;
        }

        if (powerRatio <= 0.9f)
        {
            return BanditBattleState.CautiousSkirmish;
        }

        if (powerRatio >= 1.3f || (powerRatio >= 1.1f && engagementDistanceSquared < 2500f))
        {
            return BanditBattleState.AggressiveRush;
        }

        return BanditBattleState.HarassingAdvance;
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

    private void ApplyCautiousSkirmish()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            BehaviorFallbackLine fallback = _mainInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(2f);
            fallback.AnchorFormation = _mainInfantry;
            fallback.FallbackDistance = 26f;
            fallback.FlankSide = _supportInfantry != null ? FormationAI.BehaviorSide.Left : (_favorLeftHarassment ? FormationAI.BehaviorSide.Left : FormationAI.BehaviorSide.Right);
            fallback.LateralOffset = _supportInfantry != null ? 28f : 18f;
            _mainInfantry.AI.Side = fallback.FlankSide;
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            BehaviorFallbackLine fallback = _supportInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(1.8f);
            fallback.AnchorFormation = _supportInfantry;
            fallback.FallbackDistance = 24f;
            fallback.FlankSide = FormationAI.BehaviorSide.Right;
            fallback.LateralOffset = 28f;
            _supportInfantry.AI.Side = FormationAI.BehaviorSide.Right;
        }

        ApplySkirmisherBackline();
        ApplyCautiousSplitHarassment();
    }

    private void ApplyMinorityInfantrySplit()
    {
        if (_hasAppliedMinorityInfantrySplit || base.Team.QuerySystem.RemainingPowerRatio > 0.9f || _mainInfantry == null)
        {
            return;
        }

        Formation? splitTarget = _supportInfantry ?? FindEmptyAiControlledFormation(_mainInfantry);
        if (splitTarget == null)
        {
            return;
        }

        int totalInfantry = _mainInfantry.CountOfUnits + (_supportInfantry?.CountOfUnits ?? 0);
        if (totalInfantry < 2)
        {
            return;
        }

        int mainCount = totalInfantry / 2;
        int supportCount = totalInfantry - mainCount;
        if (mainCount <= 0 || supportCount <= 0)
        {
            return;
        }

        List<(Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents)> transferData = new()
        {
            (_mainInfantry, mainCount, TroopTraitsMask.Melee | TroopTraitsMask.Shield | TroopTraitsMask.Spear, new List<Agent>()),
            (splitTarget, supportCount, TroopTraitsMask.Melee | TroopTraitsMask.Shield | TroopTraitsMask.Spear, new List<Agent>())
        };

        base.Team.RearrangeFormationsAccordingToFilter(transferData);
        _hasAppliedMinorityInfantrySplit = true;
        BattleAIDebug.TacticStateChanged(base.Team, nameof(TacticBanditAdaptiveSkirmish), "MinoritySplit", $"main={mainCount} support={supportCount}");
    }

    private void RefreshInfantryAssignments()
    {
        List<Formation> infantryFormations = ChooseAndSortByPriority(
            FormationsIncludingEmpty,
            f => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation,
            f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower);

        _mainInfantry = infantryFormations.FirstOrDefault();
        _supportInfantry = infantryFormations.Skip(1).FirstOrDefault();

        if (_mainInfantry != null)
        {
            _mainInfantry.AI.IsMainFormation = true;
            _mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
        }
    }

    private Formation? FindEmptyAiControlledFormation(Formation excludedFormation)
    {
        return FormationsIncludingEmpty.FirstOrDefault(f => f != excludedFormation && f.IsAIControlled && f.CountOfUnits == 0);
    }

    private void ApplyHarassingAdvance()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.8f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).DefensePosition = _mainInfantry.CachedMedianPosition;
        }

        if (_supportInfantry != null)
        {
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

        ApplySkirmisherBackline();
        ApplyFlankHarassment();
    }

    private void ApplyCorneredStand()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.1f).DefensePosition = _mainInfantry.CachedMedianPosition;
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.2f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(0.9f);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.1f);
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0.8f);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(0.8f);
        }

        if (_leftCavalry != null)
        {
            _leftCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_leftCavalry);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.8f);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rightCavalry);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.8f);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(0.8f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(0.8f);
        }
    }

    private void ApplyAggressiveRush()
    {
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1.1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.35f);
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.2f);
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

    private void ApplySkirmisherBackline()
    {
        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1.1f);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(0.7f);
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
        }
    }

    private void ApplyFlankProtection()
    {
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
    }

    private void ApplyFlankHarassment()
    {
        if (_leftCavalry != null)
        {
            _leftCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_leftCavalry);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.9f);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(0.7f).FlankSide = FormationAI.BehaviorSide.Left;
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rightCavalry);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.9f);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(0.7f).FlankSide = FormationAI.BehaviorSide.Right;
        }
    }

    private void ApplyCautiousSplitHarassment()
    {
        if (_leftCavalry != null && _rightCavalry != null)
        {
            ApplyRearHarassReposition(_leftCavalry, FormationAI.BehaviorSide.Left);
            ApplyRearHarassReposition(_rightCavalry, FormationAI.BehaviorSide.Right);
            return;
        }

        if (_favorLeftHarassment)
        {
            ApplyRearHarassReposition(_leftCavalry, FormationAI.BehaviorSide.Left);
            ApplyFlankProtector(_rightCavalry, FormationAI.BehaviorSide.Right);
            return;
        }

        ApplyRearHarassReposition(_rightCavalry, FormationAI.BehaviorSide.Right);
        ApplyFlankProtector(_leftCavalry, FormationAI.BehaviorSide.Left);
    }

    private void ApplyRearHarassReposition(Formation? cavalry, FormationAI.BehaviorSide side)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(cavalry);

        BehaviorCavalryReposition reposition = cavalry.AI.SetBehaviorWeight<BehaviorCavalryReposition>(1.8f);
        reposition.AnchorFormation = _mainInfantry ?? _supportInfantry ?? cavalry;
        reposition.FlankSide = side;
        reposition.ForwardOffset = 12f;
        reposition.LateralDistance = 50f;
        reposition.OuterArcLateralDistance = 60f;
        reposition.OuterArcRearOffset = 16f;

        cavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.9f);
        cavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(0.45f);
    }

    private void ApplyFlankProtector(Formation? cavalry, FormationAI.BehaviorSide side)
    {
        if (cavalry == null)
        {
            return;
        }

        cavalry.AI.ResetBehaviorWeights();
        SetDefaultBehaviorWeights(cavalry);
        cavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = side;
        cavalry.AI.SetBehaviorWeight<BehaviorFlank>(0.45f);
    }

    private bool SelectHarassmentSide()
    {
        if (_leftCavalry == null)
        {
            return false;
        }

        if (_rightCavalry == null)
        {
            return true;
        }

        return _leftCavalry.QuerySystem.FormationPower >= _rightCavalry.QuerySystem.FormationPower;
    }
}
