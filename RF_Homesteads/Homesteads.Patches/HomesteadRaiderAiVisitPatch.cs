using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
internal class HomesteadRaiderAiVisitPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty mobileParty)
	{
		return !(mobileParty?.PartyComponent is HomesteadRaiderPartyComponent);
	}
}
