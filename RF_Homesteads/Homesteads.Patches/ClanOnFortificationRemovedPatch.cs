using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Clan), "OnFortificationRemoved")]
public static class ClanOnFortificationRemovedPatch
{
	public static bool Prefix(Clan __instance, Town settlement)
	{
		try
		{
			if (!__instance.IsReady)
			{
				TraceLogger.Write("ClanOnFortificationRemovedPatch", "Skipped OnFortificationRemoved for presumed clan " + __instance.StringId);
				return false;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("ClanOnFortificationRemovedPatch", "Prefix failed: " + ex.Message);
		}
		return true;
	}
}
