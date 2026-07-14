using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Hero), "set_StayingInSettlement")]
public static class HomesteadStayingInSettlementDiagnosticPatch
{
	private static void Prefix(Hero __instance, Settlement value)
	{
		try
		{
			Settlement settlement = __instance?.StayingInSettlement;
			if (settlement != value && ((settlement != null && settlement.StringId?.StartsWith("hsr_settlement_") == true) || (value != null && value.StringId?.StartsWith("hsr_settlement_") == true)))
			{
				TraceLogger.Write("HomesteadStayingInSettlementDiagnosticPatch", $"'{__instance?.Name}' StayingInSettlement: '{settlement?.StringId}' -> '{value?.StringId}'. {HomesteadHwpRemoveHeroDiagnosticPatch.ShortStack()}");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadStayingInSettlementDiagnosticPatch", "Prefix failed: " + ex.Message);
		}
	}
}
