using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.Models
{
    internal class RFVolunteerModel : DefaultVolunteerModel
    {

        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            int baseValue = base.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);
            if (CustomSettings.Instance?.InfluenceCostForDifferentCultures == true)
            {
                IFaction buyerKingdom = buyerHero.MapFaction;
                if (buyerKingdom == null || buyerHero.Clan != null && buyerHero.Clan.IsClanTypeMercenary && buyerHero.Clan.IsMinorFaction)
                    return baseValue;
                if (buyerKingdom.IsAtWarWith(sellerHero.HomeSettlement.MapFaction) || (buyerHero.Clan?.Influence <= 0 && buyerHero?.Culture != sellerHero?.Culture))
                    return 0;
            }

            return baseValue;
        }
    }
}
