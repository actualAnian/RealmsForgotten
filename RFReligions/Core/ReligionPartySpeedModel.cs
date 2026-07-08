using RealmsForgotten.RFReligions.Behavior;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFReligions.Core
{
    public class ReligionPartySpeedModel : DefaultPartySpeedCalculatingModel
    {
        // This model registers last, so it becomes the head of the model chain.
        // Both overrides must delegate to the model it replaced
        // (RFPartySpeedCalculatingModel -> vanilla / NavalDLC): the previous
        // version hid CalculateFinalSpeed with `new`, so the game never called
        // it and the blessing bonus never applied.
        private readonly PartySpeedModel _previousModel;

        public ReligionPartySpeedModel(PartySpeedModel previousModel)
        {
            _previousModel = previousModel;
        }

        public override ExplainedNumber CalculateBaseSpeed(MobileParty party, bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0)
        {
            return _previousModel.CalculateBaseSpeed(party, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);
        }

        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            finalSpeed = _previousModel.CalculateFinalSpeed(mobileParty, finalSpeed);

            if (mobileParty?.LeaderHero != null
                && ReligionBehavior.Instance != null
                && ReligionBehavior.Instance.IsHeroBlessed(mobileParty.LeaderHero, RFReligions.TengralorOrkhai))
            {
                finalSpeed.Add(0.3f, new TextObject("Tengralor Orkhai Blessing"));
            }

            return finalSpeed;
        }
    }
}
