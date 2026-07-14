using HarmonyLib;
using SandBox.ViewModelCollection.Map.Tracker;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MapTrackerCollectionVM), "Tick")]
internal class HomesteadMapAppearancePartyTrackerTickPatch
{
	private static float syncTimer;

	[HarmonyPostfix]
	private static void Postfix(MapTrackerCollectionVM __instance, float dt)
	{
		syncTimer += dt;
		if (!(syncTimer < 1f))
		{
			syncTimer = 0f;
			HomesteadMapTrackerSync.SyncHomesteadTrackers(__instance, "tick");
		}
	}
}
