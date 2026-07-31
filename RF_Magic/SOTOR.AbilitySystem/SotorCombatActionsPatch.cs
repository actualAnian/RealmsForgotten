using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

[HarmonyPatchCategory("SotorMissionOnlyPatches")]
[HarmonyPatch(typeof(Agent), "CombatActionsEnabled", MethodType.Getter)]
public static class SotorCombatActionsPatch
{
	public static void Postfix(ref bool __result, Agent __instance)
	{
		try
		{
			if (__instance.IsMainAgent && __result)
			{
				AbilityManagerMissionLogic abilityManagerMissionLogic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
				if (abilityManagerMissionLogic != null && abilityManagerMissionLogic.ShouldSuppressCombatActions)
				{
					__result = false;
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorCombatActionsPatch.Postfix failed: " + ex.Message);
		}
	}
}
