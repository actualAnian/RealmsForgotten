using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch]
public class HomesteadConversationCrashPatch
{
	public static MethodBase TargetMethod()
	{
		return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.GetName().Name == "SandBox")?.GetType("SandBox.Conversation.MissionLogics.MissionConversationLogic")?.GetMethod("FillConversationPointList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	}

	[HarmonyPrefix]
	public static bool Prefix()
	{
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement != null && currentSettlement.StringId != null && currentSettlement.StringId.StartsWith("hsr_settlement_"))
			{
				TraceLogger.Write("HomesteadConversationCrashPatch", "Skipped FillConversationPointList for '" + currentSettlement.StringId + "' (homestead settlement).");
				return false;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadConversationCrashPatch", "Prefix failed: " + ex.Message);
		}
		return true;
	}
}
