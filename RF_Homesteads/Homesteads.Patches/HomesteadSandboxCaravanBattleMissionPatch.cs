using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.Core;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadSandboxCaravanBattleMissionPatch
{
	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("SandBox.CampaignMission");
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
		HomesteadBattleContext.TryApplyTo(ref rec, "HomesteadSandboxCaravanBattleMissionPatch");
	}

	private static void Postfix(object? __result)
	{
		HomesteadBattleMissionPatch.TryAttachBattleSceneLogic(__result, "HomesteadSandboxCaravanBattleMissionPatch");
	}
}
