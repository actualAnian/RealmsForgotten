using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

/// <summary>
/// Kills pursuing stragglers. Formation-median logic cannot see a handful of
/// enemy agents running ahead of their (far away) formation body — telemetry
/// showed 5 pursuers on the bandits' heels while the enemy formation median
/// sat 250m+ away, so every formation-level rule kept saying "flee". This
/// executor works at AGENT level: while enemy agents are inside SenseRadius it
/// issues a plain charge (each unit engages the nearest enemy — the pursuers),
/// and when its own surroundings are clear it converges on the hunted ally
/// group so both bandit groups gang up on the same pursuers. The tactic only
/// arms this behavior after checking the fight is winnable.
/// </summary>
public sealed class BehaviorSwatPursuers : BehaviorComponent
{
    private readonly MBList<Agent> _nearbyEnemyBuffer = new();

    /// <summary>Hunted ally group to converge on while nothing chases this one.</summary>
    public Formation? AllyFormation { get; set; }

    public float SenseRadius { get; set; } = 75f;

    public BehaviorSwatPursuers(Formation formation)
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
        return new TextObject("{=!}Swat pursuers");
    }

    protected override void CalculateCurrentOrder()
    {
        Mission? mission = Mission.Current;
        Vec2 myPosition = base.Formation.CachedMedianPosition.AsVec2;
        bool hasLocalEnemies = false;
        if (mission != null && myPosition.IsValid)
        {
            foreach (Agent agent in mission.GetNearbyEnemyAgents(myPosition, SenseRadius, base.Formation.Team, _nearbyEnemyBuffer))
            {
                if (agent.IsActive())
                {
                    hasLocalEnemies = true;
                    break;
                }
            }
        }

        if (hasLocalEnemies || AllyFormation == null || AllyFormation.CountOfUnits <= 0)
        {
            base.CurrentOrder = MovementOrder.MovementOrderCharge;
            return;
        }

        base.CurrentOrder = MovementOrder.MovementOrderMove(AllyFormation.CachedMedianPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
