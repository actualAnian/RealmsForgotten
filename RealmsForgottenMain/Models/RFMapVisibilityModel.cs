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
    internal class RFMapVisibilityModel : MapVisibilityModel
    {
        private MapVisibilityModel baseModel;

        public RFMapVisibilityModel(MapVisibilityModel defaultModel)
        {
            baseModel = defaultModel;
        }

        public override float GetHideoutSpottingDistance()
        {
            return baseModel.GetHideoutSpottingDistance();
        }

        public override float GetPartyRelativeInspectionRange(IMapPoint party)
        {
            return baseModel.GetPartyRelativeInspectionRange(party);
        }

        public override float GetPartySpottingDifficulty(MobileParty spotterParty, MobileParty party)
        {
            return baseModel.GetPartySpottingDifficulty(spotterParty, party);
        }

        public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
        {
            ExplainedNumber value = baseModel.GetPartySpottingRange(party, includeDescriptions);
            if (party != MobileParty.MainParty || !PlayerCareerExtension.HasAnyCareer() || Hero.MainHero.PartyBelongedTo == null) return value;
            CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.SpottingRange, true);
            return value;
        }
    }
}
