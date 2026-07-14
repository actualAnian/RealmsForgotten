using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadMedicalHealingPatch
{
	private static readonly TextObject MedicalCareText = new TextObject("{=homestead_medical_care_healing_bonus}Homestead medical care");

	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(DefaultPartyHealingModel), "GetDailyHealingForRegulars");
		yield return AccessTools.Method(typeof(DefaultPartyHealingModel), "GetDailyHealingHpForHeroes");
		Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.GameComponents.NavalDLCPartyHealingModel");
		MethodInfo methodInfo = ((type == null) ? null : AccessTools.Method(type, "GetDailyHealingForRegulars"));
		MethodInfo navalHeroesMethod = ((type == null) ? null : AccessTools.Method(type, "GetDailyHealingHpForHeroes"));
		if (methodInfo != null)
		{
			yield return methodInfo;
		}
		if (navalHeroesMethod != null)
		{
			yield return navalHeroesMethod;
		}
	}

	[HarmonyPostfix]
	private static void Postfix(MethodBase __originalMethod, PartyBase __0, bool __1, ref ExplainedNumber __result)
	{
		if (__1 || __0?.MobileParty == null)
		{
			return;
		}
		MobileParty mobileParty = __0.MobileParty;
		Homestead homestead = Homestead.GetFor(mobileParty);
		if (homestead != null && homestead.MedicalCare > 0)
		{
			float num = ((__originalMethod.Name == "GetDailyHealingHpForHeroes") ? homestead.GetHeroHealingBonus() : homestead.GetRegularHealingBonus());
			if (num > 0f)
			{
				__result.Add(num, MedicalCareText);
			}
			return;
		}
		Homestead nearby = Homestead.GetNearby(mobileParty.GetPosition2D);
		if (nearby != null && nearby.MedicalCare > 0)
		{
			float num2 = ((__originalMethod.Name == "GetDailyHealingHpForHeroes") ? nearby.GetHeroHealingBonus() : nearby.GetRegularHealingBonus());
			if (num2 > 0f)
			{
				__result.Add(num2, MedicalCareText);
			}
		}
	}
}
