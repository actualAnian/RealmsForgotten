using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(GarrisonPartyComponent), "PartyOwner", MethodType.Getter)]
internal static class HomesteadGarrisonPartyOwnerCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(GarrisonPartyComponent __instance, ref Hero __result)
	{
		try
		{
			Settlement settlement = __instance.Settlement;
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			Clan ownerClan = settlement.OwnerClan;
			if (ownerClan == null || !ownerClan.IsReady || ownerClan.Leader == null)
			{
				TraceLogger.WriteOnce("GarrisonPartyOwnerFallback:" + settlement.StringId, "HomesteadGarrisonPartyOwnerCrashPatch", "'" + settlement.StringId + "' has no ready OwnerClan/Leader yet — returning Clan.PlayerClan's leader as a fallback PartyOwner instead of crashing.");
				__result = Clan.PlayerClan?.Leader;
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadGarrisonPartyOwnerCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
