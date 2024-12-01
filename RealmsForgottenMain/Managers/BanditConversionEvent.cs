using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;


namespace RealmsForgotten.Career
{
    public class BanditConversionEvent : EventArgs
    {
        public Hero Hero { get; }
        public int BanditCount { get; }

        public BanditConversionEvent(Hero hero, int banditCount)
        {
            Hero = hero;
            BanditCount = banditCount;
        }
    }
}
