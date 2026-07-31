using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.CampaignBehaviors;

[HarmonyPatch(typeof(Mission), "SpawnTroop")]
public static class SotorGraveyardMountPatch
{
	public static bool SuppressPlayerMount;

	public static void Prefix(IAgentOriginBase troopOrigin, ref bool forceDismounted)
	{
		try
		{
			if (SuppressPlayerMount && troopOrigin != null && troopOrigin.Troop != null && troopOrigin.Troop.IsPlayerCharacter)
			{
				forceDismounted = true;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorGraveyardMountPatch.Prefix failed: " + ex.Message);
		}
	}
}
