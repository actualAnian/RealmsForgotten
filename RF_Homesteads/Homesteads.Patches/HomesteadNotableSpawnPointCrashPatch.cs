using System;
using HarmonyLib;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(NotableSpawnPointHandler), "OnBehaviorInitialize")]
public static class HomesteadNotableSpawnPointCrashPatch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement == null || currentSettlement.StringId == null || !currentSettlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			TraceLogger.Write("HomesteadNotableSpawnPointCrashPatch", "Skipped OnBehaviorInitialize for '" + currentSettlement.StringId + "' (our settlements don't have the full marker set this handler expects).");
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadNotableSpawnPointCrashPatch", "Prefix error: " + ex.Message);
			return true;
		}
	}
}
