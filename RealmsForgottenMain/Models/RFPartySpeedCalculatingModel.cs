using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models
{
    internal class RFPartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel
    {
        public override ExplainedNumber CalculateBaseSpeed(MobileParty party, bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0)
        {
            ExplainedNumber baseValue = base.CalculateBaseSpeed(party, includeDescriptions, additionalTroopOnFootCount,
                additionalTroopOnHorseCount);
            if (party.Owner?.CharacterObject.Race == FaceGen.GetRaceOrDefault("Xilantlacay"))
                baseValue.AddFactor(0.20f, new TextObject("Xilantlacay's Speedness"));
            return baseValue;

        }
    }
}
