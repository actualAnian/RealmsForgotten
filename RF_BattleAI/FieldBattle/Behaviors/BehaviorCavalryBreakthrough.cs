using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

public sealed class BehaviorCavalryBreakthrough : BehaviorComponent
{
    public Formation? AnchorFormation { get; set; }

    public Formation? TargetFormation { get; set; }

    public FormationAI.BehaviorSide FlankSide { get; set; } = FormationAI.BehaviorSide.Left;

    public float ForwardDistance { get; set; } = 56f;

    public float LateralClearance { get; set; } = 20f;

    public BehaviorCavalryBreakthrough(Formation formation)
        : base(formation)
    {
    }

    protected override float GetAiWeight()
    {
        if (base.Formation.CountOfUnits <= 0)
        {
            return 0f;
        }

        Formation? anchor = AnchorFormation ?? base.Formation;
        Formation? target = TargetFormation ?? anchor.CachedClosestEnemyFormation?.Formation ?? base.Formation.CachedClosestEnemyFormation?.Formation;
        return anchor.CountOfUnits > 0 && target != null && target.CountOfUnits > 0 ? 1f : 0f;
    }

    public override TextObject GetBehaviorString()
    {
        return new TextObject("{=!}Cavalry breakthrough");
    }

    protected override void CalculateCurrentOrder()
    {
        Formation anchor = AnchorFormation ?? base.Formation;
        Formation? target = TargetFormation ?? anchor.CachedClosestEnemyFormation?.Formation ?? base.Formation.CachedClosestEnemyFormation?.Formation;
        if (anchor.CountOfUnits <= 0 || target == null || target.CountOfUnits <= 0)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 targetPosition = target.CachedMedianPosition.AsVec2;
        Vec2 directionToTarget = targetPosition.IsValid ? (targetPosition - anchorPosition).Normalized() : anchor.Direction;
        if (!directionToTarget.IsValid)
        {
            directionToTarget = anchor.Direction;
        }

        if (!directionToTarget.IsValid)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 lateralDirection = FlankSide == FormationAI.BehaviorSide.Left
            ? new Vec2(-directionToTarget.y, directionToTarget.x)
            : new Vec2(directionToTarget.y, -directionToTarget.x);
        if (!lateralDirection.IsValid)
        {
            lateralDirection = FlankSide == FormationAI.BehaviorSide.Left ? new Vec2(-1f, 0f) : new Vec2(1f, 0f);
        }

        Vec2 breakthroughPoint = targetPosition + directionToTarget * ForwardDistance + lateralDirection * LateralClearance;
        WorldPosition worldPosition = target.CachedMedianPosition;
        worldPosition.SetVec2(breakthroughPoint);
        base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
