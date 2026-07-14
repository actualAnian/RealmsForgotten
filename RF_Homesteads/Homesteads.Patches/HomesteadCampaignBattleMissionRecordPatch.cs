using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.Core;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadCampaignBattleMissionRecordPatch
{
	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignMission");
		if (!(type == null))
		{
			return AccessTools.Method(type, "OpenBattleMission", new Type[1] { typeof(MissionInitializerRecord) });
		}
		return null;
	}

	private static void Prefix(ref MissionInitializerRecord rec)
	{
		HomesteadBattleContext.TryApplyTo(ref rec, "HomesteadCampaignBattleMissionRecordPatch");
	}

	private static void Postfix(object? __result)
	{
		HomesteadBattleMissionPatch.TryAttachBattleSceneLogic(__result, "HomesteadCampaignBattleMissionRecordPatch");
	}
}
