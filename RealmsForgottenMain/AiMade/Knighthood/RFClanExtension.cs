using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

