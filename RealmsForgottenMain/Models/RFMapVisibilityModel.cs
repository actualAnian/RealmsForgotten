
using RealmsForgotten.Career;
using RealmsForgotten.Career.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.Models
{
    public class RFMapVisibilityModel : MapVisibilityModel
    {
        private MapVisibilityModel _baseModel;

        public RFMapVisibilityModel(MapVisibilityModel defaultModel)
        {
            _baseModel = defaultModel;
        }

        public override float GetHideoutSpottingDistance()
        {
            return _baseModel.GetHideoutSpottingDistance();
        }

        public override float GetPartySeeingRangeBase(MobileParty party)
        {
            return _baseModel.GetPartySeeingRangeBase(party);
        }

        public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
        {
            ExplainedNumber value = _baseModel.GetPartySpottingRange(party, includeDescriptions);
            if (party != MobileParty.MainParty || !PlayerCareerExtension.HasAnyCareer() || Hero.MainHero.PartyBelongedTo == null) return value;
            CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.SpottingRange);
            return value;
        }
        public override float GetPartySpottingRatioForMainPartySeeingRange(MobileParty party)
        {
            return _baseModel.GetPartySpottingRatioForMainPartySeeingRange(party);
        }

        public override float MaximumSeeingRange()
        {
            return _baseModel.MaximumSeeingRange();
        }
    }
}
