using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.TradePact
{
    public class EconomicPact
    {
        public IFaction Faction1 { get; }
        public IFaction Faction2 { get; }
        public int DaysRemaining { get; private set; }
        public string Type { get; }

        public EconomicPact(IFaction f1, IFaction f2, int duration, string type)
        {
            Faction1 = f1;
            Faction2 = f2;
            DaysRemaining = duration;
            Type = type;
        }

        public void Tick()
        {
            DaysRemaining--;
        }

        public bool IsExpired() => DaysRemaining <= 0;
    }
}