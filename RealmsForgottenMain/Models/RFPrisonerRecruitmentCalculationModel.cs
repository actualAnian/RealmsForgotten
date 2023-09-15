using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Models
{
    internal class RFPrisonerRecruitmentCalculationModel : DefaultPrisonerRecruitmentCalculationModel
    {
        public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
        {
            int baseValue = base.CalculateRecruitableNumber(party, character);
            if (party.Owner?.Culture.StringId == "aqarun" && character.Occupation == Occupation.Bandit)
                return party.PrisonRoster.GetTroopCount(character);
            return baseValue;
        }
        public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
        {
            int baseNumber = base.GetPrisonerRecruitmentMoraleEffect(party, character, num);
            if (character.Occupation == Occupation.Bandit && character.Culture.StringId == "sea_raiders" &&
                party.Owner?.CharacterObject.Race == FaceGen.GetRaceOrDefault("undead"))
                return 0;
            if (character.Occupation == Occupation.Bandit && party.Owner?.Culture.StringId == "aqarun")
                return 0;
            return baseNumber;
        }
        public override bool ShouldPartyRecruitPrisoners(PartyBase party)
        {
            bool baseBool = base.ShouldPartyRecruitPrisoners(party);
            if (party.Owner?.Culture.StringId == "aqarun")
                return true;
            return baseBool;

        }
    }
}
