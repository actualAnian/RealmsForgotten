using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.Core;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadSandboxBattleMissionRecordPatch
{
	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("SandBox.CampaignMission");
		if (!(type == null))
		{
			return AccessTools.Method(type, "OpenBattleMission", new Type[1] { typeof(MissionInitializerRecord) });
		}
		return null;
	}

	private static void Prefix(ref MissionInitializerRecord rec)
	{
		HomesteadBattleContext.TryApplyTo(ref rec, "HomesteadSandboxBattleMissionRecordPatch");
	}

	private static void Postfix(object? __result)
	{
		HomesteadBattleMissionPatch.TryAttachBattleSceneLogic(__result, "HomesteadSandboxBattleMissionRecordPatch");
	}
}
