using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;

namespace RealmsForgotten.Models
{
    internal class RFClanPoliticsModel : ClanPoliticsModel
    {
        private ClanPoliticsModel _previousModel;

        public RFClanPoliticsModel(ClanPoliticsModel previousModel)
        {
            _previousModel = previousModel;
        }

        public override ExplainedNumber CalculateInfluenceChange(Clan clan, bool includeDescriptions = false)
        {
            ExplainedNumber value = _previousModel.CalculateInfluenceChange(clan, includeDescriptions);
            if (clan != Clan.PlayerClan) return value;

            //CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.DailyInfluence, false);
            return value;
        }

        public override float CalculateRelationshipChangeWithSponsor(Clan clan, Clan sponsorClan)
        {
            return _previousModel.CalculateRelationshipChangeWithSponsor(clan, sponsorClan);
        }

        public override float CalculateSupportForPolicyInClan(Clan clan, PolicyObject policy)
        {
            return _previousModel.CalculateSupportForPolicyInClan(clan, policy);
        }

        public override bool CanHeroBeGovernor(Hero hero)
        {
            return _previousModel.CanHeroBeGovernor(hero);
        }

        public override int GetInfluenceRequiredToOverrideKingdomDecision(DecisionOutcome popularOption, DecisionOutcome overridingOption, KingdomDecision decision)
        {
            return _previousModel.GetInfluenceRequiredToOverrideKingdomDecision(popularOption, overridingOption, decision);
        }
    }

}
