using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultSettlementLoyaltyModel), "GetSettlementLoyaltyChangeDueToGovernorCulture")]
internal static class HomesteadSettlementGovernorCultureLoyaltyPatch
{
	private static readonly FieldInfo? GovernorCultureTextField = AccessTools.Field(typeof(DefaultSettlementLoyaltyModel), "GovernorCultureText");

	[HarmonyPrefix]
	private static bool Prefix(DefaultSettlementLoyaltyModel __instance, Town town, ref ExplainedNumber explainedNumber)
	{
		try
		{
			Settlement settlement = town?.Settlement;
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			if (town.Governor != null)
			{
				bool flag = town.Governor.Culture == settlement.OwnerClan?.Culture;
				TextObject description = GovernorCultureTextField?.GetValue(null) as TextObject;
				explainedNumber.Add(flag ? __instance.GovernorSameCultureLoyaltyEffect : __instance.GovernorDifferentCultureLoyaltyEffect, description);
			}
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementGovernorCultureLoyaltyPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
