using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MapTracksCampaignBehavior), "OnHourlyTickParty")]
internal class HomesteadMapTracksCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty mobileParty)
	{
		if (mobileParty?.Party == null)
		{
			TraceLogger.Write("HomesteadMapTracksCrashPatch", "Skipping map-track tick for null or removed mobile party.");
			return false;
		}
		if (Homestead.GetFor(mobileParty) != null)
		{
			TraceLogger.Write("HomesteadMapTracksCrashPatch", "Skipping map-track tick for homestead party '" + mobileParty.StringId + "'.");
			return false;
		}
		if (HomesteadBehavior.Instance != null && HomesteadBehavior.Instance.PatrolMobileParties.ContainsKey(mobileParty))
		{
			TraceLogger.Write("HomesteadMapTracksCrashPatch", "Skipping map-track tick for patrol party '" + mobileParty.StringId + "'.");
			return false;
		}
		if (HomesteadRecruiterComponent.GetFor(mobileParty) != null)
		{
			TraceLogger.Write("HomesteadMapTracksCrashPatch", "Skipping map-track tick for recruiter party '" + mobileParty.StringId + "'.");
			return false;
		}
		if (mobileParty.PartyComponent is HomesteadRaiderPartyComponent)
		{
			TraceLogger.Write("HomesteadMapTracksCrashPatch", "Skipping map-track tick for raider party '" + mobileParty.StringId + "'.");
			return false;
		}
		return true;
	}
}
