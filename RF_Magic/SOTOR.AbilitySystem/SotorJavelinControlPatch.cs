using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SOTOR.AbilitySystem;

[HarmonyPatchCategory("SotorMissionOnlyPatches")]
[HarmonyPatch(typeof(MissionMainAgentController), "ControlTick")]
public static class SotorJavelinControlPatch
{
	public static void Postfix()
	{
		try
		{
			SotorThrownJavelinMissionLogic.Instance?.DriveThrowFlagsFromControlTick();
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorJavelinControlPatch.Postfix failed: " + ex.Message);
		}
	}
}
