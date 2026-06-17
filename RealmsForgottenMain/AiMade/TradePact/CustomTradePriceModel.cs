using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.ComponentInterfaces;


namespace RealmsForgotten.AiMade.TradePact
{
    public class CustomTradeItemPriceFactorModel : TradeItemPriceFactorModel
    {
        public override float GetTradePenalty(ItemObject item, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStore, float supply, float demand)
        {
            return 1.0f;
        }

        public override float GetBasePriceFactor(ItemCategory category, float inStore, float supply, float demand, bool isSelling, int transferValue)
        {
            return 1.0f;
        }

        public override int GetPrice(EquipmentElement itemRosterElement, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStore, float supply, float demand)
        {
            var basePrice = itemRosterElement.ItemValue;
            var multiplier = 1.0f;

            if (clientParty?.MapFaction != null && merchant?.MapFaction != null)
            {
                if (EconomicPactManager.TryGetPact(clientParty.MapFaction, merchant.MapFaction, out var pact)
                    && pact.Type == "General Goods")
                {
                    multiplier = 0.90f;
                }
            }

            return (int)(basePrice * multiplier);
        }

        public override int GetTheoreticalMaxItemMarketValue(ItemObject item)
        {
            return item.Value * 2;
        }
    }
}
