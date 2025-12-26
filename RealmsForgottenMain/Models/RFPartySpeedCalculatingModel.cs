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
using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;

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


            if (partyOwner != null &&
                 (partyOwner.Culture.StringId == "devils" || partyOwner.Culture.StringId == "urkhai"))
            {
                // Add a 20% factor to the existing speed
                baseValue.AddFactor(0.20f, new TextObject("{=culture_bonus}Culture Speed Bonus"));
            }
            return baseValue;
        }
        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            ExplainedNumber value = base.CalculateFinalSpeed(mobileParty, finalSpeed);
            if (mobileParty == MobileParty.MainParty && mobileParty.LeaderHero != null && mobileParty.LeaderHero == Hero.MainHero)
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.PartyMovementSpeed);
            return value;
        }
    }
}
