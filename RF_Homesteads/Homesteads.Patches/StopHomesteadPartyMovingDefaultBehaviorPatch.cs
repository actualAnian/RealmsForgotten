using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MobileParty), "set_DefaultBehavior")]
internal class StopHomesteadPartyMovingDefaultBehaviorPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty __instance)
	{
		return !HomesteadPartyBlocker.ShouldBlockMainPartyMovement(__instance);
	}
}
