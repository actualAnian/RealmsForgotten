using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(DefaultMobilePartyFoodConsumptionModel), "CalculateDailyBaseFoodConsumptionf")]
public static class SotorSkeletonFoodPatch
{
	private static void Postfix(MobileParty party, ref ExplainedNumber __result)
	{
		try
		{
			if (party?.Party?.MemberRoster == null)
			{
				return;
			}
			int num = 0;
			TroopRoster memberRoster = party.Party.MemberRoster;
			for (int i = 0; i < memberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
				if (SkeletonUpkeep.IsSkeletonChar(elementCopyAtIndex.Character))
				{
					num += elementCopyAtIndex.Number;
				}
			}
			if (num > 0)
			{
				float num2 = 0.05f;
				__result.Add((float)num * num2, new TextObject("Undead (no food)"));
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SkeletonFoodPatch failed: " + ex.Message);
		}
	}
}
