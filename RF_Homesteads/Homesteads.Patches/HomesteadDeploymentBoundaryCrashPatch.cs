using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadDeploymentBoundaryCrashPatch
{
	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer.MissionDeploymentBoundaryMarker");
		if (!(type == null))
		{
			return AccessTools.Method(type, "OnDeploymentPlanMade", new Type[2]
			{
				typeof(Team),
				typeof(bool)
			});
		}
		return null;
	}

	[HarmonyFinalizer]
	private static Exception? Finalizer(Exception? __exception)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (Mission.Current?.GetMissionBehavior<HomesteadBattleSceneMissionLogic>() != null)
			{
				TraceLogger.Write("HomesteadDeploymentBoundaryCrashPatch", "Suppressed MissionDeploymentBoundaryMarker.OnDeploymentPlanMade crash in homestead battle: " + __exception.Message);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}
}
