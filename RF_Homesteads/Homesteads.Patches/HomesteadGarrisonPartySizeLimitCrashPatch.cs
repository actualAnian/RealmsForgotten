using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultPartySizeLimitModel), "CalculateGarrisonPartySizeLimit")]
internal static class HomesteadGarrisonPartySizeLimitCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Settlement settlement, bool includeDescriptions, ref ExplainedNumber __result)
	{
		try
		{
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			if (settlement.OwnerClan?.Leader != null)
			{
				return true;
			}
			TraceLogger.Write("HomesteadGarrisonPartySizeLimitCrashPatch", "'" + settlement.StringId + "' has no ready OwnerClan/Leader yet — returning a default garrison size limit instead of crashing.");
			float baseNumber = (settlement.IsTown ? 400f : 200f);
			__result = new ExplainedNumber(baseNumber, includeDescriptions);
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadGarrisonPartySizeLimitCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
