using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

/// <summary>
/// Bait arm of the bandit trap. It keeps a stand-off distance from its chaser:
/// the flee point is anchored to the ENEMY at <see cref="StandoffDistance"/>, so
/// the bait gives ground only as the enemy advances and then HOLDS — it does not
/// sprint continuously off the map (the old behaviour, which scattered the two
/// bandit halves out of mutual support). The flight curves toward this group's
/// assigned side so the two groups fan to opposite flanks, but only as far as
/// the stand-off allows. The bait stops luring when the tactic springs the trap
/// (weights switch to fight) or orders the cornered stand.
/// </summary>
public sealed class BehaviorBaitLure : BehaviorComponent
{
    /// <summary>The enemy formation to flee from (set by the tactic — the
    /// formation actually chasing this group), with engine fallbacks.</summary>
    public Formation? TargetEnemyFormation { get; set; }

    /// <summary>Which side this group's flight curves toward.</summary>
    public FormationAI.BehaviorSide FlankSide { get; set; } = FormationAI.BehaviorSide.Left;

    /// <summary>The stand-off distance the bait tries to keep from its chaser.
    /// The flee point is anchored to the ENEMY at this range, not to the group's
    /// own moving median — so the bait recedes only until it is this far from the
    /// enemy and then HOLDS, instead of sprinting off the map. It gives ground
    /// gradually, in step with the enemy's advance, so the two bandit groups stay
    /// within supporting distance instead of scattering to opposite horizons.</summary>
    public float StandoffDistance { get; set; } = 90f;

    /// <summary>Hysteresis band so the bait does not micro-jitter right on the
    /// stand-off line: it recedes when closer than StandoffDistance and stops
    /// once back past StandoffDistance + this.</summary>
    public float StandoffHysteresis { get; set; } = 12f;

    /// <summary>Sideways curve of the flight, in radians (~34 degrees).</summary>
    public float LateralBiasRadians { get; set; } = 0.6f;

    private Vec2 _heldPosition = Vec2.Invalid;

    /// <summary>Set by the tactic when this fleeing group is taking arrows in
    /// the back. A tight LINE running away is a shooting gallery for archers;
    /// LOOSE spacing scatters the ranks so most shafts fall in the gaps — the
    /// same trick vanilla uses to keep skirmishers alive under fire.</summary>
    public bool UnderRangedFire { get; set; }

    public BehaviorBaitLure(Formation formation)
        : base(formation)
    {
    }

    protected override float GetAiWeight()
    {
        if (base.Formation.CountOfUnits <= 0)
        {
            return 0f;
        }

        return 1f;
    }

    public override TextObject GetBehaviorString()
    {
        return new TextObject("{=!}Bait lure");
    }

    protected override void CalculateCurrentOrder()
    {
        // Flee from the assigned chaser; fall back to the engine's threat-
        // filtered pick (what vanilla skirmishers use), then plain nearest.
        Formation? enemy = (TargetEnemyFormation != null && TargetEnemyFormation.CountOfUnits > 0
                ? TargetEnemyFormation
                : null)
            ?? base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation?.Formation
            ?? base.Formation.CachedClosestEnemyFormation?.Formation;
        Vec2 myPosition = base.Formation.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = enemy?.CachedMedianPosition.AsVec2
            ?? base.Formation.Team.QuerySystem.AverageEnemyPosition;
        if (!myPosition.IsValid || !enemyPosition.IsValid)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 awayFromEnemy = (myPosition - enemyPosition).Normalized();
        if (!awayFromEnemy.IsValid)
        {
            awayFromEnemy = (-base.Formation.Direction).Normalized();
        }

        // GRADUAL, PROXIMITY-RELATIVE FLIGHT. The old target was myPosition + a
        // fixed 45m vector recomputed every tick — a treadmill that sprinted the
        // group off the map even while the enemy was far away, scattering the two
        // bandit halves so they could never support each other. Instead, anchor
        // the flee point to the ENEMY at StandoffDistance: the bait gives ground
        // only to keep that gap and then HOLDS. When already at (or beyond) the
        // stand-off, it stands its ground and keeps luring rather than running.
        float distToEnemy = myPosition.Distance(enemyPosition);
        float biasSign = FlankSide == FormationAI.BehaviorSide.Left ? 1f : -1f;
        Mission? mission = Mission.Current;

        bool alreadyAtStandoff = _heldPosition.IsValid
            ? distToEnemy >= StandoffDistance
            : distToEnemy >= StandoffDistance + StandoffHysteresis;
        Vec2 targetPosition;
        if (alreadyAtStandoff)
        {
            // Far enough: hold the current spot (captured once) and keep taunting.
            if (!_heldPosition.IsValid || _heldPosition.Distance(myPosition) > StandoffHysteresis)
            {
                _heldPosition = myPosition;
            }
            targetPosition = _heldPosition;
        }
        else
        {
            // Enemy closed in: recede to a point StandoffDistance from the enemy,
            // curved toward this group's side. Anchored to the enemy, so it never
            // runs further than needed to restore the gap.
            Vec2 fleeDirection = Vec2.FromRotation(awayFromEnemy.RotationInRadians + biasSign * LateralBiasRadians);
            targetPosition = enemyPosition + fleeDirection * StandoffDistance;
            _heldPosition = Vec2.Invalid; // re-capture the hold spot after this retreat

            // Map edge: rotate the flee point toward our side until it is back
            // inside the boundary — slides along the edge instead of clamping.
            for (int attempt = 1; attempt <= 5 && mission != null && !mission.IsPositionInsideBoundaries(targetPosition); attempt++)
            {
                float rotation = awayFromEnemy.RotationInRadians + biasSign * (LateralBiasRadians + attempt * 0.55f);
                targetPosition = enemyPosition + Vec2.FromRotation(rotation) * StandoffDistance;
            }
        }

        WorldPosition worldPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            targetPosition,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 10f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);

        // Spread out while running under fire; re-form tight once the arrows
        // stop, so the group is ready to turn and fight.
        ArrangementOrder desiredArrangement = UnderRangedFire && base.Formation.CountOfUnits > 1
            ? ArrangementOrder.ArrangementOrderLoose
            : ArrangementOrder.ArrangementOrderLine;
        if (base.Formation.ArrangementOrder != desiredArrangement)
        {
            base.Formation.SetArrangementOrder(desiredArrangement);
        }
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
