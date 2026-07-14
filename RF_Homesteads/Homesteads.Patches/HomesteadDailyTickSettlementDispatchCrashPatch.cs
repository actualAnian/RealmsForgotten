using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(CampaignEventDispatcher), "DailyTickSettlement")]
internal static class HomesteadDailyTickSettlementDispatchCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, Settlement settlement)
	{
		if (__exception == null)
		{
			return null;
		}
		if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_"))
		{
			TraceLogger.Write("HomesteadDailyTickSettlementDispatchCrashPatch", "Swallowed DailyTickSettlement error for '" + settlement.StringId + "': " + __exception.Message);
			return null;
		}
		return __exception;
	}
}
