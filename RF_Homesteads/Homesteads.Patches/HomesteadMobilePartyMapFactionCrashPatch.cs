using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MobileParty), "MapFaction", MethodType.Getter)]
internal static class HomesteadMobilePartyMapFactionCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty __instance, ref IFaction __result)
	{
		try
		{
			Settlement homeSettlement = __instance.HomeSettlement;
			if (homeSettlement == null || homeSettlement.StringId == null || !homeSettlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			Clan ownerClan = homeSettlement.OwnerClan;
			if (ownerClan == null || !ownerClan.IsReady)
			{
				TraceLogger.WriteOnce("MapFactionFallback:" + __instance.StringId, "HomesteadMobilePartyMapFactionCrashPatch", "Party '" + __instance.StringId + "' home settlement '" + homeSettlement.StringId + "' has no ready OwnerClan yet — returning Clan.PlayerClan as a fallback MapFaction instead of crashing.");
				__result = Clan.PlayerClan;
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadMobilePartyMapFactionCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
