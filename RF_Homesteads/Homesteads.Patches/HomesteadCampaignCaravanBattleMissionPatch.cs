using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.Core;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadCampaignCaravanBattleMissionPatch
{
	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignMission");
		if (!(type == null))
		{
			return AccessTools.Method(type, "OpenCaravanBattleMission", new Type[2]
			{
				typeof(MissionInitializerRecord),
				typeof(bool)
			});
		}
		return null;
	}

	private static void Prefix(ref MissionInitializerRecord rec)
	{
		HomesteadBattleContext.TryApplyTo(ref rec, "HomesteadCampaignCaravanBattleMissionPatch");
	}

	private static void Postfix(object? __result)
	{
		HomesteadBattleMissionPatch.TryAttachBattleSceneLogic(__result, "HomesteadCampaignCaravanBattleMissionPatch");
	}
}
