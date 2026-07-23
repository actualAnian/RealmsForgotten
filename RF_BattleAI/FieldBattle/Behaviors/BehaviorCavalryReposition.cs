using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

public sealed class BehaviorCavalryReposition : BehaviorComponent
{
    public float ForwardOffset { get; set; }

    public Formation? AnchorFormation { get; set; }

    public FormationAI.BehaviorSide FlankSide { get; set; } = FormationAI.BehaviorSide.Left;

    public float LateralDistance { get; set; } = 34f;

    public float RearOffset { get; set; } = 12f;

    public float OuterArcLateralDistance { get; set; } = 55f;

    public float OuterArcRearOffset { get; set; } = 8f;

    /// <summary>
    /// While the anvil is farther than this from the enemy, the wings ride in
    /// ESCORT: level with the advancing line, on its flanks — marching with
    /// the army instead of riding deep toward enemy-anchored arc points 200m
    /// early (long exposure to volleys, and it reads as the cavalry abandoning
    /// the line). Once the anvil closes inside this distance the wings swing
    /// out to the rear arc; cavalry speed covers that ground well before the
    /// release gate (~32m vs infantry) opens.
    /// </summary>
    public float EscortEngageDistance { get; set; } = 120f;

    private const float EscortHysteresisBand = 30f;

    private const float EscortLateralDistance = 30f;

    private bool _swungOut;

    public BehaviorCavalryReposition(Formation formation)
        : base(formation)
    {
    }

    protected override float GetAiWeight()
    {
        if (base.Formation.CountOfUnits <= 0 || AnchorFormation == null || AnchorFormation.CountOfUnits <= 0)
        {
            return 0f;
        }

        return 1f;
    }

    public override TextObject GetBehaviorString()
    {
        return new TextObject("{=!}Cavalry reposition");
    }

    protected override void CalculateCurrentOrder()
    {
        Formation anchor = AnchorFormation ?? base.Formation;
        Formation? enemyFormation = anchor.CachedClosestEnemyFormation?.Formation ?? base.Formation.CachedClosestEnemyFormation?.Formation;
        if (anchor.CountOfUnits <= 0 || enemyFormation == null)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 targetPosition;
        if (ForwardOffset > 0f)
        {
            targetPosition = ComputeWingPoint(anchor, enemyFormation, FlankSide, ForwardOffset, LateralDistance);
        }
        else if (ShouldEscort(anchor, enemyFormation))
        {
            targetPosition = ComputeEscortPoint(anchor, enemyFormation, FlankSide);
        }
        else
        {
            targetPosition = ComputeApproachPoint(base.Formation, anchor, enemyFormation, FlankSide, OuterArcLateralDistance, OuterArcRearOffset, LateralDistance, RearOffset);
        }
        if (!targetPosition.IsValid)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        WorldPosition worldPosition = anchor.CachedMedianPosition;
        worldPosition.SetVec2(targetPosition);
        base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }

    private bool ShouldEscort(Formation anchor, Formation enemyFormation)
    {
        float distanceSquared = anchor.CachedMedianPosition.AsVec2.DistanceSquared(enemyFormation.CachedMedianPosition.AsVec2);
        if (!_swungOut && distanceSquared <= EscortEngageDistance * EscortEngageDistance)
        {
            _swungOut = true;
        }
        else if (_swungOut && distanceSquared > (EscortEngageDistance + EscortHysteresisBand) * (EscortEngageDistance + EscortHysteresisBand))
        {
            // Full disengagement — fall back into escort at the line's side.
            _swungOut = false;
        }

        return !_swungOut;
    }

    private static Vec2 ComputeEscortPoint(Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide flankSide)
    {
        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = GetDirectionToEnemy(anchor, enemyFormation);
        Vec2 lateralDirection = GetLateralDirection(directionToEnemy, flankSide);
        return anchorPosition + lateralDirection * EscortLateralDistance;
    }

    public static Vec2 ComputeApproachPoint(Formation cavalry, Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide flankSide, float outerArcLateralDistance, float outerArcRearOffset, float lateralDistance, float rearOffset)
    {
        if (enemyFormation == null || enemyFormation.CountOfUnits <= 0)
        {
            return ComputeAssemblyPoint(anchor, enemyFormation, flankSide, lateralDistance, rearOffset);
        }

        // Real two-stage rear arc. The strike axis is GEOMETRIC: anvil → enemy
        // extended past the enemy is, by definition, their rear — no guessed
        // angles. Stage 1 rides a wide outer point on the chosen flank (out of
        // javelin reach, clear of the enemy front); stage 2, once the cavalry
        // is deep enough along the axis, tucks in behind the enemy. From there
        // the released charge hits backs, not braced shields.
        Vec2 enemyPosition = enemyFormation.CachedMedianPosition.AsVec2;
        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 axis = (enemyPosition - anchorPosition).Normalized();
        if (!axis.IsValid)
        {
            return ComputeAssemblyPoint(anchor, enemyFormation, flankSide, lateralDistance, rearOffset);
        }
        Vec2 perpendicular = flankSide == FormationAI.BehaviorSide.Left
            ? new Vec2(-axis.y, axis.x)
            : new Vec2(axis.y, -axis.x);

        Vec2 cavalryPosition = cavalry.CachedMedianPosition.AsVec2;
        float depthAlongAxis = Vec2.DotProduct(cavalryPosition - enemyPosition, axis);

        if (depthAlongAxis < outerArcRearOffset * 0.5f)
        {
            // Stage 1: wide outer arc, level with the enemy's rear corner.
            return enemyPosition + perpendicular * outerArcLateralDistance + axis * (outerArcRearOffset * 0.6f);
        }

        // Stage 2: settle directly behind the enemy on this flank.
        return enemyPosition + perpendicular * lateralDistance + axis * rearOffset;
    }

    public static Vec2 ComputeWingPoint(Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide flankSide, float forwardOffset, float lateralDistance)
    {
        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = GetDirectionToEnemy(anchor, enemyFormation);
        if (!directionToEnemy.IsValid)
        {
            return anchorPosition;
        }

        Vec2 lateralDirection = GetLateralDirection(directionToEnemy, flankSide);
        return anchorPosition + directionToEnemy * forwardOffset + lateralDirection * lateralDistance;
    }

    public static Vec2 ComputeAssemblyPoint(Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide flankSide, float lateralDistance, float rearOffset)
    {
        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = GetDirectionToEnemy(anchor, enemyFormation);
        if (!directionToEnemy.IsValid)
        {
            return anchorPosition;
        }

        Vec2 lateralDirection = GetLateralDirection(directionToEnemy, flankSide);

        return anchorPosition + lateralDirection * lateralDistance - directionToEnemy * rearOffset;
    }

    private static Vec2 GetDirectionToEnemy(Formation anchor, Formation enemyFormation)
    {
        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = enemyFormation.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - anchorPosition).Normalized() : anchor.Direction;
        if (!directionToEnemy.IsValid)
        {
            directionToEnemy = anchor.Direction;
        }

        return directionToEnemy;
    }

    private static Vec2 GetLateralDirection(Vec2 directionToEnemy, FormationAI.BehaviorSide flankSide)
    {
        Vec2 lateralDirection = flankSide == FormationAI.BehaviorSide.Left
            ? new Vec2(-directionToEnemy.y, directionToEnemy.x)
            : new Vec2(directionToEnemy.y, -directionToEnemy.x);

        if (!lateralDirection.IsValid)
        {
            lateralDirection = flankSide == FormationAI.BehaviorSide.Left ? new Vec2(-1f, 0f) : new Vec2(1f, 0f);
        }

        return lateralDirection;
    }
}
