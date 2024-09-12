using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.Quest.SecondUpdate;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using RealmsForgotten.Quest;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace RealmsForgotten.Models
{
    public class RFPartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel
    {
        private PartySpeedModel _previousModel;
        
        public RFPartySpeedCalculatingModel(PartySpeedModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override ExplainedNumber CalculateBaseSpeed(MobileParty party, bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0)
        {
            ExplainedNumber baseValue;
            try
            {
                baseValue = _previousModel.CalculateBaseSpeed(party, includeDescriptions, additionalTroopOnFootCount,
                    additionalTroopOnHorseCount);
            }
            catch (Exception e)
            {
                return party.MoraleExplained;
            }

            Hero partyOwner;
            try
            {
                partyOwner = party.Owner;
            }
            catch (Exception)
            {
                return baseValue;
            }

            if (partyOwner?.CharacterObject?.Race == FaceGen.GetRaceOrDefault("Xilantlacay"))
                baseValue.AddFactor(0.20f, new TextObject("Xilantlacay's Speedness"));

            if(QuestPatches.AvoidDisbanding && party?.Army?.Parties?.Contains(MobileParty.MainParty) == true)
                baseValue.AddFactor(2.0f);
            return baseValue;
        }
    }
}
