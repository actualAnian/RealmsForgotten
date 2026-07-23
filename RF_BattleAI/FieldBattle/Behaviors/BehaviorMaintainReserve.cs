using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

public sealed class BehaviorMaintainReserve : BehaviorComponent
{
    public Formation? AnchorFormation { get; set; }

    public float DesiredDistance { get; set; } = 22f;

    public BehaviorMaintainReserve(Formation formation)
        : base(formation)
    {
    }

    protected override float GetAiWeight()
    {
        if (AnchorFormation == null || AnchorFormation.CountOfUnits <= 0 || base.Formation.CountOfUnits <= 0)
        {
            return 0f;
        }

        float distanceSquared = base.Formation.CachedAveragePosition.DistanceSquared(AnchorFormation.CachedAveragePosition);
        if (distanceSquared > DesiredDistance * DesiredDistance * 4f)
        {
            return 1.25f;
        }

        return 0.85f;
    }

    public override TextObject GetBehaviorString()
    {
        return new TextObject("{=!}Maintain reserve");
    }

    protected override void CalculateCurrentOrder()
    {
        if (AnchorFormation == null || AnchorFormation.CountOfUnits <= 0)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 anchorPosition = AnchorFormation.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = AnchorFormation.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Formation.Team.QuerySystem.AverageEnemyPosition;

        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - anchorPosition).Normalized() : base.Formation.Direction;
        if (!directionToEnemy.IsValid)
        {
            directionToEnemy = base.Formation.Direction;
        }

        Vec2 reservePosition = anchorPosition - directionToEnemy * DesiredDistance;

        // Reposition hysteresis: jittery enemy medians (an army shuffling in a
        // corner) used to drag the reserve back and forth every tick. Keep the
        // held station until the ideal spot has drifted meaningfully.
        if (_heldReservePosition.IsValid
            && reservePosition.DistanceSquared(_heldReservePosition) < RepositionThreshold * RepositionThreshold)
        {
            reservePosition = _heldReservePosition;
        }
        else
        {
            _heldReservePosition = reservePosition;
        }

        WorldPosition targetPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            reservePosition,
            enemyPosition,
            BattleAITerrainPreference.ReserveRear,
            searchRadius: 14f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(targetPosition);
    }

    /// <summary>How far the ideal reserve spot must drift before the formation
    /// is actually re-stationed.</summary>
    public float RepositionThreshold { get; set; } = 15f;

    private Vec2 _heldReservePosition = Vec2.Invalid;

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
