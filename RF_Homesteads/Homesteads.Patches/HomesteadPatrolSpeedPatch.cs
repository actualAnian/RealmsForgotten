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
internal static class HomesteadPatrolSpeedPatch
{
	public const float PatrolSpeed = 4f;

	public const float MovingHomesteadSpeed = 1f;

	private static readonly TextObject PatrolSpeedText = new TextObject("{=homestead_patrol_speed}Homestead Patrol");

	private static readonly TextObject MovingHomesteadText = new TextObject("{=homestead_moving_speed}Homestead Relocating");

	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(DefaultPartySpeedCalculatingModel), "CalculateFinalSpeed");
		Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.GameComponents.NavalDLCPartySpeedCalculatingModel");
		if (type != null)
		{
			MethodInfo methodInfo = AccessTools.Method(type, "CalculateFinalSpeed");
			if (methodInfo != null)
			{
				yield return methodInfo;
			}
		}
	}

	[HarmonyPostfix]
	private static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
	{
		if (mobileParty?.PartyComponent is HomesteadPatrolPartyComponent)
		{
			__result = new ExplainedNumber(4f, includeDescriptions: true, PatrolSpeedText);
		}
		else if (mobileParty?.PartyComponent is Homestead { IsMoving: not false })
		{
			__result = new ExplainedNumber(1f, includeDescriptions: true, MovingHomesteadText);
		}
	}
}
