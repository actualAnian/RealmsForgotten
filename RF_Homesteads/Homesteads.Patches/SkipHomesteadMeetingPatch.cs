using System;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PlayerEncounter), "DoMeetingInternal")]
internal class SkipHomesteadMeetingPatch
{
	[HarmonyPriority(800)]
	[HarmonyPrefix]
	private static bool Prefix()
	{
		try
		{
			if (PlayerEncounter.Current == null)
			{
				return true;
			}
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if (encounteredParty == null || !encounteredParty.IsMobile)
			{
				return true;
			}
			MobileParty mobileParty = encounteredParty.MobileParty;
			if (mobileParty == null)
			{
				return true;
			}
			if (Homestead.GetFor(mobileParty) != null)
			{
				TraceLogger.Write("SkipHomesteadMeetingPatch", "Skipped vanilla meeting for homestead party '" + mobileParty.StringId + "'.");
				return false;
			}
			if (HomesteadBattleContext.BypassMeetingSkip)
			{
				HomesteadBattleContext.BypassMeetingSkip = false;
				return true;
			}
			if (HomesteadBattleContext.HasPendingHomestead || HomesteadBehavior.Instance?.CurrentHomestead != null)
			{
				TraceLogger.Write("SkipHomesteadMeetingPatch", "Skipped vanilla meeting for homestead-defense encounter (attacker='" + mobileParty.StringId + "').");
				GameMenu.SwitchToMenu("homestead_menu_encounter");
				return false;
			}
			string text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			if (text != null && text.StartsWith("homestead_menu", StringComparison.Ordinal))
			{
				TraceLogger.Write("SkipHomesteadMeetingPatch", "Skipped vanilla meeting: player is in homestead menu '" + text + "' (attacker='" + mobileParty.StringId + "').");
				GameMenu.SwitchToMenu("homestead_menu_encounter");
				return false;
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("SkipHomesteadMeetingPatch", $"Exception in DoMeetingInternal prefix: {arg}");
		}
		return true;
	}
}
