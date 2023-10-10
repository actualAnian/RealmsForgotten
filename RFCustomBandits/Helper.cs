using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFCustomBandits
{
    internal static class Helper
    {

        internal static bool IsSlaverParty(this MobileParty mobileParty)
        {
            return mobileParty != null && mobileParty.Party != null && mobileParty.Party.Id.Contains("Slavers");
        }
        internal static bool IsSlaverParty(this PartyBase party)
        {
            return (party != null) && party.Id.Contains("Slavers");
        }
    }
}