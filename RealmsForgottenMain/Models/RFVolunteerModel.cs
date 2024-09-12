using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.AiMade.Career;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace RealmsForgotten.Models
{
    internal class RFVolunteerModel : DefaultVolunteerModel
    {
        private VolunteerModel _previousModel;
        
        public RFVolunteerModel(VolunteerModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            int baseValue = _previousModel.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);
            if (CustomSettings.Instance?.InfluenceCostForDifferentCultures == true)
            {
                IFaction buyerKingdom = buyerHero.MapFaction;
                if (buyerKingdom == null || buyerHero.Clan != null && buyerHero.Clan.IsClanTypeMercenary && buyerHero.Clan.IsMinorFaction || sellerHero.HomeSettlement.Owner == buyerHero)
                    return baseValue;
                if (buyerKingdom.IsAtWarWith(sellerHero.HomeSettlement.MapFaction) || (buyerHero.Clan?.Influence <= 0 && buyerHero?.Culture != sellerHero?.Culture))
                    return 0;
            }
            MercenaryVolunteerModel.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, ref baseValue);

            return baseValue;
        }
    }
}
