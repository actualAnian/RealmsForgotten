using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultPartyTrainingModel), "GetEffectiveDailyExperience")]
internal static class HomesteadPartyTrainingExperienceCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty mobileParty, TroopRosterElement troop, ref ExplainedNumber __result)
	{
		try
		{
			if (troop.Character == null)
			{
				TraceLogger.Write("HomesteadPartyTrainingExperienceCrashPatch", "Skipped GetEffectiveDailyExperience for party '" + mobileParty?.StringId + "' — a roster entry has a null Character.");
				__result = default(ExplainedNumber);
				return false;
			}
			Settlement settlement = mobileParty?.CurrentSettlement;
			if (mobileParty != null && mobileParty.IsGarrison && settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_") && settlement.Town == null)
			{
				TraceLogger.Write("HomesteadPartyTrainingExperienceCrashPatch", "Skipped GetEffectiveDailyExperience for GARRISON party '" + mobileParty.StringId + "' — settlement '" + settlement.StringId + "' has no Town yet.");
				__result = default(ExplainedNumber);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadPartyTrainingExperienceCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
