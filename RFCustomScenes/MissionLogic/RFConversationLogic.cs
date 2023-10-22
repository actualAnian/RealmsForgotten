using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements
{
    public class RFConversationLogic : MissionLogic
    {
        private MissionMode oldMissionMode;
        private static List<string> listOfTalkableNpcs = new List<string>();
        public ConversationManager ConversationManager { get; private set; }

        public static void AddNpcAsTalkable(string npcId)
        {
            listOfTalkableNpcs.Add(npcId);
        }
        public override void OnAgentInteraction(Agent userAgent, Agent agent)
        {
            if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
            {
                if (Game.Current.GameStateManager.ActiveState is MissionState)
                {
                    if (this.IsThereAgentAction(userAgent, agent))
                    {
                        this.StartConversation(agent);
                    }
                }
            }
        }

        private void StartConversation(Agent agent)
        {
            oldMissionMode = base.Mission.Mode;
            bool setActionsInstantly = false;
            ConversationManager = Campaign.Current.ConversationManager;
            ConversationManager.SetupAndStartMissionConversation(agent, base.Mission.MainAgent, setActionsInstantly);
            ConversationManager.ConversationEnd += this.OnConversationEnd;
            //_conversationStarted = true;
            foreach (IAgent agent2 in this.ConversationManager.ConversationAgents)
            {
                Agent agent3 = (Agent)agent2;
                agent3.ForceAiBehaviorSelection();
                agent3.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(true);
            }
            base.Mission.MainAgentServer.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(true);
            base.Mission.SetMissionMode(MissionMode.Conversation, setActionsInstantly);
        }
        private void OnConversationEnd()
        {
            foreach (IAgent agent in ConversationManager.ConversationAgents)
            {
                Agent agent2 = (Agent)agent;
                agent2.AgentVisuals.SetVisible(true);
                agent2.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(false);
                Agent mountAgent = agent2.MountAgent;
                if (mountAgent != null)
                {
                    mountAgent.AgentVisuals.SetVisible(true);
                }
            }
            if (base.Mission.Mode == MissionMode.Conversation)
            {
                base.Mission.SetMissionMode(oldMissionMode, false);
            }
            if (Agent.Main != null)
            {
                Agent.Main.AgentVisuals.SetVisible(true);
                Agent.Main.AgentVisuals.SetClothComponentKeepStateOfAllMeshes(false);
                if (Agent.Main.MountAgent != null)
                {
                    Agent.Main.MountAgent.AgentVisuals.SetVisible(true);
                }
            }
            base.Mission.MainAgentServer.Controller = Agent.ControllerType.Player;
            ConversationManager.ConversationEnd -= this.OnConversationEnd;
        }
        public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
        {
            if (otherAgent.Character != null && listOfTalkableNpcs.Contains(otherAgent.Character.StringId))
                return true;
            else return false;
        }
    }
}
