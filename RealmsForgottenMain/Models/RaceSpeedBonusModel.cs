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

namespace RealmsForgotten.Models
{
    public class RaceSpeedBonusModel : DefaultPartySpeedCalculatingModel
    {
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