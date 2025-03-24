using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Models
{
    public class UrkhaiPartySizeModel : DefaultPartySizeLimitModel
    {
        public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
        {
            ExplainedNumber baseResult = base.GetPartyMemberSizeLimit(party, includeDescriptions);

            if (party.MobileParty != null && party.MobileParty.LeaderHero != null)
            {
                CultureObject culture = party.MobileParty.LeaderHero.Culture;

                if (culture.StringId == "urkhai")
                {
                    int minPartySize = 70; // Default for Urkhai lords

                    // If they are a kingdom leader, set a higher minimum
                    if (party.MobileParty.LeaderHero.Clan?.Kingdom?.Leader == party.MobileParty.LeaderHero)
                    {
                        minPartySize = 100;
                    }

                    // Ensure the minimum party size is met
                    if (baseResult.ResultNumber < minPartySize)
                    {
                        baseResult.Add(minPartySize - baseResult.ResultNumber, new TextObject("{=CustomMod}Urkhai Minimum Party Size"));
                    }

                    // Extend the max party size by 15% to ensure they can support more troops
                    baseResult.AddFactor(0.15f, new TextObject("{=CustomMod}Urkhai Warband Expansion"));
                }
            }

            return baseResult;
        }
    }
}
