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

        Vec2 targetPosition = ForwardOffset > 0f
            ? ComputeWingPoint(anchor, enemyFormation, FlankSide, ForwardOffset, LateralDistance)
            : ComputeApproachPoint(base.Formation, anchor, enemyFormation, FlankSide, OuterArcLateralDistance, OuterArcRearOffset, LateralDistance, RearOffset);
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

    public static Vec2 ComputeApproachPoint(Formation cavalry, Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide flankSide, float outerArcLateralDistance, float outerArcRearOffset, float lateralDistance, float rearOffset)
    {
        return ComputeAssemblyPoint(anchor, enemyFormation, flankSide, lateralDistance, rearOffset);
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
