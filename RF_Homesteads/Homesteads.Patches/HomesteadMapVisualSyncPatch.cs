using HarmonyLib;
using SandBox.View.Map.Managers;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MobilePartyVisualManager), "OnVisualTick")]
internal class HomesteadMapVisualSyncPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		HomesteadBehavior.Instance?.SyncHomesteadMapVisuals("map visual tick");
	}
}
