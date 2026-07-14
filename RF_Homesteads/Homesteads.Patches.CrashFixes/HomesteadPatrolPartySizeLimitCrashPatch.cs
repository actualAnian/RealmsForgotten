using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches.CrashFixes;

[HarmonyPatch(typeof(DefaultPartySizeLimitModel), "CalculatePatrolPartySizeLimit")]
internal static class HomesteadPatrolPartySizeLimitCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(MobileParty mobileParty, bool includeDescriptions, ref ExplainedNumber __result)
	{
		try
		{
			if (mobileParty?.HomeSettlement?.Town == null)
			{
				TraceLogger.Write("HomesteadPatrolPartySizeLimitCrashPatch", "Prevented crash: patrol party " + mobileParty?.StringId + " has null HomeSettlement or Town.");
				__result = new ExplainedNumber(10f, includeDescriptions);
				return false;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadPatrolPartySizeLimitCrashPatch", "Prefix failed: " + ex.Message);
		}
		return true;
	}
}
