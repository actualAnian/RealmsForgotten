using RealmsForgotten.HuntableHerds.Extensions;
using RealmsForgotten.HuntableHerds.Models;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.HuntableHerds.AgentComponents
{
    public class PassiveHerdAgentComponent : HerdAgentComponent
    {
        public PassiveHerdAgentComponent(Agent agent, HerdBuildData data) : base(agent, data)
        {
        }

        public override void HuntableAITick(float dt)
        {
            // SightRange is METERS. It used to be passed into the angle slot, which made the cone
            // always-true and silently pinned the real distance to the 30m default.
            if (Agent.CanSeeOtherAgent(Agent.Main, angleMax: Settings.Instance.SightConeHalfAngle, distance: Data.SightRange))
                GoToPositionOppositeFromOtherAgent(Agent.Main);
        }
    }
}
