using System;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
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