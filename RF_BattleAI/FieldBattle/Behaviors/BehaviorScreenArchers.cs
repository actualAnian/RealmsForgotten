using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

public sealed class BehaviorScreenArchers : BehaviorComponent
{
    public Formation? ProtectedFormation { get; set; }

    public float ScreenDistance { get; set; } = 10f;

    public BehaviorScreenArchers(Formation formation)
        : base(formation)
    {
    }

    protected override float GetAiWeight()
    {
        if (ProtectedFormation == null || ProtectedFormation.CountOfUnits <= 0 || ReferenceEquals(ProtectedFormation, base.Formation))
        {
            return 0f;
        }

        return 1f;
    }

    public override TextObject GetBehaviorString()
    {
        return new TextObject("{=!}Screen archers");
    }

    protected override void CalculateCurrentOrder()
    {
        if (ProtectedFormation == null || ProtectedFormation.CountOfUnits <= 0)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 archerPosition = ProtectedFormation.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = ProtectedFormation.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Formation.Team.QuerySystem.AverageEnemyPosition;

        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - archerPosition).Normalized() : base.Formation.Direction;
        if (!directionToEnemy.IsValid)
        {
            directionToEnemy = base.Formation.Direction;
        }

        Vec2 screenPosition = archerPosition + directionToEnemy * ScreenDistance;
        WorldPosition targetPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            screenPosition,
            enemyPosition,
            BattleAITerrainPreference.ForwardScreen,
            searchRadius: 12f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(targetPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
