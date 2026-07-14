using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Village), "DailyTick")]
internal static class HomesteadVillageDailyTickCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, Village __instance)
	{
		if (__exception == null)
		{
			return null;
		}
		Settlement settlement = __instance?.Settlement;
		if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_"))
		{
			TraceLogger.Write("HomesteadVillageDailyTickCrashPatch", "Swallowed Village.DailyTick error for '" + settlement.StringId + "': " + __exception.Message);
			return null;
		}
		return __exception;
	}
}
