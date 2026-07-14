using HarmonyLib;
using SandBox.View.Map.Visuals;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MobilePartyVisual), "Tick")]
internal class HomesteadMobilePartyVisualTickPatch
{
	[HarmonyPostfix]
	private static void Postfix(MobilePartyVisual __instance)
	{
	}
}
