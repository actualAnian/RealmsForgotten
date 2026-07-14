using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PartyScreenLogic), "Initialize")]
internal static class HomesteadPrisonerCapacityInitPatch
{
	private static readonly MethodInfo? LeftPrisonerSizeSetter = AccessTools.PropertySetter(typeof(PartyScreenLogic), "LeftPartyPrisonersSizeLimit");

	[HarmonyPostfix]
	private static void Postfix(PartyScreenLogic __instance)
	{
		if (HomesteadPartyScreenTalkGuardPatch.IsHomesteadPartyScreenOpen)
		{
			int homesteadPrisonerCapacity = HomesteadPartyScreenTalkGuardPatch.HomesteadPrisonerCapacity;
			LeftPrisonerSizeSetter?.Invoke(__instance, new object[1] { homesteadPrisonerCapacity });
			TraceLogger.Write("HomesteadPrisonerCapacityInitPatch", $"Overwrote LeftPartyPrisonersSizeLimit = {homesteadPrisonerCapacity} (setter found: {LeftPrisonerSizeSetter != null})");
		}
	}
}
