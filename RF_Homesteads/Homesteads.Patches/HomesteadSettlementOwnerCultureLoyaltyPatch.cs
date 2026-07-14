using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultSettlementLoyaltyModel), "GetSettlementLoyaltyChangeDueToOwnerCulture")]
internal static class HomesteadSettlementOwnerCultureLoyaltyPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Town town, ref ExplainedNumber explainedNumber)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementOwnerCultureLoyaltyPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
