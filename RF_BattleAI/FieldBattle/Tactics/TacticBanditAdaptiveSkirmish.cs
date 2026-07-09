using RF_BattleAI.FieldBattle.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Tactics;

public sealed class TacticBanditAdaptiveSkirmish : TacticComponent
{
    private enum BanditBattleState
    {
        CautiousSkirmish,
        CorneredStand,
        HarassingAdvance,
        AggressiveRush,
        SpringTheTrap,
        MountedOnslaught
    }

    private const float TrapEngageDistance = 75f;
    private const float ChaseCommitDistance = 90f;
    private const float TrapCommitDuration = 20f;
    private const float TrapPowerGate = 0.9f;
    // A prey is any isolated enemy fragment WEAKER than the attacking group —
    // 0.7 was so strict the opportunity never fired in real battles.
    private const float PreyPowerRatio = 1.0f;
    private const float PreyMaxDistance = 300f;
    private const float PreySafeDiversionRatio = 0.8f;
    private const float PreyThreatFarDistance = 150f;
    private const float PreyIsolationDistance = 60f;
    private const float EnemyClusterRadius = 40f;
    // Anti-pursuer sensor: formation medians hide the handful of enemy AGENTS
    // that run ahead of a big blob. When stragglers are on a group's heels but
    // the nearest enemy FORMATION median is still far, the pursuers are alone
    // and the group turns and eats them if the fight is winnable.
    private const float PursuerSenseRadius = 75f;
    private const float PursuerMainBodyFarDistance = 150f;
    private const float PursuerParityRatio = 1.05f;

    private readonly bool _isBanditTeam;
    private Formation? _supportInfantry;
    private int _cachedAiControlledFormationCount;
    private BanditBattleState? _lastState;
    private bool _favorLeftHarassment;
    private bool _hasAppliedMinorityInfantrySplit;
    private float _trapSprungUntil = float.MinValue;
    private Formation? _trapChaser;
    private Formation? _lastChaser;
    private string _lastSupportDecision = "none";
    private float _lastChaserToBait = float.MaxValue;
    private bool _lastPowerFavorable;
    private bool _supportIsBait;
    private string _lastBaitDecision = "none";

    private string RolePrefix()
    {
        return _supportIsBait ? "[ROLES-SWAPPED] " : "";
    }
    private readonly HysteresisGate _weakGate = HysteresisGate.FallsBelow(0.9f);
    private readonly HysteresisGate _rushGate = HysteresisGate.RisesAbove(1.3f);
    private readonly HysteresisGate _nearRushGate = HysteresisGate.RisesAbove(1.1f);
    private readonly HysteresisGate _mainSwatGate = HysteresisGate.RisesAbove(PursuerParityRatio, 0.15f);
    private readonly HysteresisGate _supportSwatGate = HysteresisGate.RisesAbove(PursuerParityRatio, 0.15f);
    // A DOMINANT force never cowers: while this gate is open, CorneredStand is
    // suppressed (telemetry showed superior all-mounted bandits ending the
    // battle in a defensive ball at the enemy's corner).
    private readonly HysteresisGate _dominantGate = HysteresisGate.RisesAbove(1.1f, 0.1f);
    private readonly MBList<Agent> _pursuerBuffer = new();
    private float _mainPursuerPower;
    private int _mainPursuerCount;
    private float _supportPursuerPower;
    private int _supportPursuerCount;

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

