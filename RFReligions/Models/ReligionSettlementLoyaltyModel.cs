using RealmsForgotten.RFReligions.Behavior;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.RFReligions.Models;

public class ReligionSettlementLoyaltyModel : DefaultSettlementLoyaltyModel
{
    public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
    {
        var baseValue = base.CalculateLoyaltyChange(town, includeDescriptions);

        var num = ReligionBehavior.Instance.SettlementGetLoyaltyEffect(town);
        if (num != 0f) baseValue.Add(num, GameTexts.FindText("RFRxjxR1z"), null);

        return baseValue;
    }
}