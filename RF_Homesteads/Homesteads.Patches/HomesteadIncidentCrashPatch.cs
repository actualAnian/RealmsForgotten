using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Incidents;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Incident), "CanIncidentBeInvoked")]
internal static class HomesteadIncidentCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Incident __instance, Exception __exception, ref bool __result)
	{
		if (__exception == null)
		{
			return null;
		}
		TraceLogger.WriteOnce("IncidentCrash:" + __instance?.StringId, "HomesteadIncidentCrashPatch", "Incident '" + __instance?.StringId + "' condition threw — treating as not invokable instead of crashing: " + __exception.GetType().Name + ": " + __exception.Message);
		__result = false;
		return null;
	}
}
