using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PartyCharacterVM), "ExecuteTalk")]
internal static class HomesteadPartyScreenTalkGuardPatch
{
	private static bool isHomesteadPartyScreenOpen;

	private static string? activeHomesteadPartyId;

	private static int homesteadPrisonerCapacity;

	internal static bool IsHomesteadPartyScreenOpen => isHomesteadPartyScreenOpen;

	internal static int HomesteadPrisonerCapacity => homesteadPrisonerCapacity;

	public static void BeginHomesteadPartyScreen(string? partyId, int prisonerCapacity)
	{
		isHomesteadPartyScreenOpen = true;
		activeHomesteadPartyId = partyId;
		homesteadPrisonerCapacity = prisonerCapacity;
		PartyScreenHelper.IsHomesteadPartyScreenOpen = true;
		TraceLogger.Write("HomesteadPartyScreenTalkGuardPatch", string.Format("Enabled homestead garrison screen (partyId={0}, prisonerCap={1})", partyId ?? "null", prisonerCapacity));
	}

	public static void EndHomesteadPartyScreen()
	{
		TraceLogger.Write("HomesteadPartyScreenTalkGuardPatch", "Disabled homestead garrison screen for '" + (activeHomesteadPartyId ?? "null") + "'");
		isHomesteadPartyScreenOpen = false;
		activeHomesteadPartyId = null;
		homesteadPrisonerCapacity = 0;
		PartyScreenHelper.IsHomesteadPartyScreenOpen = false;
	}

	[HarmonyPrefix]
	private static bool Prefix(PartyCharacterVM __instance)
	{
		if (!isHomesteadPartyScreenOpen)
		{
			return true;
		}
		if (__instance?.Character?.HeroObject != null)
		{
			return true;
		}
		TraceLogger.Write("HomesteadPartyScreenTalkGuardPatch", "Blocked ExecuteTalk for non-hero troop while managing homestead '" + (activeHomesteadPartyId ?? "unknown") + "'");
		return false;
	}
}
