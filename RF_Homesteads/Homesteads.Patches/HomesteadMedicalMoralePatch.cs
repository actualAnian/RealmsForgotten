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
internal static class HomesteadMedicalMoralePatch
{
	private static readonly TextObject MedicalCareText = new TextObject("{=homestead_medical_care_morale_bonus}Medical care");

	private static readonly TextObject DiseaseCareText = new TextObject("{=homestead_ai_influence_disease_care_bonus}Medical care against disease");

	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(DefaultPartyMoraleModel), "GetEffectivePartyMorale");
		Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.GameComponents.NavalDLCPartyMoraleModel");
		MethodInfo methodInfo = ((type == null) ? null : AccessTools.Method(type, "GetEffectivePartyMorale"));
		if (methodInfo != null)
		{
			yield return methodInfo;
		}
	}

	[HarmonyPostfix]
	private static void Postfix(MobileParty __0, ref ExplainedNumber __result)
	{
		Homestead homestead = Homestead.GetFor(__0);
		if (homestead != null && homestead.MedicalCare > 0)
		{
			float medicalMoraleBonus = homestead.GetMedicalMoraleBonus();
			if (medicalMoraleBonus > 0f)
			{
				__result.Add(medicalMoraleBonus, MedicalCareText);
			}
			float aiInfluenceDiseaseCareBonus = homestead.GetAiInfluenceDiseaseCareBonus();
			if (aiInfluenceDiseaseCareBonus > 0f)
			{
				__result.Add(aiInfluenceDiseaseCareBonus, DiseaseCareText);
			}
		}
	}
}
