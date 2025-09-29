using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using RealmsForgotten.Career;
using TaleWorlds.Library;

namespace RealmsForgotten.Models
{
    internal class RFPrisonerRecruitmentCalculationModel : DefaultPrisonerRecruitmentCalculationModel
    {
        private PrisonerRecruitmentCalculationModel _previousModel;
        public static bool DebugMode = false;
        public RFPrisonerRecruitmentCalculationModel(PrisonerRecruitmentCalculationModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
        {
            int baseValue = _previousModel.CalculateRecruitableNumber(party, character);
            if (party.Owner?.Culture.StringId == "aqarun" && character.Occupation == Occupation.Bandit)
                return party.PrisonRoster.GetTroopCount(character);
            return baseValue;
        }
        public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
        {
            if (DebugMode)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage("[DEBUG] CareerID = " + PlayerCareerExtension.PlayerCareerInfo?.CareerID)
                );
            }

            int baseNumber = _previousModel.GetPrisonerRecruitmentMoraleEffect(party, character, num);

            if (character.Occupation == Occupation.Bandit && character.Culture.StringId == "sea_raiders" &&
                party.Owner?.CharacterObject.Race == FaceGen.GetRaceOrDefault("undead"))
                return 0;

            if (character.Occupation == Occupation.Bandit && party.Owner?.Culture.StringId == "aqarun")
                return 0;

            if (character.Occupation == Occupation.Bandit
                && party == PartyBase.MainParty
                && PlayerCareerExtension.PlayerCareerInfo != null
                && PlayerCareerExtension.PlayerCareerInfo.CareerID == "mercenary")
            {
                if (DebugMode)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage("[DEBUG] Mercenary detected → morale penalty removed")
                    );
                }
                return 0;
            }

            return baseNumber;
        }

        public override bool ShouldPartyRecruitPrisoners(PartyBase party)
        {
            bool baseBool = _previousModel.ShouldPartyRecruitPrisoners(party);
            if (party.Owner?.Culture.StringId == "aqarun")
                return true;
            return baseBool;

        }
    }
}
