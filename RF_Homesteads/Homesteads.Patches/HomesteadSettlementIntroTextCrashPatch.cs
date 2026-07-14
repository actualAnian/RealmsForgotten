using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "SetIntroductionText")]
internal static class HomesteadSettlementIntroTextCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Settlement settlement, bool fromKeep)
	{
		try
		{
			if (settlement == null || settlement.StringId == null || !settlement.StringId.StartsWith("hsr_"))
			{
				return true;
			}
			Clan ownerClan = settlement.OwnerClan;
			if (ownerClan != null && ownerClan.IsReady && ownerClan.Leader != null)
			{
				return true;
			}
			TraceLogger.Write("HomesteadSettlementIntroTextCrashPatch", "'" + settlement.StringId + "' has no ready OwnerClan/Leader yet — using a minimal placeholder introduction text instead of crashing.");
			TextObject textObject = new TextObject("{=homestead_settlement_intro_fallback}You have arrived at {SETTLEMENT_LINK}.");
			textObject.SetTextVariable("SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("SETTLEMENT_INFO", textObject);
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementIntroTextCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}
}
