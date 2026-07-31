using System;
using HarmonyLib;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(DefaultPartySizeLimitModel), "GetPartyMemberSizeLimit")]
public static class SotorSkeletonPartyWeightPatch
{
	private static void Postfix(PartyBase party, ref ExplainedNumber __result)
	{
		try
		{
			if (party?.MemberRoster == null || party.MobileParty == null)
			{
				return;
			}
			float num = BestNecromancerAddBack(party);
			if (num <= 0f)
			{
				return;
			}
			int num2 = 0;
			TroopRoster memberRoster = party.MemberRoster;
			for (int i = 0; i < memberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
				if (SkeletonUpkeep.IsSkeletonChar(elementCopyAtIndex.Character))
				{
					num2 += elementCopyAtIndex.Number;
				}
			}
			if (num2 > 0)
			{
				__result.Add((float)num2 * num, new TextObject("Necromancy (undead retinue)"));
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SkeletonPartyWeightPatch failed: " + ex.Message);
		}
	}

	private static float BestNecromancerAddBack(PartyBase party)
	{
		float num = 0f;
		TroopRoster memberRoster = party.MemberRoster;
		for (int i = 0; i < memberRoster.Count; i++)
		{
			Hero hero = memberRoster.GetElementCopyAtIndex(i).Character?.HeroObject;
			if (hero == null)
			{
				continue;
			}
			HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
			if (extendedInfo == null || !extendedInfo.HasLore("LoreOfNecromancy"))
			{
				continue;
			}
			int num2 = (int)(SotorSpellcraftHelper.GetCastingLevel(hero) - 2);
			if (num2 > 0)
			{
				float num3 = 0.2f * (float)num2;
				if (num3 > num)
				{
					num = num3;
				}
			}
		}
		return num;
	}
}
