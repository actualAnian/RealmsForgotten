using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using System;

namespace RealmsForgotten.Models
{
    internal class RFPartyHealingModel : DefaultPartyHealingModel
    {
        private PartyHealingModel partyHealingModel;

        public RFPartyHealingModel(PartyHealingModel partyHealingModel)
        {
            this.partyHealingModel = partyHealingModel;
        }
        public override ExplainedNumber GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions = false)
        {
            var value = partyHealingModel.GetDailyHealingForRegulars(party, includeDescriptions);
            if (party == MobileParty.MainParty) AddCareerPassivesForTroopRegeneration(party, ref value);
            return value;
        }
        public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
        {
            ExplainedNumber baseValue = partyHealingModel.GetDailyHealingHpForHeroes(party, includeDescriptions);
            if (party == MobileParty.MainParty) AddCareerPassivesForHeroRegeneration(party, ref baseValue);
            return baseValue;
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
        public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null)
        {
            float value = partyHealingModel.GetSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty);
            if (party == PartyBase.MainParty) value += AddCareerPassivesForSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty, ref value);
            return value;
        }

        private float AddCareerPassivesForSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty, ref float value)
        {
            ExplainedNumber num = new();
            if (PlayerCareerExtension.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(ref num, PassiveEffectType.HealthRegeneration, false);
            }
            return num.ResultNumber;
        }
    }
}