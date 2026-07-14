using System;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MissionAgentHandler), "GetAllProps")]
internal static class HomesteadGetAllPropsCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception)
	{
		if (__exception == null)
		{
			return null;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null || currentSettlement.StringId == null || !currentSettlement.StringId.StartsWith("hsr_settlement_"))
		{
			return __exception;
		}
		TraceLogger.Write("HomesteadGetAllPropsCrashPatch", "Swallowed MissionAgentHandler.GetAllProps error for '" + currentSettlement.StringId + "': " + __exception.Message);
		return null;
	}
}
