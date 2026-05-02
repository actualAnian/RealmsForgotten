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
    internal class RFPartyHealingModel : PartyHealingModel
    {
        private PartyHealingModel _baseModel;

        public RFPartyHealingModel(PartyHealingModel partyHealingModel)
        {
            _baseModel = partyHealingModel;
        }
        private void AddCareerPassivesForTroopRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (PlayerCareerExtension.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(ref explainedNumber, PassiveEffectType.TroopRegeneration);
            }
        }

        private void AddCareerPassivesForHeroRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (PlayerCareerExtension.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(ref explainedNumber, PassiveEffectType.HealthRegeneration);
            }
        }
        public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null)
        {
            float value = _baseModel.GetSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty);
            if (party == PartyBase.MainParty) value += AddCareerPassivesForSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty, ref value);
            return value;
        }

        private float AddCareerPassivesForSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty, ref float value)
        {
            ExplainedNumber num = new();
            if (PlayerCareerExtension.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(ref num, PassiveEffectType.HealthRegeneration);
            }
            return num.ResultNumber;
        }

        public override float GetSurgeryChance(PartyBase party) => _baseModel.GetSurgeryChance(party);

        public override int GetSkillXpFromHealingTroop(PartyBase party) => _baseModel.GetSkillXpFromHealingTroop(party);

        public override ExplainedNumber GetDailyHealingForRegulars(PartyBase party, bool isPrisoner, bool includeDescriptions = false)
        {
            var value = _baseModel.GetDailyHealingForRegulars(party, includeDescriptions);
            if (party == MobileParty.MainParty.Party) AddCareerPassivesForTroopRegeneration(MobileParty.MainParty, ref value);
            return value;

        }

        public override ExplainedNumber GetDailyHealingHpForHeroes(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
        {
            ExplainedNumber baseValue = _baseModel.GetDailyHealingHpForHeroes(party, includeDescriptions);
            if (party == MobileParty.MainParty.Party) AddCareerPassivesForHeroRegeneration(MobileParty.MainParty, ref baseValue);
            return baseValue;
        }

        public override int GetHeroesEffectedHealingAmount(Hero hero, float healingRate) =>_baseModel.GetHeroesEffectedHealingAmount(hero, healingRate);

        public override float GetSiegeBombardmentHitSurgeryChance(PartyBase party) => _baseModel.GetSiegeBombardmentHitSurgeryChance(party);

        public override ExplainedNumber GetBattleEndHealingAmount(PartyBase partyBase, Hero hero) => _baseModel.GetBattleEndHealingAmount(partyBase, hero);
    }
}