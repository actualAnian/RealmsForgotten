using System;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
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
