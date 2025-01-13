using HuntableHerds.Models;
using RealmsForgotten.HuntableHerds.Models;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.HuntableHerds.AgentComponents
{
    public class HerdAgentComponent : LootableAgentComponent
    {

        public HerdAgentComponent(Agent agent) : base(agent, HerdBuildData.CurrentHerdBuildData.GetCopyOfItemDrops())
        {
            agent.Health = HerdBuildData.CurrentHerdBuildData.StartingHealth;
            ;
        }

        public override void OnTickAsAI(float dt)
        {
            if (Agent.Main == null)
                return;

            HuntableAITick(dt);
        }
        public virtual void HuntableAITick(float dt) { }

        public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
        {
            if (!HerdBuildData.CurrentHerdBuildData.FleeOnAttacked || affectorAgent == null || affectorAgent == Agent)
                return;

            GoToPositionOppositeFromOtherAgent(affectorAgent);
        }
        public void SetMoveToPosition(WorldPosition position, bool addHumanLikeDelay = false, Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.None)
        {
            this.Agent.SetScriptedPosition(ref position, addHumanLikeDelay, flags);
        }

        public void GoToPositionOppositeFromOtherAgent(Agent otherAgent)
        {
            Vec3 differenceWithMult = (Agent.Position - otherAgent.Position) * 2;
            Vec3 gotoPosition = Agent.Position + differenceWithMult;
            SetMoveToPosition(gotoPosition.ToWorldPosition());
        }
    }
}