        UpdatePursuerSensors();
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
            case BanditBattleState.SpringTheTrap:
                ApplySpringTheTrap();
                break;
            case BanditBattleState.MountedOnslaught:
                ApplyMountedOnslaught();
                break;
            default:
                ApplyHarassingAdvance();
                break;
        }

        BanditTrapTelemetry.LogDecision(
            base.Team, state.ToString(), _mainInfantry, _supportInfantry,
            state == BanditBattleState.SpringTheTrap ? _trapChaser : _lastChaser,
            $"bait={_lastBaitDecision} striker={_lastSupportDecision}",
            _lastChaserToBait, _lastPowerFavorable,
            $"@{PursuerSenseRadius:F0}m bait n={_mainPursuerCount} pow={_mainPursuerPower:F0} | support n={_supportPursuerCount} pow={_supportPursuerPower:F0}");

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
        bool isDominant = _dominantGate.Evaluate(powerRatio);
        bool isCornered = engagementDistanceSquared < 625f || (engagementDistanceSquared < 1225f && powerRatio < 1.05f);

        // Superior bandits at contact range are PRESSING, not cornered —
        // CorneredStand is for the weak who got caught.
        if (isCornered && !isDominant)
        {
            _trapSprungUntil = float.MinValue;
            return BanditBattleState.CorneredStand;
        }

        float currentTime = Mission.Current?.CurrentTime ?? 0f;
        if (currentTime < _trapSprungUntil)
        {
            return BanditBattleState.SpringTheTrap;
        }

        if (_weakGate.Evaluate(powerRatio))
        {
            // The bait only works if it eventually snaps shut: once a striker
            // sits behind the pursuing enemy, turn and strike from both sides.
            if (IsTrapReady())
            {
                _trapSprungUntil = currentTime + TrapCommitDuration;
                return BanditBattleState.SpringTheTrap;
            }

            // Telemetry showed the old withdraw cap forced half the battle into
            // a standing CorneredStand — with continuous flight the ONLY reason
            // to stand is being genuinely caught (isCornered above, which also
            // covers being pressed against the map edge).
            return BanditBattleState.CautiousSkirmish;
        }

        if (_rushGate.Evaluate(powerRatio) || (_nearRushGate.Evaluate(powerRatio) && engagementDistanceSquared < 2500f))
        {
            // Mounted-majority bands fight with open-side geometry (firing
            // arc + charge waves) instead of the generic rush weights.
            return IsMountedDoctrineTeam() ? BanditBattleState.MountedOnslaught : BanditBattleState.AggressiveRush;
        }

        if (isDominant && IsMountedDoctrineTeam())
        {
            return BanditBattleState.MountedOnslaught;
        }

        return BanditBattleState.HarassingAdvance;
    }

    /// <summary>Mounted doctrine applies when the band is mostly on horseback
    /// — or has NO infantry at all, in which case every infantry-centric rule
    /// (bait, pincer, prey) is a no-op anyway.</summary>
    private bool IsMountedDoctrineTeam()
    {
        float mountedRatio = base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio;
        return mountedRatio > 0.5f || (_mainInfantry == null && _supportInfantry == null);
    }

    private bool IsTrapReady()
    {
        // Roles are dynamic: the bait is whichever group the enemy is hunting.
        Formation? bait = _supportIsBait
            ? _supportInfantry ?? _mainInfantry
            : _mainInfantry ?? _supportInfantry;
        if (bait == null)
        {
            return false;
        }

        // The trap is reasoned against the FORMATION ACTUALLY CHASING THE BAIT
        // — never against "whatever is closest to whoever".
        Formation? enemy = FindNearestEnemyTo(bait, EnumerateEnemyFormations());
        if (enemy == null)
        {
            return false;
        }

        Vec2 baitPosition = bait.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = enemy.CachedMedianPosition.AsVec2;
        if (!baitPosition.IsValid || !enemyPosition.IsValid)
        {
            return false;
        }

        // The trap springs when the chaser has COMMITTED to the bait, the
        // fight is winnable (inferior never attacks superior), and a striker
        // is within reach outside the chaser's front cone.
        if (baitPosition.Distance(enemyPosition) > TrapEngageDistance
            || !IsTrapPowerFavorable(enemy, EnumerateEnemyFormations()))
        {
            return false;
        }

        Vec2 towardBait = (baitPosition - enemyPosition).Normalized();
        foreach (Formation? pincer in new[] { _leftCavalry, _rightCavalry, _mainInfantry, _supportInfantry })
        {
            if (pincer == null || pincer == bait || pincer.CountOfUnits <= 0)
            {
                continue;
            }

            Vec2 pincerPosition = pincer.CachedMedianPosition.AsVec2;
            if (!pincerPosition.IsValid)
            {
                continue;
            }

            // Infantry must strike from truly BEHIND (rear hemisphere and
            // close); cavalry is fast enough to be allowed a looser arc.
            bool isCavalry = pincer.QuerySystem.IsCavalryFormation || pincer.QuerySystem.IsRangedCavalryFormation;
            float maxDistance = isCavalry ? 130f : 110f;
            float coneDot = isCavalry ? 0.3f : -0.1f;

            if (pincerPosition.Distance(enemyPosition) > maxDistance)
            {
                continue;
            }

            Vec2 towardPincer = (pincerPosition - enemyPosition).Normalized();
            if (towardBait.IsValid && towardPincer.IsValid
                && Vec2.DotProduct(towardPincer, towardBait) < coneDot)
            {
                _trapChaser = enemy;
                return true;
            }
        }

        return false;
    }

    private void ApplySpringTheTrap()
    {
        // Every striker charges the SPECIFIC formation that took the bait —
        // MovementOrderChargeToTarget guarantees nobody picks a different
        // (bigger) enemy on their own.
        if (_trapChaser == null || _trapChaser.CountOfUnits <= 0)
        {
            _trapSprungUntil = float.MinValue;
            ApplyCautiousSkirmish();
            return;
        }

        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorDirectedCharge>(2f).TargetEnemyFormation = _trapChaser;
        }

        if (_supportInfantry != null)
        {
            _supportInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_supportInfantry);
            _supportInfantry.AI.SetBehaviorWeight<BehaviorDirectedCharge>(2f).TargetEnemyFormation = _trapChaser;
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
        }

        foreach (Formation? cavalry in new[] { _leftCavalry, _rightCavalry })
        {
            if (cavalry == null)
            {
                continue;
            }

            cavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(cavalry);
            cavalry.AI.SetBehaviorWeight<BehaviorDirectedCharge>(1.6f).TargetEnemyFormation = _trapChaser;
        }

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
        }
    }

    /// <summary>
    /// Offensive doctrine for mounted-majority bands in the advantage. All
    /// geometry hangs off the OPEN-SIDE BEARING — the direction from the
    /// target toward our own force, by construction free of the rocks and map
    /// edges the enemy is pinned against: horse archers sweep a firing arc on
    /// that side, melee cavalry wings stage on two lanes and charge in waves,
    /// foot troops (if any) press behind. Exclusive executor weights — the
    /// telemetry precedent showed vanilla behaviors outbidding assigned ones.
    /// </summary>
    private void ApplyMountedOnslaught()
    {
        List<Formation> enemies = EnumerateEnemyFormations();
        Formation? anchor = _rangedCavalry ?? _leftCavalry ?? _rightCavalry ?? _mainInfantry ?? _archers;
        Formation? target = anchor != null ? FindNearestEnemyTo(anchor, enemies) : null;
        if (target == null)
        {
            ApplyAggressiveRush();
            return;
        }

        Vec2 targetPosition = target.CachedMedianPosition.AsVec2;
        Vec2 ownPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 openDirection = (ownPosition - targetPosition).Normalized();
        if (!openDirection.IsValid || openDirection.LengthSquared < 0.01f)
        {
            openDirection = Vec2.FromRotation(0f);
        }

        _lastBaitDecision = $"onslaught:F{(int)target.FormationIndex}";
        _lastSupportDecision = "onslaught";

        if (_rangedCavalry != null)
        {
            _rangedCavalry.AI.ResetBehaviorWeights();
            BehaviorMountedFiringArc arc = _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedFiringArc>(10f);
            arc.TargetEnemyFormation = target;
            arc.OpenSideDirection = openDirection;
            arc.PreferFullOrbit = true;
            arc.SweepSpeed = 0.38f;
        }

        foreach (Formation? cavalry in new[] { _leftCavalry, _rightCavalry })
        {
            if (cavalry == null)
            {
                continue;
            }

            cavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(cavalry);
            cavalry.AI.SetBehaviorWeight<BehaviorFlank>(1.1f);
            cavalry.AI.SetBehaviorWeight<BehaviorDirectedCharge>(1.6f).TargetEnemyFormation = target;
        }

        // Foot troops keep conventional pressure so the mounted arms have an
        // anvil to work around.
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1.1f);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1.3f);
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
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1.1f);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(0.8f);
        }
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
        FormationAI.BehaviorSide baitSide = _favorLeftHarassment ? FormationAI.BehaviorSide.Left : FormationAI.BehaviorSide.Right;
        FormationAI.BehaviorSide pincerSide = _favorLeftHarassment ? FormationAI.BehaviorSide.Right : FormationAI.BehaviorSide.Left;

        // The tactic is the brain: it sees every enemy formation and assigns
        // explicit roles/targets each tick. Behaviors are dumb executors.
        List<Formation> enemies = EnumerateEnemyFormations();

        // ROLES ARE DYNAMIC: whichever group the enemy actually hunts plays
        // bait, the other becomes the striker. The player choosing to chase
        // the "support" simply swaps the roles.
        Formation? chaserOfMain = _mainInfantry != null ? FindNearestEnemyTo(_mainInfantry, enemies) : null;
        Formation? chaserOfSupport = _supportInfantry != null ? FindNearestEnemyTo(_supportInfantry, enemies) : null;
        float enemyToMain = _mainInfantry != null && chaserOfMain != null
            ? chaserOfMain.CachedMedianPosition.AsVec2.Distance(_mainInfantry.CachedMedianPosition.AsVec2)
            : float.MaxValue;
        float enemyToSupport = _supportInfantry != null && chaserOfSupport != null
            ? chaserOfSupport.CachedMedianPosition.AsVec2.Distance(_supportInfantry.CachedMedianPosition.AsVec2)
            : float.MaxValue;

        _supportIsBait = _supportInfantry != null && enemyToSupport < enemyToMain;
        Formation? baitGroup = _supportIsBait ? _supportInfantry : _mainInfantry;
        Formation? strikerGroup = _supportIsBait ? _mainInfantry : _supportInfantry;
        FormationAI.BehaviorSide baitGroupSide = _supportIsBait ? pincerSide : baitSide;
        FormationAI.BehaviorSide strikerGroupSide = _supportIsBait ? baitSide : pincerSide;
        Formation? baitChaser = _supportIsBait ? chaserOfSupport : chaserOfMain;

        // ANTI-PURSUER: stragglers within PursuerSenseRadius while the nearest
        // enemy FORMATION median is far mean a handful of agents outran their
        // army. If the two bandit groups together outweigh them (inferior never
        // attacks superior), the hunted group turns and fights, and when it
        // cannot win alone the other group converges to gang up in sync.
        float baitPursuerPower = _supportIsBait ? _supportPursuerPower : _mainPursuerPower;
        int baitPursuerCount = _supportIsBait ? _supportPursuerCount : _mainPursuerCount;
        float strikerPursuerPower = _supportIsBait ? _mainPursuerPower : _supportPursuerPower;
        int strikerPursuerCount = _supportIsBait ? _mainPursuerCount : _supportPursuerCount;
        HysteresisGate baitSwatGate = _supportIsBait ? _supportSwatGate : _mainSwatGate;
        HysteresisGate strikerSwatGate = _supportIsBait ? _mainSwatGate : _supportSwatGate;
        float baitPower = baitGroup?.QuerySystem.FormationPower ?? 0f;
        float strikerPower = strikerGroup?.QuerySystem.FormationPower ?? 0f;
        bool baitMainBodyFar = (_supportIsBait ? enemyToSupport : enemyToMain) > PursuerMainBodyFarDistance;
        bool strikerMainBodyFar = (_supportIsBait ? enemyToMain : enemyToSupport) > PursuerMainBodyFarDistance;

        bool baitSwats = baitGroup != null && baitPursuerCount > 0 && baitMainBodyFar
            && baitSwatGate.Evaluate((baitPower + strikerPower) / MathF.Max(baitPursuerPower, 0.01f));
        bool strikerSwats = strikerGroup != null && strikerPursuerCount > 0 && strikerMainBodyFar
            && strikerSwatGate.Evaluate((strikerPower + baitPower) / MathF.Max(strikerPursuerPower, 0.01f));
        bool baitNeedsHelp = baitSwats && baitPower < baitPursuerPower * PursuerParityRatio;
        bool strikerNeedsHelp = strikerSwats && strikerPower < strikerPursuerPower * PursuerParityRatio;

        if (baitGroup != null)
        {
            // Telemetry proved vanilla behaviors (Charge/Defend/PullBack) were
            // winning the weight competition and hijacking the bait — so no
            // SetDefaultBehaviorWeights here: after the reset, ONLY the assigned
            // executor has weight, and it always wins.
            baitGroup.AI.ResetBehaviorWeights();

            // Even the hunted group is an opportunist: if the enemy split off a
            // weaker isolated fragment right next to us, stop fleeing and eat it.
            Formation? baitPrey = FindIsolatedPrey(baitGroup, enemies);
            if (baitSwats)
            {
                baitGroup.AI.SetBehaviorWeight<BehaviorSwatPursuers>(10f).AllyFormation = null;
                _lastBaitDecision = $"swatPursuers:n{baitPursuerCount}";
            }
            else if (strikerNeedsHelp && strikerGroup != null && baitPursuerCount == 0 && baitMainBodyFar)
            {
                baitGroup.AI.SetBehaviorWeight<BehaviorSwatPursuers>(10f).AllyFormation = strikerGroup;
                _lastBaitDecision = "convergeAid";
            }
            else if (baitPrey != null)
            {
                BehaviorDirectedCharge charge = baitGroup.AI.SetBehaviorWeight<BehaviorDirectedCharge>(10f);
                charge.TargetEnemyFormation = baitPrey;
                _lastBaitDecision = $"prey:F{(int)baitPrey.FormationIndex}";
            }
            else
            {
                BehaviorBaitLure lure = baitGroup.AI.SetBehaviorWeight<BehaviorBaitLure>(10f);
                lure.FlankSide = baitGroupSide;
                lure.TargetEnemyFormation = baitChaser;
                _lastBaitDecision = "flee";
            }

            baitGroup.AI.Side = baitGroupSide;
        }

        if (strikerGroup != null)
        {
            strikerGroup.AI.ResetBehaviorWeights();
            if (_leftCavalry == null && _rightCavalry == null && baitGroup != null)
            {
                Formation? prey = FindIsolatedPrey(strikerGroup, enemies);
                float chaserToBait = baitChaser != null
                    ? baitChaser.CachedMedianPosition.AsVec2.Distance(baitGroup.CachedMedianPosition.AsVec2)
                    : float.MaxValue;

                _lastPowerFavorable = baitChaser != null && IsTrapPowerFavorable(baitChaser, enemies);
                if (strikerSwats)
                {
                    strikerGroup.AI.SetBehaviorWeight<BehaviorSwatPursuers>(10f).AllyFormation = null;
                    _lastSupportDecision = $"{RolePrefix()}swatPursuers:n{strikerPursuerCount}";
                }
                else if (baitNeedsHelp && strikerPursuerCount == 0 && strikerMainBodyFar)
                {
                    strikerGroup.AI.SetBehaviorWeight<BehaviorSwatPursuers>(10f).AllyFormation = baitGroup;
                    _lastSupportDecision = $"{RolePrefix()}convergeAid";
                }
                else if (prey != null)
                {
                    // The enemy split off a weaker isolated fragment: charge THAT
                    // formation explicitly — never whatever happens to be nearest.
                    BehaviorDirectedCharge charge = strikerGroup.AI.SetBehaviorWeight<BehaviorDirectedCharge>(10f);
                    charge.TargetEnemyFormation = prey;
                    _lastSupportDecision = $"{RolePrefix()}prey:F{(int)prey.FormationIndex}";
                }
                else if (baitChaser != null && chaserToBait <= ChaseCommitDistance && _lastPowerFavorable)
                {
                    // The chaser committed to the bait and the fight is winnable:
                    // travel to the point directly behind THAT formation, around it.
                    BehaviorRearPincer pincer = strikerGroup.AI.SetBehaviorWeight<BehaviorRearPincer>(10f);
                    pincer.BaitFormation = baitGroup;
                    pincer.TargetEnemyFormation = baitChaser;
                    _lastSupportDecision = $"{RolePrefix()}rearApproach:F{(int)baitChaser.FormationIndex}";
                }
                else
                {
                    // Nothing convenient: keep fleeing to our own side of the field.
                    BehaviorBaitLure flee = strikerGroup.AI.SetBehaviorWeight<BehaviorBaitLure>(10f);
                    flee.FlankSide = strikerGroupSide;
                    flee.TargetEnemyFormation = FindNearestEnemyTo(strikerGroup, enemies);
                    _lastSupportDecision = $"{RolePrefix()}flee";
                }

                strikerGroup.AI.Side = strikerGroupSide;
                _lastChaser = baitChaser;
                _lastChaserToBait = chaserToBait;
            }
            else if (strikerSwats)
            {
                strikerGroup.AI.SetBehaviorWeight<BehaviorSwatPursuers>(10f).AllyFormation = null;
                strikerGroup.AI.Side = strikerGroupSide;
                _lastSupportDecision = $"{RolePrefix()}swatPursuers:n{strikerPursuerCount}";
            }
            else
            {
                // Cavalry handles the striking: the second infantry line also
                // flees continuously to its own side.
                BehaviorBaitLure flee = strikerGroup.AI.SetBehaviorWeight<BehaviorBaitLure>(10f);
                flee.FlankSide = strikerGroupSide;
                flee.TargetEnemyFormation = FindNearestEnemyTo(strikerGroup, enemies);
                strikerGroup.AI.Side = strikerGroupSide;
            }
        }

        ApplySkirmisherBackline();
        ApplyCautiousSplitHarassment();
    }

    private List<Formation> EnumerateEnemyFormations()
    {
        List<Formation> result = new();
        Mission? mission = Mission.Current;
        if (mission == null)
        {
            return result;
        }

        foreach (Team team in mission.Teams)
        {
            if (team == null || !team.IsEnemyOf(base.Team))
            {
                continue;
            }

            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    result.Add(formation);
                }
            }
        }

        return result;
    }

    private static Formation? FindNearestEnemyTo(Formation formation, List<Formation> enemies)
    {
        Vec2 position = formation.CachedMedianPosition.AsVec2;
        Formation? best = null;
        float bestDistance = float.MaxValue;
        foreach (Formation enemy in enemies)
        {
            float distance = enemy.CachedMedianPosition.AsVec2.Distance(position);
            if (distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static Formation? FindIsolatedPrey(Formation hunter, List<Formation> enemies)
    {
        Vec2 hunterPosition = hunter.CachedMedianPosition.AsVec2;
        float hunterPower = hunter.QuerySystem.FormationPower;
        Formation? best = null;
        float bestDistance = PreyMaxDistance;

        foreach (Formation candidate in enemies)
        {
            float distance = candidate.CachedMedianPosition.AsVec2.Distance(hunterPosition);
            if (distance > bestDistance
                || candidate.QuerySystem.FormationPower >= hunterPower * PreyPowerRatio)
            {
                continue;
            }

            // Isolation: no allied enemy formation close enough to rescue it.
            bool isolated = true;
            float nearestOtherThreat = float.MaxValue;
            foreach (Formation ally in enemies)
            {
                if (ally == candidate)
                {
                    continue;
                }

                if (ally.CachedMedianPosition.AsVec2.Distance(candidate.CachedMedianPosition.AsVec2) < PreyIsolationDistance)
                {
                    isolated = false;
                    break;
                }

                float allyToHunter = ally.CachedMedianPosition.AsVec2.Distance(hunterPosition);
                if (allyToHunter < nearestOtherThreat)
                {
                    nearestOtherThreat = allyToHunter;
                }
            }

            if (!isolated)
            {
                continue;
            }

            // Safe diversion: only abandon the flight if we reach the prey
            // clearly before the nearest OTHER enemy reaches us — fleeing
            // groups don't cross the map in front of the pursuing army.
            if (nearestOtherThreat < PreyThreatFarDistance
                && distance > nearestOtherThreat * PreySafeDiversionRatio)
            {
                continue;
            }

            best = candidate;
            bestDistance = distance;
        }

        return best;
    }

    private void UpdatePursuerSensors()
    {
        _mainPursuerPower = GetPursuerPower(_mainInfantry, out _mainPursuerCount);
        _supportPursuerPower = GetPursuerPower(_supportInfantry, out _supportPursuerCount);
    }

    private float GetPursuerPower(Formation? group, out int count)
    {
        count = 0;
        Mission? mission = Mission.Current;
        if (mission == null || group == null || group.CountOfUnits <= 0)
        {
            return 0f;
        }

        Vec2 median = group.CachedMedianPosition.AsVec2;
        if (!median.IsValid)
        {
            return 0f;
        }

        float power = 0f;
        foreach (Agent agent in mission.GetNearbyEnemyAgents(median, PursuerSenseRadius, base.Team, _pursuerBuffer))
        {
            if (agent.IsActive())
            {
                power += agent.CharacterPowerCached;
                count++;
            }
        }

        return power;
    }

    private bool IsTrapPowerFavorable(Formation chaser, List<Formation> enemies)
    {
        // Inferior never attacks superior: the bait plus every striker must
        // roughly match the chaser's whole CLUSTER — telemetry showed a 9-man
        // enemy formation marching inside an 82-man army fooling a per-
        // formation gate.
        float ourPower = (_mainInfantry?.QuerySystem.FormationPower ?? 0f)
            + (_supportInfantry?.QuerySystem.FormationPower ?? 0f)
            + (_leftCavalry?.QuerySystem.FormationPower ?? 0f)
            + (_rightCavalry?.QuerySystem.FormationPower ?? 0f);

        Vec2 chaserPosition = chaser.CachedMedianPosition.AsVec2;
        float clusterPower = 0f;
        foreach (Formation enemy in enemies)
        {
            if (enemy == chaser
                || enemy.CachedMedianPosition.AsVec2.Distance(chaserPosition) <= EnemyClusterRadius)
            {
                clusterPower += enemy.QuerySystem.FormationPower;
            }
        }

        return ourPower >= clusterPower * TrapPowerGate;
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
        reposition.ForwardOffset = 20f;
        reposition.LateralDistance = 85f;
        reposition.OuterArcLateralDistance = 100f;
        reposition.OuterArcRearOffset = 24f;

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
