using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace RealmsForgotten.AiMade.Career
{
    public static class MercenaryVolunteerModel
    {
        private const int RequiredInfluence = 50; // Example required influence amount

        public static void MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, ref int baseValue)
        {
            if (CustomSettings.Instance?.InfluenceCostForDifferentCultures == true)
            {
                IFaction buyerKingdom = buyerHero.MapFaction;
                if (buyerKingdom == null || IsMinorOrMercenaryClan(buyerHero) || sellerHero.HomeSettlement.Owner == buyerHero)
                    return;

                if (buyerKingdom.IsAtWarWith(sellerHero.HomeSettlement.MapFaction) || !HasSufficientInfluenceWithFaction(buyerHero, sellerHero, RequiredInfluence))
                    baseValue = 0;
            }
        }

        private static bool HasSufficientInfluenceWithFaction(Hero buyerHero, Hero sellerHero, int requiredInfluence)
        {
            // Check if the buyer hero's clan has at least the required influence and cultures match or not
            return buyerHero.Clan != null && (buyerHero.Clan.Influence >= requiredInfluence || buyerHero.Culture == sellerHero.Culture);
        }

        private static bool IsMinorOrMercenaryClan(Hero hero)
        {
            // Check if the hero belongs to a minor faction or is a mercenary
            return hero.Clan != null && (hero.Clan.IsClanTypeMercenary || hero.Clan.IsMinorFaction);
        }
    }
}