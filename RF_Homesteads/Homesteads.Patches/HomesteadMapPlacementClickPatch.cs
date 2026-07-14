using System;
using System.Reflection;
using HarmonyLib;
using Homesteads.Views;
using SandBox.View.Map;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadMapPlacementClickPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(MapScreen), "HandleLeftMouseButtonClick", new Type[4]
		{
			typeof(MapEntityVisual),
			typeof(CampaignVec2),
			typeof(PathFaceRecord),
			typeof(bool)
		});
	}

	[HarmonyPrefix]
	private static bool Prefix(MapEntityVisual? visualOfSelectedEntity, CampaignVec2 intersectionPoint, PathFaceRecord mouseOverFaceIndex)
	{
		HomesteadSettlementPlacementMapView active = HomesteadSettlementPlacementMapView.Active;
		if (active != null)
		{
			try
			{
				active.PlaceAt(intersectionPoint.ToVec2());
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadMapPlacementClickPatch", "settlement-place prefix failed: " + ex.GetType().Name + ": " + ex.Message);
			}
			return false;
		}
		HomesteadPlacementMapView active2 = HomesteadPlacementMapView.Active;
		if (active2 != null)
		{
			try
			{
				if (intersectionPoint.IsOnLand && mouseOverFaceIndex.IsValid())
				{
					active2.PlaceAt(intersectionPoint.ToVec2());
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("Cannot place homestead here. Invalid terrain.", Colors.Red));
				}
			}
			catch (Exception ex2)
			{
				TraceLogger.Write("HomesteadMapPlacementClickPatch", "Prefix failed: " + ex2.GetType().Name + ": " + ex2.Message);
			}
			return false;
		}
		return true;
	}
}
