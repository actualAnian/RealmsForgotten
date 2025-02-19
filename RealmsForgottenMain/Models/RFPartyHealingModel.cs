using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace RealmsForgotten.Models
{
    internal class RFPartyHealingModel : DefaultPartyHealingModel
    {
        private PartyHealingModel partyHealingModel;

        public RFPartyHealingModel(PartyHealingModel partyHealingModel)
        {
            this.partyHealingModel = partyHealingModel;
        }
        public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
        {
            ExplainedNumber baseValue = partyHealingModel.GetDailyHealingHpForHeroes(party, includeDescriptions);
            if (party == MobileParty.MainParty) AddCareerPassivesForHeroRegeneration(party, ref baseValue);
            return base.GetDailyHealingHpForHeroes(party, includeDescriptions);
        }
        private void AddCareerPassivesForTroopRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (PlayerCareerExtension.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(ref explainedNumber, PassiveEffectType.TroopRegeneration, false);
            }
        }

        private void AddCareerPassivesForHeroRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (PlayerCareerExtension.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(ref explainedNumber, PassiveEffectType.HealthRegeneration, false);
            }
        }
    }
}