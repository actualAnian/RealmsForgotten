using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.CustomBandits
{
    internal static class Helper
    {
        internal static bool IsSlaverParty(this MobileParty mobileParty)
        {
            return mobileParty != null && mobileParty.Party != null && mobileParty.Party.Id.Contains("Slavers");
        }
        internal static bool IsSlaverParty(this PartyBase party)
        {
            return party != null && party.Id.Contains("Slavers");
        }
    }
}