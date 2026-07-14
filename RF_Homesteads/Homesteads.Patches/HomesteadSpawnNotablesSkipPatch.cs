using System;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(SettlementHelper), "SpawnNotablesIfNeeded")]
internal static class HomesteadSpawnNotablesSkipPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Settlement settlement)
	{
		try
		{
			if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_"))
			{
				TraceLogger.WriteOnce("SkipSpawnNotables:" + settlement.StringId, "HomesteadSpawnNotablesSkipPatch", "Skipped native SpawnNotablesIfNeeded for '" + settlement.StringId + "' — we manage our own notable roster.");
				return false;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSpawnNotablesSkipPatch", "Prefix failed: " + ex.Message);
		}
		return true;
	}
}
