using HarmonyLib;
using Homesteads.MissionLogics;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MissionConversationLogic), "OnAgentInteraction")]
internal class StopSparringConversationPatch
{
	public static bool Prefix(Agent userAgent, Agent agent)
	{
		if (agent != null && Mission.Current != null)
		{
			HomesteadTrainingFieldMissionLogic missionBehavior = Mission.Current.GetMissionBehavior<HomesteadTrainingFieldMissionLogic>();
			if (missionBehavior != null && missionBehavior.IsSparringAgent(agent))
			{
				return false;
			}
		}
		return true;
	}
}
