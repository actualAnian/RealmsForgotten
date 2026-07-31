using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

[HarmonyPatchCategory("SotorMissionOnlyPatches")]
[HarmonyPatch(typeof(Agent), "HandleBlowAux")]
public static class SotorSkeletonPainPatch
{
	private static bool Prefix(Agent __instance)
	{
		try
		{
			if (!SkeletonVoice.IsSkeleton(__instance))
			{
				return true;
			}
			SkeletonVoice.PlayRattle(__instance);
			return false;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SkeletonPainPatch failed: " + ex.Message);
			return true;
		}
	}
}
