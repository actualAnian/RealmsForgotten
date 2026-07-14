using HarmonyLib;
using Homesteads.Views;
using SandBox.View.Map;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MapCameraView), "OnBeforeTick")]
internal static class HomesteadMapCameraInputPatch
{
	[HarmonyPrefix]
	private static void Prefix(ref MapCameraView.InputInformation inputInformation)
	{
		if (HomesteadSettlementPlacementMapView.Active != null)
		{
			inputInformation.RotateLeftKeyDown = false;
			inputInformation.RotateRightKeyDown = false;
		}
	}
}
