using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PartyCharacterVM), "UpdateTalkable")]
internal static class HomesteadPartyScreenTalkableStatePatch
{
	[HarmonyPostfix]
	private static void Postfix(PartyCharacterVM __instance)
	{
		if (HomesteadPartyScreenTalkGuardPatch.IsHomesteadPartyScreenOpen && __instance?.Character?.HeroObject == null)
		{
			__instance.CanTalk = false;
			__instance.IsTalkableCharacter = false;
		}
	}
}
