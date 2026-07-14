using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(WorkshopsCampaignBehavior), "RunTownWorkshop")]
internal static class HomesteadRunTownWorkshopCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Town townComponent, Workshop workshop)
	{
		try
		{
			Settlement settlement = townComponent?.Settlement ?? workshop?.Settlement;
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			if (workshop != null && workshop.Settlement?.Town == null && townComponent?.Settlement?.Town != null)
			{
				try
				{
					AccessTools.Field(typeof(Workshop), "_settlement")?.SetValue(workshop, townComponent.Settlement);
					TraceLogger.Write("HomesteadRunTownWorkshopCrashPatch", "Re-pointed workshop '" + workshop.WorkshopType?.StringId + "' of '" + townComponent.Settlement.StringId + "' from a stale settlement object to the canonical one — production resumes.");
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadRunTownWorkshopCrashPatch", "_settlement re-point failed: " + ex.Message);
				}
			}
			if (workshop == null || workshop.WorkshopType == null || workshop.Owner == null || workshop.Settlement?.Town == null || townComponent?.Owner == null)
			{
				TraceLogger.Write("HomesteadRunTownWorkshopCrashPatch", "Skipped RunTownWorkshop for '" + settlement.StringId + "' this tick — incomplete workshop state " + $"(type={workshop?.WorkshopType != null} owner={workshop?.Owner != null} " + $"wsTown={workshop?.Settlement?.Town != null} townOwner={townComponent?.Owner != null}).");
				return false;
			}
			return true;
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadRunTownWorkshopCrashPatch", "Prefix failed: " + ex2.Message);
			return true;
		}
	}
}
