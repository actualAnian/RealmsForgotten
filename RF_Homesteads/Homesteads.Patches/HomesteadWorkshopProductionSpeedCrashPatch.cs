using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultWorkshopModel), "GetEffectiveConversionSpeedOfProduction")]
internal static class HomesteadWorkshopProductionSpeedCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Workshop workshop, float speed, bool includeDescription, ref ExplainedNumber __result)
	{
		try
		{
			Settlement settlement = workshop?.Settlement;
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			bool flag = settlement.OwnerClan?.Leader == null;
			bool flag2 = workshop.Owner?.CharacterObject == null;
			if (!flag && !flag2)
			{
				return true;
			}
			TraceLogger.Write("HomesteadWorkshopProductionSpeedCrashPatch", $"'{settlement.StringId}' workshop production speed: ownerClanNotReady={flag} workshopOwnerMissing={flag2} — returning base speed instead of crashing.");
			__result = new ExplainedNumber(speed, includeDescription);
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadWorkshopProductionSpeedCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
