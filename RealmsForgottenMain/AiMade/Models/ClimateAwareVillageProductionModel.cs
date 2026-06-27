using RealmsForgotten.AiMade.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Models
{
    public class ClimateAwareVillageProductionModel : VillageProductionCalculatorModel
    {
        private readonly VillageProductionCalculatorModel _baseModel;
        private readonly TextObject _weatherProductionText = new TextObject("{=rf_weather_prod}Weather and climate");

        public ClimateAwareVillageProductionModel(VillageProductionCalculatorModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override ExplainedNumber CalculateDailyProductionAmount(Village village, ItemObject item)
        {
            ExplainedNumber result = _baseModel.CalculateDailyProductionAmount(village, item);
            if (village == null || item == null)
                return result;

            WeatherType weather = WeatherRegionManager.GetWeatherForVillage(village);
            ClimateRegionType climate = WeatherRegionManager.GetClimateForVillage(village);
            float multiplier = WeatherClimateCatalog.GetVillageProductionMultiplier(village, item, weather, climate);

            if (multiplier != 1f)
            {
                result.AddFactor(multiplier - 1f, _weatherProductionText);
            }

            return result;
        }

        public override float CalculateDailyFoodProductionAmount(Village village)
        {
            float result = _baseModel.CalculateDailyFoodProductionAmount(village);
            if (village == null)
                return result;

            WeatherType weather = WeatherRegionManager.GetWeatherForVillage(village);
            ClimateRegionType climate = WeatherRegionManager.GetClimateForVillage(village);
            float multiplier = WeatherClimateCatalog.GetVillageProductionMultiplier(village, village.VillageType.PrimaryProduction, weather, climate);
            return result * multiplier;
        }

        public override float CalculateProductionSpeedOfItemCategory(ItemCategory item)
        {
            return _baseModel.CalculateProductionSpeedOfItemCategory(item);
        }
    }
}
