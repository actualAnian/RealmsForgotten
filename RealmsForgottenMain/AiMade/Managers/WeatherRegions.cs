using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Managers
{
    public enum ClimateRegionType
    {
        TropicalRainforest,
        Savannah,
        SavannahTemperateTransition,
        DryTemperate,
        FullSteppe,
        PermaWinter,
        SacredBalancedMountain,
        SwampTemperate,
        CentralEuropeanTemperate,
        MildAtlanticTemperate,
        HotTemperatePlains,
        HarshDesert,
        SemiAridCoast,
        WinterNord
    }

    public enum WeatherType
    {
        Clear,
        Blizzard,
        Drought,
        Rainy,
        Flooding,
        Abundance
    }

    [SaveableClass]
    public class WeatherRegion
    {
        [SaveableProperty(1)]
        public string CultureId { get; private set; }

        [SaveableProperty(2)]
        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;

        [SaveableProperty(3)]
        public ClimateRegionType ClimateRegion { get; private set; } = ClimateRegionType.CentralEuropeanTemperate;

        private static Random _rng = new();

        public WeatherRegion(string cultureId, ClimateRegionType climateRegion)
        {
            CultureId = cultureId;
            ClimateRegion = climateRegion;
            RotateWeather();
        }

        public void SetClimateRegion(ClimateRegionType climateRegion)
        {
            ClimateRegion = climateRegion;
        }

        public void SetWeather(WeatherType weather)
        {
            CurrentWeather = weather;
        }

        public void RotateWeather()
        {
            CurrentWeather = WeatherClimateCatalog.RollWeather(ClimateRegion, _rng);
        }
    }
}
