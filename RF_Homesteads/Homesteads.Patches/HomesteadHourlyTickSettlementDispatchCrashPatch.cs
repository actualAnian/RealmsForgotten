using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(CampaignEventDispatcher), "HourlyTickSettlement")]
internal static class HomesteadHourlyTickSettlementDispatchCrashPatch
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
			TraceLogger.Write("HomesteadHourlyTickSettlementDispatchCrashPatch", "Swallowed HourlyTickSettlement error for '" + settlement.StringId + "': " + __exception.Message);
			return null;
		}
		return __exception;
	}
}
