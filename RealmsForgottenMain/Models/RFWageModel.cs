using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using RealmsForgotten.Career;
using RealmsForgotten.Career.Logic;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

namespace RealmsForgotten.Models
{
    internal class RFWageModel : DefaultPartyWageModel
    {
        private PartyWageModel _previousModel;
        
        public RFWageModel(PartyWageModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override int GetCharacterWage(CharacterObject character)
        {
            return base.GetCharacterWage(character) * (character.IsGiant() ? Globals.GiantsCostMult : 1);
        }
        public override int GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
        {
            int baseValue = base.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);
            if (buyerHero == null)
                return baseValue;
            int nasoriaBonus = (int)(baseValue - 15f / 100f * baseValue);
            if (buyerHero.Culture.StringId == "vlandia" && troop.Occupation == Occupation.Mercenary && nasoriaBonus > 0)
                return nasoriaBonus;
            return baseValue;
        }
        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, bool includeDescriptions = false)
        {
            ExplainedNumber value = base.GetTotalWage(mobileParty, includeDescriptions);
            if (mobileParty != MobileParty.MainParty) return value;
            var career = PlayerCareerExtension.GetCareer();
            if (career == null) return value;

            CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.TroopWages, true);
            if (PlayerCareerExtension.HasCareerChoice("KnightErrant1_5"))
            {
                var choice = career.AllChoices.First(c => c.StringId == "KnightErrant1_5");
                float totalReduction = 0;
                foreach (TaleWorlds.CampaignSystem.Roster.TroopRosterElement troop in mobileParty.MemberRoster.GetTroopRoster())
                    if (troop.Character.IsMounted)
                        totalReduction += troop.Number * troop.Character.TroopWage;// * choice.Passive!.EffectMagnitude;
                if (totalReduction > 0) value.Add(-1 * totalReduction, new("{=knight_cav_wage_reduction}Class knight cavalry wage reduction"));
            }

            if (PlayerCareerExtension.HasCareerChoice("WanderingBlade2_5"))
            {
                int totalReduction = 0;
                foreach (TaleWorlds.CampaignSystem.Roster.TroopRosterElement troop in mobileParty.MemberRoster.GetTroopRoster())
                {
                    if (troop.Character.Occupation == Occupation.Mercenary)
                        totalReduction += troop.Number * troop.Character.TroopWage / 3;
                }
                if (totalReduction > 0) value.Add(-1 * totalReduction, new("{=merc_merc_wage_reduction}Class mercenary wage reduction"));
            }
            return value;
        }
    }
}
