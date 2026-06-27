using RealmsForgotten.AiMade.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Models
{
    public class ClimateAwareSettlementFoodModel : SettlementFoodModel
    {
        private readonly SettlementFoodModel _baseModel;
        private readonly TextObject _regionalWeatherText = new TextObject("{=rf_weather_food}Regional weather");

        public ClimateAwareSettlementFoodModel(SettlementFoodModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override int FoodStocksUpperLimit => _baseModel.FoodStocksUpperLimit;

        public override int NumberOfProsperityToEatOneFood => _baseModel.NumberOfProsperityToEatOneFood;

        public override int NumberOfMenOnGarrisonToEatOneFood => _baseModel.NumberOfMenOnGarrisonToEatOneFood;

        public override int CastleFoodStockUpperLimitBonus => _baseModel.CastleFoodStockUpperLimitBonus;

        public override ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false)
        {
            ExplainedNumber result = _baseModel.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);
            if (town?.Settlement == null)
                return result;

            Settlement settlement = town.Settlement;
            WeatherType townWeather = WeatherRegionManager.GetWeatherForSettlement(settlement);
            ClimateRegionType townClimate = WeatherRegionManager.GetClimateForSettlement(settlement);
            float townDelta = WeatherClimateCatalog.GetTownWeatherFoodDelta(town, townWeather, townClimate);
            if (townDelta != 0f)
            {
                result.Add(townDelta, _regionalWeatherText);
            }

            foreach (Village boundVillage in settlement.BoundVillages)
            {
                WeatherType villageWeather = WeatherRegionManager.GetWeatherForVillage(boundVillage);
                ClimateRegionType villageClimate = WeatherRegionManager.GetClimateForVillage(boundVillage);
                float villageDelta = WeatherClimateCatalog.GetVillageFoodStockDelta(boundVillage, villageWeather, villageClimate);
                if (villageDelta != 0f)
                {
                    result.Add(villageDelta, includeDescriptions ? boundVillage.Name : null);
                }
            }

            return result;
        }
    }
}
