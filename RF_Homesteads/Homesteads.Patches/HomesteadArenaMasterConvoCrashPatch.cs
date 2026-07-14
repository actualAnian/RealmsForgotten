using System;
using HarmonyLib;
using SandBox.CampaignBehaviors;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(ArenaMasterCampaignBehavior), "conversation_arena_master_player_knows_arenas_on_condition")]
internal static class HomesteadArenaMasterConvoCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, ref bool __result)
	{
		if (__exception == null)
		{
			return null;
		}
		TraceLogger.WriteOnce("ArenaMasterConvoCrash", "HomesteadArenaMasterConvoCrashPatch", "conversation_arena_master_player_knows_arenas_on_condition threw — treating as false instead of crashing: " + __exception.GetType().Name + ": " + __exception.Message);
		__result = false;
		return null;
	}
}
