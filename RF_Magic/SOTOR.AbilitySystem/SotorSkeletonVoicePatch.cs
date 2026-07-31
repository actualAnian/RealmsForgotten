using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

[HarmonyPatchCategory("SotorMissionOnlyPatches")]
[HarmonyPatch(typeof(Agent), "MakeVoice")]
public static class SotorSkeletonVoicePatch
{
	private static bool Prefix(Agent __instance, SkinVoiceManager.SkinVoiceType voiceType)
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
			SotorLog.Warn("SkeletonVoicePatch failed: " + ex.Message);
			return true;
		}
	}
}
