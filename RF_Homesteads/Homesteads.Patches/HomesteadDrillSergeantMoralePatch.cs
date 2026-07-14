using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadDrillSergeantMoralePatch
{
	private static readonly TextObject DrillSergeantText = new TextObject("{=homestead_drill_sergeant_morale}Drill Sergeant's training");

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
		if (__0 == MobileParty.MainParty)
		{
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			if (instance != null && instance.HasArmsMasterMasteryUnlocked)
			{
				__result.Add(5f, DrillSergeantText);
			}
		}
	}
}
