using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

internal static class NavalDLCCrashFixPatch
{
	internal static void TryApply(Harmony harmony)
	{
		try
		{
			Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.Map.DistanceCache.SandBoxNavigationCache");
			if (!(type == null))
			{
				MethodInfo methodInfo = AccessTools.Method(type, "GetRealDistanceAndLandRatioBetweenSettlements");
				MethodInfo methodInfo2 = AccessTools.Method(typeof(NavalDLCCrashFixPatch), "Prefix");
				if (methodInfo != null && methodInfo2 != null)
				{
					harmony.Patch(methodInfo, new HarmonyMethod(methodInfo2));
					TraceLogger.Write("NavalDLCCrashFixPatch", "Successfully patched GetRealDistanceAndLandRatioBetweenSettlements to prevent NavalDLC crashes.");
				}
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("NavalDLCCrashFixPatch", $"Failed to patch NavalDLCCrashFixPatch: {arg}");
		}
	}

	private static bool Prefix(ref NavigationCacheElement<Settlement> settlement1, ref NavigationCacheElement<Settlement> settlement2, out float landRatio, ref float __result)
	{
		landRatio = 1f;
		try
		{
			bool isPortUsed = settlement1.IsPortUsed;
			bool isPortUsed2 = settlement2.IsPortUsed;
			CampaignVec2 campaignVec = (isPortUsed ? settlement1.PortPosition : settlement1.GatePosition);
			CampaignVec2 campaignVec2 = (isPortUsed2 ? settlement2.PortPosition : settlement2.GatePosition);
			if (campaignVec.Face.FaceIndex <= 0 || campaignVec2.Face.FaceIndex <= 0)
			{
				__result = 0f;
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
}
