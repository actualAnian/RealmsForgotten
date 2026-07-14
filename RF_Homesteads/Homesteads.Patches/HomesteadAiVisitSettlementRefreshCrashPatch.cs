using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(AiVisitSettlementBehavior), "RefreshTheTargetingSettlementDictionary")]
internal static class HomesteadAiVisitSettlementRefreshCrashPatch
{
	private static FieldInfo? _dictField;

	[HarmonyPrefix]
	private static bool Prefix(AiVisitSettlementBehavior __instance)
	{
		try
		{
			if ((object)_dictField == null)
			{
				_dictField = AccessTools.Field(typeof(AiVisitSettlementBehavior), "_numberOfAlliedMobilePartiesTargetingSettlement");
			}
			if (!(_dictField?.GetValue(__instance) is Dictionary<Settlement, int> dictionary))
			{
				return true;
			}
			foreach (Settlement item in Settlement.All)
			{
				if (item != null && (item.IsFortification || item.IsVillage))
				{
					dictionary[item] = 0;
				}
			}
			foreach (MobileParty allLordParty in MobileParty.AllLordParties)
			{
				if (allLordParty == null)
				{
					continue;
				}
				bool num = allLordParty.Army == null || allLordParty.AttachedTo == null || allLordParty.Army.LeaderParty == allLordParty;
				Settlement targetSettlement = allLordParty.TargetSettlement;
				if (num && targetSettlement != null && allLordParty.CurrentSettlement != targetSettlement && targetSettlement.MapFaction == allLordParty.MapFaction)
				{
					int num2 = allLordParty.Army?.LeaderPartyAndAttachedPartiesCount ?? 1;
					if (!dictionary.ContainsKey(targetSettlement))
					{
						dictionary[targetSettlement] = 0;
						TraceLogger.Write("HomesteadAiVisitSettlementRefreshCrashPatch", "Seeded missing dictionary entry for stale/unmatched TargetSettlement '" + targetSettlement.StringId + "' (lord party '" + allLordParty.StringId + "') to avoid a KeyNotFoundException.");
					}
					dictionary[targetSettlement] += num2;
				}
			}
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadAiVisitSettlementRefreshCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
