using HarmonyLib;
using SandBox.ViewModelCollection.Map.Tracker;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MapTrackerCollectionVM), "UpdateProperties")]
internal class HomesteadMapAppearancePartyTrackerRefreshPatch
{
	[HarmonyPostfix]
	private static void Postfix(MapTrackerCollectionVM __instance)
	{
		HomesteadMapTrackerSync.SyncHomesteadTrackers(__instance, "update");
	}
}
