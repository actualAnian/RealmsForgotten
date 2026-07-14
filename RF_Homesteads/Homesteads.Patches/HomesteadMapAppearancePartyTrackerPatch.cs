using HarmonyLib;
using SandBox.ViewModelCollection.Map.Tracker;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MapTrackerCollectionVM), MethodType.Constructor)]
internal class HomesteadMapAppearancePartyTrackerPatch
{
	[HarmonyPostfix]
	private static void Postfix(MapTrackerCollectionVM __instance)
	{
		HomesteadMapTrackerSync.SyncHomesteadTrackers(__instance, "constructor");
	}
}
