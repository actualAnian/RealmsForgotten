using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

/// <summary>
/// Horse-archer arm of the mounted onslaught. The formation sweeps back and
/// forth along a firing ARC on the OPEN side of the target (the hemisphere
/// away from map edges and terrain the enemy is cornered against), shooting
/// on the move — constant fire pressure without ever piling into the corner.
/// Vanilla mounted skirmish computes positions relative to the enemy's
/// facing, which degenerates into a clamped ball when the enemy sits between
/// a rock and the map boundary; this executor works from an explicit
/// open-side bearing assigned by the tactic instead.
/// </summary>
public sealed class BehaviorMountedFiringArc : BehaviorComponent
{
    /// <summary>Formation under fire (set by the tactic each tick).</summary>
    public Formation? TargetEnemyFormation { get; set; }

    /// <summary>Unit vector from the target toward open ground.</summary>
    public Vec2 OpenSideDirection { get; set; }

    public float ArcRadius { get; set; } = 70f;

    /// <summary>Half-width of the sweep, radians (~60 degrees).</summary>
    public float ArcHalfWidthRadians { get; set; } = 1.05f;

    /// <summary>Sweep speed, radians per second of oscillation phase.</summary>
    public float SweepSpeed { get; set; } = 0.25f;

    public BehaviorMountedFiringArc(Formation formation)
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
        return new TextObject("{=!}Mounted firing arc");
    }

    protected override void CalculateCurrentOrder()
    {
        Formation? enemy = TargetEnemyFormation != null && TargetEnemyFormation.CountOfUnits > 0
            ? TargetEnemyFormation
            : base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation?.Formation
                ?? base.Formation.CachedClosestEnemyFormation?.Formation;
        if (enemy == null)
        {
            base.CurrentOrder = MovementOrder.MovementOrderCharge;
            return;
        }

        Vec2 targetPosition = enemy.CachedMedianPosition.AsVec2;
        Vec2 openDirection = OpenSideDirection.IsValid && OpenSideDirection.LengthSquared > 0.01f
            ? OpenSideDirection.Normalized()
            : (base.Formation.CachedMedianPosition.AsVec2 - targetPosition).Normalized();
        if (!targetPosition.IsValid || !openDirection.IsValid)
        {
            base.CurrentOrder = MovementOrder.MovementOrderCharge;
            return;
        }

        // Oscillating sweep along the arc; the whole formation rides the arc
        // as a moving firing line.
        float phase = MathF.Sin((Mission.Current?.CurrentTime ?? 0f) * SweepSpeed) * ArcHalfWidthRadians;
        Vec2 arcPoint = targetPosition
            + Vec2.FromRotation(openDirection.RotationInRadians + phase) * ArcRadius;

        // Never sweep out of the map: rotate the arc point back toward the
        // open bearing until it is inside (same recipe as the bait's flight).
        Mission? mission = Mission.Current;
        for (int attempt = 1; attempt <= 5 && mission != null && !mission.IsPositionInsideBoundaries(arcPoint); attempt++)
        {
            float shrunkPhase = phase * (1f - attempt * 0.25f);
            arcPoint = targetPosition
                + Vec2.FromRotation(openDirection.RotationInRadians + shrunkPhase) * ArcRadius;
        }

        WorldPosition worldPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            arcPoint,
            targetPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 8f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
