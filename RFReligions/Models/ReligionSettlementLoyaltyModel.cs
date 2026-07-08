using RealmsForgotten.RFReligions.Behavior;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.RFReligions.Models;

public class ReligionSettlementLoyaltyModel : DefaultSettlementLoyaltyModel
{
    // Delegates to whatever model was registered before it so future loyalty
    // models keep working; today the previous model is vanilla's default.
    private readonly SettlementLoyaltyModel _previousModel;

    public ReligionSettlementLoyaltyModel(SettlementLoyaltyModel previousModel)
    {
        _previousModel = previousModel;
    }

    public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
    {
        ExplainedNumber baseValue = _previousModel.CalculateLoyaltyChange(town, includeDescriptions);

        if (ReligionBehavior.Instance == null)
        {
            return baseValue;
        }

        float num = ReligionBehavior.Instance.SettlementGetLoyaltyEffect(town);
        if (num != 0f) baseValue.Add(num, GameTexts.FindText("RFRxjxR1z"), null);

        return baseValue;
    }
}
