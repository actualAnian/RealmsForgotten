using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
internal class HomesteadAiVisitSettlementCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty mobileParty)
	{
		if (HomesteadBehavior.Instance == null)
		{
			return true;
		}
		if (HomesteadBehavior.Instance.HomesteadMobileParties.Count == 0 && HomesteadBehavior.Instance.PatrolMobileParties.Count == 0)
		{
			return true;
		}
		Homestead? homestead = Homestead.GetFor(mobileParty);
		HomesteadRecruiterComponent homesteadRecruiterComponent = HomesteadRecruiterComponent.GetFor(mobileParty);
		if (homestead != null || homesteadRecruiterComponent != null)
		{
			TraceLogger.Write("HomesteadAiVisitSettlementCrashPatch", "Skipping AiHourlyTick for Homesteads custom party '" + (mobileParty?.StringId ?? "null") + "'");
			return false;
		}
		if (HomesteadBehavior.Instance.PatrolMobileParties.ContainsKey(mobileParty))
		{
			return false;
		}
		if (mobileParty?.PartyComponent is HomesteadRaiderPartyComponent)
		{
			return false;
		}
		if (HomesteadBehavior.Instance.IsCaravanRedirected(mobileParty))
		{
			return false;
		}
		return true;
	}
}
