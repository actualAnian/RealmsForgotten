using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Knighthood
{
    public static class RFExtension
    {
        public static readonly BiDirectionalMap<Clan, Hero> BiMap = new();

        public static bool IsKnight(this Hero? hero)
        {
            return hero != null && BiMap.ContainsMany(hero);
        }

        public static bool MarkAsKnight(this Hero? hero, Clan? clan)
        {
            return hero != null && clan != null && BiMap.Add(clan, hero);
        }

        public static void SyncData(IDataStore dataStore)
        {
            BiMap.SyncData(dataStore, nameof(RFExtension));
        }
    }
}
