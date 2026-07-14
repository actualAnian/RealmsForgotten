using System.Runtime.CompilerServices;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.ObjectSystem;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(BuildingHelper), "ChangeDefaultBuilding")]
public static class HomesteadChangeDefaultBuildingDiagPatch
{
	public static void Postfix(Building newDefault, Town town)
	{
		try
		{
			string text = town?.Settlement?.StringId;
			if (text != null && text.StartsWith("hsr_settlement_"))
			{
				Town town2 = MBObjectManager.Instance.GetObject<Settlement>(text)?.Town;
				TraceLogger.Write("HomesteadChangeDefaultBuildingDiagPatch", "DIAG ChangeDefaultBuilding '" + text + "': picked='" + (newDefault?.BuildingType?.StringId ?? "null") + "', " + $"objId={RuntimeHelpers.GetHashCode(town)}, canonicalId={((town2 != null) ? RuntimeHelpers.GetHashCode(town2) : 0)}, " + $"sameObject={town == town2}, " + $"postDefaultFlag='{FlagId(town)}', queueBusy={town.BuildingsInProgress.Count > 0}.");
			}
		}
		catch
		{
		}
	}

	private static string FlagId(Town town)
	{
		foreach (Building building in town.Buildings)
		{
			if (building.IsCurrentlyDefault)
			{
				return building.BuildingType?.StringId ?? "?";
			}
		}
		return "null";
	}
}
