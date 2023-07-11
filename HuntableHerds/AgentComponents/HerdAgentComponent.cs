using HuntableHerds.Models;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HuntableHerds.AgentComponents {
    public class HerdAgentComponent : AgentComponent {
        private ItemRoster itemDrops;

		public HerdAgentComponent(Agent agent) : base(agent) {
            agent.Health = HerdBuildData.CurrentHerdBuildData.StartingHealth;
            itemDrops = HerdBuildData.CurrentHerdBuildData.GetCopyOfItemDrops();
		}

        public override void OnTickAsAI(float dt) {
            if (Agent.Main == null)
                return;

            HuntableAITick(dt);
        }
        public virtual void HuntableAITick(float dt) { }

        public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon) {
            if (!HerdBuildData.CurrentHerdBuildData.FleeOnAttacked || affectorAgent == null || affectorAgent == Agent)
                return;

            GoToPositionOppositeFromOtherAgent(affectorAgent);
        }

        public ItemRoster GetItemDrops() {
            return itemDrops;
        }

        public void ClearItemDrops() {
            itemDrops.Clear();
        }

        public void SetMoveToPosition(WorldPosition position, bool addHumanLikeDelay = false, Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.None) {
            this.Agent.SetScriptedPosition(ref position, addHumanLikeDelay, flags);
        }

        public void GoToPositionOppositeFromOtherAgent(Agent otherAgent) {
            Vec3 differenceWithMult = (Agent.Position - otherAgent.Position) * 2;
            Vec3 gotoPosition = Agent.Position + differenceWithMult;
            SetMoveToPosition(gotoPosition.ToWorldPosition());
        }
    }
}
