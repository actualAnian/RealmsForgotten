using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models
{
    public class RFRaceSpeedBonusModel : DefaultPartySpeedCalculatingModel
    {
        private PartySpeedModel _previousModel;
        
        public RFRaceSpeedBonusModel(PartySpeedModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override ExplainedNumber CalculateBaseSpeed(MobileParty party, bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0)
        {
            ExplainedNumber baseValue = base.CalculateBaseSpeed(party, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);

            Hero partyOwner = party.LeaderHero;

            if (partyOwner != null && partyOwner.Culture.StringId == "devils")
                baseValue.AddFactor(0.20f, new TextObject("{=culture_bonus}Culture Speed Bonus"));

            return baseValue;
        }
    }
}