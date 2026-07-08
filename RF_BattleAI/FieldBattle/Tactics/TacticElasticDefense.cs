using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticElasticDefense : TacticComponent
{
    private enum ElasticDefenseProfile
    {
        InfantryHeavy,
        RangedHeavy,
        MountedHeavy,
        Balanced
    }

    private Formation? _supportInfantry;

    private int _cachedAiControlledFormationCount;
    private readonly HysteresisGate _yieldPowerGate = HysteresisGate.FallsBelow(0.82f);
    private bool? _lastFallbackState;
    private ElasticDefenseProfile? _lastProfile;

    public TacticElasticDefense(Team team)
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

    private bool ShouldYieldGround()
    {
        if (_mainInfantry?.CachedClosestEnemyFormation == null)
        {
            return false;
        }

        float distanceSquared = _mainInfantry.CachedMedianPosition.AsVec2.DistanceSquared(
            _mainInfantry.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);

        return distanceSquared < 900f || _yieldPowerGate.Evaluate(base.Team.QuerySystem.RemainingPowerRatio);
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

    private float GetEngagementDistanceSquared()
    {
        Formation? anchor = _mainInfantry ?? _supportInfantry ?? _archers ?? _leftCavalry ?? _rightCavalry ?? _rangedCavalry;
        if (anchor?.CachedClosestEnemyFormation == null)
        {
            return float.MaxValue;
        }

        return anchor.CachedMedianPosition.AsVec2.DistanceSquared(anchor.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
    }

    private bool ShouldBraceForImpact()
    {
        float distanceSquared = GetEngagementDistanceSquared();
        float enemyMountedRatio = base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio;

        if (distanceSquared <= 1225f)
        {
            return true;
        }

        return enemyMountedRatio > 0.2f && distanceSquared <= 2025f;
    }

    private static void ApplyInfantryArrangement(Formation formation, bool braceForImpact)
    {
        ArrangementOrder arrangement = braceForImpact && formation.QuerySystem.HasShield
            ? ArrangementOrder.ArrangementOrderShieldWall
            : ArrangementOrder.ArrangementOrderLine;

        if (formation.ArrangementOrder != arrangement)
        {
            formation.SetArrangementOrder(arrangement);
        }
    }

    private ElasticDefenseProfile ResolveProfile()
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(base.Team);
        if (composition.RangedRatio >= 0.38f && composition.RangedRatio > composition.InfantryRatio)
        {
            return ElasticDefenseProfile.RangedHeavy;
        }

        if (composition.MountedRatio >= 0.42f && composition.MountedRatio > composition.InfantryRatio)
        {
            return ElasticDefenseProfile.MountedHeavy;
        }

        if (composition.InfantryRatio >= 0.45f)
        {
            return ElasticDefenseProfile.InfantryHeavy;
        }

        return ElasticDefenseProfile.Balanced;
    }

    private void ApplyHoldLine()
    {
        ElasticDefenseProfile profile = ResolveProfile();
        switch (profile)
        {
            case ElasticDefenseProfile.RangedHeavy:
                ApplyRangedHeavyHoldLine();
                return;
            case ElasticDefenseProfile.MountedHeavy:
                ApplyMountedHeavyHoldLine();
                return;
            case ElasticDefenseProfile.InfantryHeavy:
                ApplyInfantryHeavyHoldLine();
                return;
            default:
                ApplyBalancedHoldLine();
                return;
        }
    }

    private void ApplyBalancedHoldLine()
    {
        bool braceForImpact = ShouldBraceForImpact();

        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, braceForImpact);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.5f).DefensePosition = GetDefensivePosition(_mainInfantry);
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, braceForImpact);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            if (_archers != null)
            {
                BehaviorScreenArchers screenBehavior = _supportInfantry.AI.SetBehaviorWeight<BehaviorScreenArchers>(2f);
                screenBehavior.ProtectedFormation = _archers;
            }
            else if (_mainInfantry != null)
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.35f).AnchorFormation = _mainInfantry;
            }
            else
            {
                _supportInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).DefensePosition = GetDefensivePosition(_supportInfantry);
            }
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

    private void ApplyInfantryHeavyHoldLine()
    {
        ApplyBalancedHoldLine();
    }

    private void ApplyRangedHeavyHoldLine()
    {
        bool braceForImpact = ShouldBraceForImpact();

        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, braceForImpact);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            if (_archers != null)
            {
                BehaviorScreenArchers screenBehavior = _mainInfantry.AI.SetBehaviorWeight<BehaviorScreenArchers>(2.2f);
                screenBehavior.ProtectedFormation = _archers;
                screenBehavior.ScreenDistance = 8f;
            }
            else
            {
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.4f).DefensePosition = GetDefensivePosition(_mainInfantry);
            }
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, braceForImpact);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            if (_mainInfantry != null)
            {
                BehaviorMaintainReserve reserveBehavior = _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.65f);
                reserveBehavior.AnchorFormation = _mainInfantry;
                reserveBehavior.DesiredDistance = 12f;
            }
            else if (_archers != null)
            {
                BehaviorScreenArchers screenBehavior = _supportInfantry.AI.SetBehaviorWeight<BehaviorScreenArchers>(1.7f);
                screenBehavior.ProtectedFormation = _archers;
                screenBehavior.ScreenDistance = 10f;
            }
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorDefend>(1.4f).DefensePosition = GetDefensivePosition(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0.35f);
        }

        ApplyCavalryElasticSupport(protectWeight: 0.95f, rangedSkirmishWeight: 1f);
    }

    private void ApplyMountedHeavyHoldLine()
    {
        bool braceForImpact = ShouldBraceForImpact();

        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, braceForImpact);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1.2f).DefensePosition = GetDefensivePosition(_mainInfantry);
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, braceForImpact);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.55f).AnchorFormation = _mainInfantry ?? _supportInfantry;
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0.85f);
        }

        ApplyCavalryElasticSupport(protectWeight: 1.3f, rangedSkirmishWeight: 1.1f);
    }

    private void ApplyCavalryElasticSupport(float protectWeight, float rangedSkirmishWeight)
    {
        if (_leftCavalry != null)
        {
            _leftCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_leftCavalry);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(protectWeight).FlankSide = FormationAI.BehaviorSide.Left;
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rightCavalry);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(protectWeight).FlankSide = FormationAI.BehaviorSide.Right;
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(rangedSkirmishWeight);
        }
    }

    private void ApplyFallbackLine()
    {
        ElasticDefenseProfile profile = ResolveProfile();
        if (profile == ElasticDefenseProfile.RangedHeavy)
        {
            ApplyRangedHeavyFallbackLine();
            return;
        }

        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, braceForImpact: false);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(2f).AnchorFormation = _mainInfantry;
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, braceForImpact: false);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(1.5f).AnchorFormation = _mainInfantry;
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

    private void ApplyRangedHeavyFallbackLine()
    {
        if (_mainInfantry != null)
        {
            ApplyInfantryArrangement(_mainInfantry, braceForImpact: false);
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            if (_archers != null)
            {
                BehaviorScreenArchers screenBehavior = _mainInfantry.AI.SetBehaviorWeight<BehaviorScreenArchers>(2.3f);
                screenBehavior.ProtectedFormation = _archers;
                screenBehavior.ScreenDistance = 7f;
            }
            _mainInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(0.7f).AnchorFormation = _mainInfantry;
        }

        if (_supportInfantry != null)
        {
            ApplyInfantryArrangement(_supportInfantry, braceForImpact: false);
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            if (_mainInfantry != null)
            {
                BehaviorMaintainReserve reserveBehavior = _supportInfantry.AI.SetBehaviorWeight<BehaviorMaintainReserve>(1.6f);
                reserveBehavior.AnchorFormation = _mainInfantry;
                reserveBehavior.DesiredDistance = 10f;
            }
            _supportInfantry.AI.SetBehaviorWeight<BehaviorFallbackLine>(0.8f).AnchorFormation = _mainInfantry ?? _supportInfantry;
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorDefend>(1.25f).DefensePosition = GetDefensivePosition(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0.45f);
        }

        ApplyCavalryElasticSupport(protectWeight: 0.9f, rangedSkirmishWeight: 1f);
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

        bool fallback = ShouldYieldGround();
        ElasticDefenseProfile profile = ResolveProfile();
        if (_lastFallbackState != fallback || _lastProfile != profile)
        {
            float distance = _mainInfantry?.CachedClosestEnemyFormation == null
                ? float.MaxValue
                : _mainInfantry.CachedMedianPosition.AsVec2.Distance(
                    _mainInfantry.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
            BattleAIDebug.TacticStateChanged(
                base.Team,
                nameof(TacticElasticDefense),
                $"{profile}_{(fallback ? "FallbackLine" : "HoldLine")}",
                $"distance={distance:F1} power={base.Team.QuerySystem.RemainingPowerRatio:F2}");
            _lastFallbackState = fallback;
            _lastProfile = profile;
        }

        if (fallback)
        {
            ApplyFallbackLine();
        }
        else
        {
            ApplyHoldLine();
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        float powerRatio = base.Team.QuerySystem.RemainingPowerRatio;
        float enemyMountedRatio = base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio;
        float weight = 0.1f;
        if (base.Team.Side == BattleSideEnum.Defender)
        {
            weight += 0.18f;
        }

        if (base.Team.QuerySystem.InfantryRatio > 0.3f)
        {
            weight += 0.15f;
        }

        if (base.Team.QuerySystem.RangedRatio > 0.15f)
        {
            weight += 0.12f;
        }

        if (enemyMountedRatio > 0.2f)
        {
            weight += 0.12f;
        }

        if (powerRatio < 0.95f)
        {
            weight += 0.25f;
        }

        if (powerRatio < 0.8f)
        {
            weight += 0.15f;
        }

        return BattleAITacticController.ApplyManualWeightBonus(base.Team, nameof(TacticElasticDefense), weight);
    }
}
