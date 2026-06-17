using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Knighthood
{
    public static class RFClanExtension  
    {
       
        public static IReadOnlyCollection<Hero>? GetKnights(this Clan? clan)
        {
            return clan == null ? null : RFExtension.BiMap.GetMany(clan);
        }
    }
}

