using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.Models
{
    public class RFPartySizeLimitModel : DefaultPartySizeLimitModel
    {
        private PartySizeLimitModel baseModel;

        public RFPartySizeLimitModel(PartySizeLimitModel defaultModel) 
        {
            baseModel = defaultModel;
        }
        public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
        {
            ExplainedNumber value = baseModel.GetPartyMemberSizeLimit(party, includeDescriptions);
            if (party != null && party.LeaderHero != null && party.LeaderHero == Hero.MainHero && PlayerCareerExtension.HasAnyCareer())
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.PartySize, false);
            return value;
        }
    }
}
