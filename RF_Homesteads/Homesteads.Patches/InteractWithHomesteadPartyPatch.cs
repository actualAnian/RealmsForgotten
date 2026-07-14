using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(EncounterManager), "StartPartyEncounter")]
internal class InteractWithHomesteadPartyPatch
{
	[HarmonyPrefix]
	private static bool Prefix(PartyBase attackerParty, PartyBase defenderParty)
	{
		if (attackerParty != PartyBase.MainParty)
		{
			return true;
		}
		MobileParty mobileParty = defenderParty.MobileParty;
		if (mobileParty != null && HomesteadRecruiterComponent.GetFor(mobileParty) != null)
		{
			if (mobileParty.MemberRoster.TotalManCount == 0)
			{
				TraceLogger.Write("InteractWithHomesteadPartyPatch", "Recruiter party '" + mobileParty.StringId + "' has 0 members — destroying and suppressing encounter.");
				try
				{
					DestroyPartyAction.Apply(null, mobileParty);
				}
				catch
				{
				}
				return false;
			}
			HomesteadRecruiterComponent homesteadRecruiterComponent = HomesteadRecruiterComponent.GetFor(mobileParty);
			if (homesteadRecruiterComponent != null)
			{
				HomesteadBehavior.Instance.CurrentRecruiterParty = mobileParty;
				HomesteadBehavior.Instance.CurrentRecruiterHomestead = homesteadRecruiterComponent.HomeHomestead;
				TraceLogger.Write("InteractWithHomesteadPartyPatch", "Recruiter encounter '" + mobileParty.StringId + "' homestead='" + (homesteadRecruiterComponent.HomeHomestead?.Name?.ToString() ?? "null") + "' - deferring to dialog intercept.");
			}
			return true;
		}
		MobileParty mobileParty2 = null;
		if (mobileParty != null)
		{
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			if (instance != null && instance.PatrolMobileParties.ContainsKey(mobileParty))
			{
				mobileParty2 = mobileParty;
				goto IL_013a;
			}
		}
		if (attackerParty.MobileParty != null)
		{
			HomesteadBehavior instance2 = HomesteadBehavior.Instance;
			if (instance2 != null && instance2.PatrolMobileParties.ContainsKey(attackerParty.MobileParty) && defenderParty == PartyBase.MainParty)
			{
				mobileParty2 = attackerParty.MobileParty;
			}
		}
		goto IL_013a;
		IL_013a:
		if (mobileParty2 != null && HomesteadBehavior.Instance != null)
		{
			if (mobileParty2.MemberRoster.TotalManCount == 0)
			{
				TraceLogger.Write("InteractWithHomesteadPartyPatch", "Patrol party '" + mobileParty2.StringId + "' has 0 members — destroying and suppressing encounter.");
				try
				{
					DestroyPartyAction.Apply(null, mobileParty2);
				}
				catch
				{
				}
				return false;
			}
			HomesteadBehavior.Instance.PatrolMobileParties.TryGetValue(mobileParty2, out Homestead value);
			HomesteadBehavior.Instance.CurrentPatrolHomestead = value;
			HomesteadBehavior.Instance.CurrentPatrolParty = mobileParty2;
			TraceLogger.Write("InteractWithHomesteadPartyPatch", "Patrol encounter '" + mobileParty2.StringId + "' homestead='" + (value?.Name?.ToString() ?? "null") + "' - deferring to dialog intercept.");
			return true;
		}
		Homestead homestead = Homestead.GetFor(mobileParty);
		if (homestead == null)
		{
			return true;
		}
		if (homestead.MobileParty.MapEvent != null)
		{
			return true;
		}
		TraceLogger.Write("InteractWithHomesteadPartyPatch", $"Intercepting StartPartyEncounter for homestead '{homestead.Name}'");
		HomesteadBehavior.Instance.CurrentHomestead = homestead;
		MobileParty.MainParty.SetMoveModeHold();
		GameMenu.ActivateGameMenu("homestead_menu_main");
		HomesteadTutorial.LaunchedMenu();
		TraceLogger.Write("InteractWithHomesteadPartyPatch", "Homestead menu opened without PlayerEncounter — homestead party remains attackable.");
		return false;
	}
}
