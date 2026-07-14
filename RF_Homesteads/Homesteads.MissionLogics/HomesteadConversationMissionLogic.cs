using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public class HomesteadConversationMissionLogic : MissionLogic
{
	public static Hero? ActiveConversationHero { get; private set; }

	public static bool IsHoundMasterConversation { get; private set; }

	public static bool IsMarketLadyConversation { get; private set; }

	public static bool IsArmsMasterConversation { get; private set; }

	public static bool IsTavernGreeterConversation { get; private set; }

	public static HomesteadNpcRole? ActiveConversationNpcRole { get; private set; }

	public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		if (!agent.IsHuman || !userAgent.IsHuman)
		{
			return;
		}
		CharacterObject characterObject = (agent.Character as CharacterObject) ?? (agent.Origin?.Troop as CharacterObject);
		ActiveConversationHero = ((characterObject != null && characterObject.IsHero && !characterObject.IsPlayerCharacter) ? characterObject.HeroObject : null);
		IsHoundMasterConversation = agent == HomesteadSpawningMissionLogic.HoundMasterAgent;
		IsMarketLadyConversation = agent == HomesteadSpawningMissionLogic.MarketLadyAgent;
		IsArmsMasterConversation = agent == HomesteadSpawningMissionLogic.ArmsMasterAgent;
		IsTavernGreeterConversation = agent == HomesteadSpawningMissionLogic.TavernGreeterAgent;
		ActiveConversationNpcRole = null;
		if (ActiveConversationHero == null && HomesteadSpawningMissionLogic.Current != null && HomesteadSpawningMissionLogic.Current.TryGetAgentRole(agent, out var role))
		{
			ActiveConversationNpcRole = role;
		}
		TraceLogger.Write("HomesteadConversationMissionLogic", "OnAgentInteraction: agent='" + agent.Name + "' co='" + characterObject?.StringId + "' " + $"isHero={characterObject?.IsHero} ActiveConversationHero='{ActiveConversationHero?.Name}'");
		agent.SetLookAgent(userAgent);
		agent.SetLookToPointOfInterest(userAgent.GetEyeGlobalPosition());
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			try
			{
				if (agent != null && agent.IsActive())
				{
					agent.SetLookAgent(null);
					agent.SetLookToPointOfInterest(Vec3.Invalid);
				}
			}
			catch
			{
			}
			ActiveConversationHero = null;
			IsHoundMasterConversation = false;
			IsMarketLadyConversation = false;
			IsArmsMasterConversation = false;
			IsTavernGreeterConversation = false;
			ActiveConversationNpcRole = null;
		};
	}
}
