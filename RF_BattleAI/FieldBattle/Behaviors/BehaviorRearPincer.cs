using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

/// <summary>
/// Dumb executor: travels to the point DIRECTLY BEHIND the assigned enemy
/// formation (relative to the bait it is chasing), inserting a wide detour
/// waypoint whenever the straight path would pass near the enemy's body — the
/// approach always goes AROUND, never through. All decisions about WHEN to do
/// this (and against WHOM) are made by the tactic, which sees the whole field.
/// </summary>
public sealed class BehaviorRearPincer : BehaviorComponent
{
    /// <summary>The bait formation the target enemy is chasing.</summary>
    public Formation? BaitFormation { get; set; }

    /// <summary>The specific enemy formation whose rear we are moving to.</summary>
    public Formation? TargetEnemyFormation { get; set; }

    public float RearAttackDistance { get; set; } = 45f;

    public float SafePassRadius { get; set; } = 55f;

    public float DetourRadius { get; set; } = 80f;

    public BehaviorRearPincer(Formation formation)
        : base(formation)
    {
    }

    protected override float GetAiWeight()
    {
        if (base.Formation.CountOfUnits <= 0
            || TargetEnemyFormation == null
            || TargetEnemyFormation.CountOfUnits <= 0)
        {
            return 0f;
        }

        return 1f;
    }

    public override TextObject GetBehaviorString()
    {
        return new TextObject("{=!}Rear pincer");
    }

    protected override void CalculateCurrentOrder()
    {
        Formation? enemy = TargetEnemyFormation;
        Formation? bait = BaitFormation;
        Vec2 myPosition = base.Formation.CachedMedianPosition.AsVec2;
        if (enemy == null || enemy.CountOfUnits <= 0 || !myPosition.IsValid)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 enemyPosition = enemy.CachedMedianPosition.AsVec2;
        Vec2 baitPosition = bait?.CachedMedianPosition.AsVec2 ?? Vec2.Invalid;
        if (!enemyPosition.IsValid)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        // Behind = past the enemy along the bait->enemy axis.
        Vec2 rearDirection = baitPosition.IsValid
            ? (enemyPosition - baitPosition).Normalized()
            : enemy.Direction;
        if (!rearDirection.IsValid)
        {
            rearDirection = enemy.Direction;
        }

        Vec2 rearPoint = enemyPosition + rearDirection * RearAttackDistance;

        Vec2 targetPosition = rearPoint;
        if (DistancePointToSegment(enemyPosition, myPosition, rearPoint) < SafePassRadius)
        {
            // Straight line would brush the enemy: detour wide on our own side.
            Vec2 perpendicular = new(-rearDirection.y, rearDirection.x);
            float side = Vec2.DotProduct(myPosition - enemyPosition, perpendicular) >= 0f ? 1f : -1f;
            targetPosition = enemyPosition + perpendicular * (side * DetourRadius);
        }

        WorldPosition worldPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            targetPosition,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 10f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);
    }

    private static float DistancePointToSegment(Vec2 point, Vec2 segmentStart, Vec2 segmentEnd)
    {
        Vec2 segment = segmentEnd - segmentStart;
        float lengthSquared = segment.LengthSquared;
        if (lengthSquared < 0.0001f)
        {
            return point.Distance(segmentStart);
        }

        float t = Vec2.DotProduct(point - segmentStart, segment) / lengthSquared;
        t = MBMath.ClampFloat(t, 0f, 1f);
        Vec2 projection = segmentStart + segment * t;
        return point.Distance(projection);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
