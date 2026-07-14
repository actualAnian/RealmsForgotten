using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultHeroAgentLocationModel), "GetLocationForHero")]
internal static class HomesteadSettlementEntryCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, Settlement settlement, ref Location __result, ref HeroAgentLocationModel.HeroLocationDetail heroLocationDetail)
	{
		if (__exception == null)
		{
			return null;
		}
		if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_settlement_"))
		{
			return __exception;
		}
		heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.None;
		__result = null;
		TraceLogger.Write("HomesteadSettlementEntryCrashPatch", "Swallowed GetLocationForHero error for '" + settlement.StringId + "': " + __exception.Message);
		return null;
	}
}
