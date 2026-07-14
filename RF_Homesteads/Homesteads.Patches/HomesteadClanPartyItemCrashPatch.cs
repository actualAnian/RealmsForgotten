using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.ObjectSystem;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(ClanPartyItemVM), MethodType.Constructor, new Type[]
{
	typeof(PartyBase),
	typeof(Action<ClanPartyItemVM>),
	typeof(Action),
	typeof(Action),
	typeof(ClanPartyItemVM.ClanPartyType),
	typeof(IDisbandPartyCampaignBehavior),
	typeof(ITeleportationCampaignBehavior)
})]
internal static class HomesteadClanPartyItemCrashPatch
{
	[HarmonyPrefix]
	private static void Prefix(PartyBase party)
	{
		try
		{
			MobileParty mobileParty = party?.MobileParty;
			Settlement settlement = mobileParty?.CurrentSettlement;
			if (settlement != null && settlement.Town == null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_"))
			{
				Settlement settlement2 = MBObjectManager.Instance.GetObject<Settlement>(settlement.StringId);
				if (settlement2?.Town != null)
				{
					mobileParty.CurrentSettlement = settlement2;
					TraceLogger.Write("HomesteadClanPartyItemCrashPatch", "Re-pointed stale CurrentSettlement for '" + settlement.StringId + "' onto the canonical settlement before ClanPartyItemVM construction.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadClanPartyItemCrashPatch", "Prefix failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
