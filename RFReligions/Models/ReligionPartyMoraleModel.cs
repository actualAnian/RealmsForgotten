using RealmsForgotten.RFReligions.Behavior;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace RealmsForgotten.RFReligions.Models;

public class ReligionPartyMoraleModel : DefaultPartyMoraleModel
{
    public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
    {
        var baseValue = base.GetEffectivePartyMorale(mobileParty, includeDescription);

        var num = ReligionBehavior.Instance.PartyGetMoraleEffect(mobileParty);
        if (num != 0f) baseValue.Add(num, GameTexts.FindText("RFRxjxR1z"), null);

        return baseValue;
    }
}