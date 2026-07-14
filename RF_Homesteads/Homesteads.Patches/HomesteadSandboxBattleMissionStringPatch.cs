using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.Models;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadSandboxBattleMissionStringPatch
{
	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("SandBox.CampaignMission");
		if (!(type == null))
		{
			return AccessTools.Method(type, "OpenBattleMission", new Type[2]
			{
				typeof(string),
				typeof(bool)
			});
		}
		return null;
	}

	private static void Prefix(ref string scene)
	{
		HomesteadBattleContext.TryApplyTo(ref scene, "HomesteadSandboxBattleMissionStringPatch");
	}

	private static void Postfix(object? __result)
	{
		HomesteadBattleMissionPatch.TryAttachBattleSceneLogic(__result, "HomesteadSandboxBattleMissionStringPatch");
	}
}
