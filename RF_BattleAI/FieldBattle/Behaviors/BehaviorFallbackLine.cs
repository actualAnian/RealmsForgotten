using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

public sealed class BehaviorFallbackLine : BehaviorComponent
{
    public Formation? AnchorFormation { get; set; }

    public float FallbackDistance { get; set; } = 18f;

    public FormationAI.BehaviorSide FlankSide { get; set; } = FormationAI.BehaviorSide.Middle;

    public float LateralOffset { get; set; }

    public BehaviorFallbackLine(Formation formation)
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
        return new TextObject("{=!}Fallback line");
    }

    protected override void CalculateCurrentOrder()
    {
        Formation anchor = AnchorFormation ?? base.Formation;
        if (anchor.CountOfUnits <= 0)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = anchor.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Formation.Team.QuerySystem.AverageEnemyPosition;

        Vec2 fallbackDirection = anchorPosition.IsValid && enemyPosition.IsValid
            ? (anchorPosition - enemyPosition).Normalized()
            : (-base.Formation.Direction).Normalized();

        if (!fallbackDirection.IsValid)
        {
            fallbackDirection = (-base.Formation.Direction).Normalized();
        }

        Vec2 fallbackPosition = anchorPosition + fallbackDirection * FallbackDistance;
        if (LateralOffset != 0f && FlankSide != FormationAI.BehaviorSide.Middle)
        {
            Vec2 lateralDirection = FlankSide == FormationAI.BehaviorSide.Left
                ? new Vec2(-fallbackDirection.y, fallbackDirection.x)
                : new Vec2(fallbackDirection.y, -fallbackDirection.x);

            if (!lateralDirection.IsValid)
            {
                lateralDirection = FlankSide == FormationAI.BehaviorSide.Left ? new Vec2(-1f, 0f) : new Vec2(1f, 0f);
            }

            fallbackPosition += lateralDirection * LateralOffset;
        }

        WorldPosition targetPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            fallbackPosition,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 16f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(targetPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
