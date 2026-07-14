using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MobileParty), "ShortTermBehavior", MethodType.Setter)]
internal class StopHomesteadPartyMovingShortTermBehaviorPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty __instance)
	{
		return !HomesteadPartyBlocker.ShouldBlockMainPartyMovement(__instance);
	}
}
