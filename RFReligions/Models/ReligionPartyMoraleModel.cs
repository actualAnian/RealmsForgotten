using RealmsForgotten.RFReligions.Behavior;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace RealmsForgotten.RFReligions.Models;

public class ReligionPartyMoraleModel : DefaultPartyMoraleModel
{
    // This model registers last, so it becomes the head of the model chain.
    // It must delegate to the model it replaced (RFPartyMoraleModel -> vanilla);
    // calling base. instead skipped every other model's morale effects.
    private readonly PartyMoraleModel _previousModel;

    public ReligionPartyMoraleModel(PartyMoraleModel previousModel)
    {
        _previousModel = previousModel;
    }

    public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
    {
        ExplainedNumber baseValue = _previousModel.GetEffectivePartyMorale(mobileParty, includeDescription);

        if (mobileParty?.LeaderHero == null || ReligionBehavior.Instance == null)
        {
            return baseValue;
        }

        float num = ReligionBehavior.Instance.PartyGetMoraleEffect(mobileParty);
        if (num != 0f) baseValue.Add(num, GameTexts.FindText("RFRxjxR1z"), null);

        return baseValue;
    }
}
