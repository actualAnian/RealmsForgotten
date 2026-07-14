using System;
using HarmonyLib;
using SandBox.Missions.MissionLogics.Towns;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(WorkshopMissionHandler), "InitShopSigns")]
public static class HomesteadWorkshopSignCrashPatch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement == null || currentSettlement.StringId == null || !currentSettlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			TraceLogger.Write("HomesteadWorkshopSignCrashPatch", "Skipped InitShopSigns for '" + currentSettlement.StringId + "' (our settlements don't have the full marker set this handler expects).");
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadWorkshopSignCrashPatch", "Prefix error: " + ex.Message);
			return true;
		}
	}
}
