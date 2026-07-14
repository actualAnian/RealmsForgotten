using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(TownManagementVM), "RefreshCurrentDevelopment")]
internal static class HomesteadTownManagementRefreshCrashPatch
{
	private static readonly FieldInfo? SettlementField = AccessTools.Field(typeof(TownManagementVM), "_settlement");

	private static readonly PropertyInfo? ProjectSelectionProp = AccessTools.Property(typeof(TownManagementVM), "ProjectSelection");

	private static readonly PropertyInfo? CurrentSelectedProjectProp = AccessTools.Property(typeof(SettlementProjectSelectionVM), "CurrentSelectedProject");

	[HarmonyPrefix]
	private static bool Prefix(TownManagementVM __instance)
	{
		try
		{
			if (!(SettlementField?.GetValue(__instance) is Settlement { StringId: not null } settlement) || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			Building building = settlement.Town?.CurrentBuilding;
			if (building == null || building.BuildingType.IsDailyProject)
			{
				return true;
			}
			object obj = ProjectSelectionProp?.GetValue(__instance);
			if (((obj == null) ? null : CurrentSelectedProjectProp?.GetValue(obj)) != null)
			{
				return true;
			}
			TraceLogger.WriteOnce("TownManagementRefresh:" + settlement.StringId, "HomesteadTownManagementRefreshCrashPatch", "'" + settlement.StringId + "' has a non-daily CurrentBuilding ('" + building.BuildingType?.StringId + "') but ProjectSelection.CurrentSelectedProject is null — skipping RefreshCurrentDevelopment to avoid a crash opening the management screen.");
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTownManagementRefreshCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
