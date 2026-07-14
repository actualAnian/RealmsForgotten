using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.LordConversationsCampaignBehavior", "conversation_clan_member_manage_troops_on_condition", MethodType.Normal)]
internal static class DisableVanillaInspectTroopsPatch
{
	[HarmonyPostfix]
	private static void Postfix(ref bool __result)
	{
		if (__result && MobileParty.ConversationParty != null && HomesteadPartyBlocker.IsHomesteadMainParty(MobileParty.ConversationParty))
		{
			__result = false;
		}
	}
}
