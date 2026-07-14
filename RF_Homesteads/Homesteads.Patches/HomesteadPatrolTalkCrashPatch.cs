using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PatrolPartiesCampaignBehavior), "patrol_talk_on_condition")]
internal static class HomesteadPatrolTalkCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, ref bool __result)
	{
		if (__exception == null)
		{
			return null;
		}
		TraceLogger.WriteOnce("PatrolTalkCrash", "HomesteadPatrolTalkCrashPatch", "patrol_talk_on_condition threw — suppressing the patrol greeting instead of crashing: " + __exception.GetType().Name + ": " + __exception.Message);
		__result = false;
		return null;
	}
}
