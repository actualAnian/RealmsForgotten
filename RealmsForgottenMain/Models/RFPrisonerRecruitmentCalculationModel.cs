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
        private const string MercenaryBanditRenownPerkId = "MercenaryLord2_3";
        private const string ClericBanditRecruitmentPerkId = "ClericBearerOfMercy1_3";
        private const string ClericPrisonerMoralePerkId = "ClericBearerOfMercy1_4";
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

            if (party == PartyBase.MainParty
                && character.Occupation == Occupation.Bandit
                && PlayerCareerExtension.HasCareerChoice(MercenaryBanditRenownPerkId))
            {
                int troopCount = party.PrisonRoster.GetTroopCount(character);
                if (troopCount <= 0)
                    return baseValue;

                float renown = Clan.PlayerClan?.Renown ?? 0f;
                float recruitFactor = MathF.Clamp(0.25f + (renown / 800f), 0.25f, 1f);
                int renownRecruitable = (int)MathF.Clamp((int)MathF.Ceiling(troopCount * recruitFactor), 1, troopCount);
                return MathF.Max(baseValue, renownRecruitable);
            }

            if (party == PartyBase.MainParty
                && character.Occupation == Occupation.Bandit
                && PlayerCareerExtension.HasCareerChoice(ClericBanditRecruitmentPerkId))
            {
                int troopCount = party.PrisonRoster.GetTroopCount(character);
                if (troopCount <= 0)
                    return baseValue;

                int redeemable = (int)MathF.Clamp((int)MathF.Ceiling(troopCount * 0.35f), 1, troopCount);
                return MathF.Max(baseValue, redeemable);
            }

            return baseValue;
        }
        public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
        {
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
                return 0;
            }

            if (party == PartyBase.MainParty
                && PlayerCareerExtension.HasCareerChoice(ClericPrisonerMoralePerkId))
            {
                return (int)MathF.Ceiling(baseNumber * 0.5f);
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
