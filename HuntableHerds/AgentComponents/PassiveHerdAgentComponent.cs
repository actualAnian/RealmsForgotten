using HuntableHerds.Extensions;
using HuntableHerds.Models;
using TaleWorlds.MountAndBlade;

namespace HuntableHerds.AgentComponents {
    public class PassiveHerdAgentComponent : HerdAgentComponent {
        public PassiveHerdAgentComponent(Agent agent) : base(agent) {
        }

        public override void HuntableAITick(float dt) {
            if (Agent.CanSeeOtherAgent(Agent.Main, HerdBuildData.CurrentHerdBuildData.SightRange))
                GoToPositionOppositeFromOtherAgent(Agent.Main);
        }
    }
}
