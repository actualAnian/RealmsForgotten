using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

/// <summary>
/// Dumb executor: charges the SPECIFIC enemy formation assigned by the tactic
/// via MovementOrderChargeToTarget — the engine's own mechanism for targeted
/// charges. This is how the trap guarantees the strike lands on the chosen
/// formation instead of whatever CachedClosestEnemyFormation happens to be.
/// </summary>
public sealed class BehaviorDirectedCharge : BehaviorComponent
{
    public Formation? TargetEnemyFormation { get; set; }

    public BehaviorDirectedCharge(Formation formation)
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
        return new TextObject("{=!}Directed charge");
    }

    protected override void CalculateCurrentOrder()
    {
        if (TargetEnemyFormation == null || TargetEnemyFormation.CountOfUnits <= 0)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        base.CurrentOrder = MovementOrder.MovementOrderChargeToTarget(TargetEnemyFormation);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
