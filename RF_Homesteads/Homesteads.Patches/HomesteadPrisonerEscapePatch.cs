using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch("TaleWorlds.CampaignSystem.CampaignBehaviors.PrisonerCaptureCampaignBehavior", "DailyHeroTick", MethodType.Normal)]
internal static class HomesteadPrisonerEscapePatch
{
	[HarmonyPrefix]
	private static bool Prefix(Hero hero)
	{
		try
		{
			PartyBase partyBase = hero?.PartyBelongedToAsPrisoner;
			if (partyBase == null || !partyBase.IsMobile || partyBase.MobileParty == null)
			{
				return true;
			}
			if (Homestead.GetFor(partyBase.MobileParty) != null)
			{
				goto IL_0052;
			}
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			if (instance != null && instance.PatrolMobileParties.ContainsKey(partyBase.MobileParty))
			{
				goto IL_0052;
			}
			goto end_IL_0000;
			IL_0052:
			return false;
			end_IL_0000:;
		}
		catch
		{
		}
		return true;
	}
}
