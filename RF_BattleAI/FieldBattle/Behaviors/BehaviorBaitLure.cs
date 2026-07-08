using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

/// <summary>
/// Bait arm of the bandit trap. From the FIRST tick of the battle it flees
/// CONTINUOUSLY — the movement target is always a lookahead point ahead of the
/// formation, recomputed every tick, so it never stands and never waits. The
/// flee direction is away from the enemy, rotated toward this group's assigned
/// side so the two bandit groups diverge to OPPOSITE sides of the field.
/// The bait only stops fleeing when the tactic springs the trap (weights are
/// switched to fight) or orders the cornered stand.
/// </summary>
public sealed class BehaviorBaitLure : BehaviorComponent
{
    /// <summary>The enemy formation to flee from (set by the tactic — the
    /// formation actually chasing this group), with engine fallbacks.</summary>
    public Formation? TargetEnemyFormation { get; set; }

    /// <summary>Which side this group's flight curves toward.</summary>
    public FormationAI.BehaviorSide FlankSide { get; set; } = FormationAI.BehaviorSide.Left;

    /// <summary>How far ahead the running point sits; recomputed every tick.</summary>
    public float FleeLookahead { get; set; } = 45f;

    /// <summary>Sideways curve of the flight, in radians (~34 degrees).</summary>
    public float LateralBiasRadians { get; set; } = 0.6f;

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

        // At the map edge the flight does not stop: rotate the flee direction
        // further and further toward our side until the target is back inside
        // the boundary — the formation slides ALONG the edge, and the flight
        // naturally becomes an orbit around the enemy (the encirclement).
        float biasSign = FlankSide == FormationAI.BehaviorSide.Left ? 1f : -1f;
        Mission? mission = Mission.Current;
        Vec2 targetPosition = myPosition + Vec2.FromRotation(awayFromEnemy.RotationInRadians + biasSign * LateralBiasRadians) * FleeLookahead;
        for (int attempt = 1; attempt <= 5 && mission != null && !mission.IsPositionInsideBoundaries(targetPosition); attempt++)
        {
            float rotation = awayFromEnemy.RotationInRadians + biasSign * (LateralBiasRadians + attempt * 0.55f);
            targetPosition = myPosition + Vec2.FromRotation(rotation) * FleeLookahead;
        }

        WorldPosition worldPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            targetPosition,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 10f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
