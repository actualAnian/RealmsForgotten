using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(UsableMachine), "GetValidStandingPointForAgentWithoutDistanceCheck")]
internal static class HomesteadUsableMachineStandingPointCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Agent agent, ref WeakGameEntity __result)
	{
		if (agent == null)
		{
			__result = WeakGameEntity.Invalid;
			TraceLogger.Write("HomesteadUsableMachineStandingPointCrashPatch", "Skipped GetValidStandingPointForAgentWithoutDistanceCheck for a null agent (e.g. player dead) — avoided native NRE.");
			return false;
		}
		return true;
	}
}
