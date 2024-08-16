using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using RealmsForgotten.Career;

namespace RealmsForgotten.Career
{
    public static class BanditConversionManager
    {
        public static event EventHandler<BanditConversionEvent> BanditConverted;

        public static void OnBanditConverted(Hero hero, int banditCount)
        {
            BanditConverted?.Invoke(null, new BanditConversionEvent(hero, banditCount));
        }
    }
}