using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.AiMade.Managers
{
    public static class WeatherRegionManager
    {
        public static Dictionary<string, WeatherRegion> Regions = new();

        public static void InitializeRegions()
        {
            var existingRegions = Regions ?? new Dictionary<string, WeatherRegion>();
            var rebuiltRegions = new Dictionary<string, WeatherRegion>();

            foreach (ClimateRegionType climateRegion in WeatherClimateCatalog.AllClimateRegions)
            {
                string key = WeatherClimateCatalog.GetRegionKey(climateRegion);
                WeatherRegion region;

                if (existingRegions.TryGetValue(key, out var currentRegion))
                {
                    currentRegion.SetClimateRegion(climateRegion);
                    region = currentRegion;
                }
                else
                {
                    region = new WeatherRegion(key, climateRegion);
                    if (TryGetLegacyWeatherForClimate(existingRegions, climateRegion, out var legacyWeather))
                    {
                        region.SetWeather(legacyWeather);
                    }
                }

                rebuiltRegions[key] = region;
            }

            Regions = rebuiltRegions;
        }

        public static void EnsureInitialized()
        {
            if (Regions == null || Regions.Count == 0 || !HasExpectedRegionSet())
            {
                InitializeRegions();
            }
        }

        public static void RotateAllWeathers()
        {
            EnsureInitialized();
            foreach (var region in Regions.Values)
            {
                region.RotateWeather();
            }
        }

        public static WeatherType GetWeatherForCulture(string cultureId)
        {
            EnsureInitialized();
            ClimateRegionType climateRegion = WeatherClimateCatalog.GetClimateForCultureId(cultureId);
            string regionKey = WeatherClimateCatalog.GetRegionKey(climateRegion);
            return Regions.TryGetValue(regionKey, out var region) ? region.CurrentWeather : WeatherType.Clear;
        }

        public static WeatherType GetWeatherForSettlement(Settlement settlement)
        {
            EnsureInitialized();
            ClimateRegionType climateRegion = WeatherClimateCatalog.GetClimateForSettlement(settlement);
            string regionKey = WeatherClimateCatalog.GetRegionKey(climateRegion);
            return Regions.TryGetValue(regionKey, out var region) ? region.CurrentWeather : WeatherType.Clear;
        }

        public static WeatherType GetWeatherForVillage(Village village)
        {
            return GetWeatherForSettlement(village?.Settlement);
        }

        public static ClimateRegionType GetClimateForSettlement(Settlement settlement)
        {
            return WeatherClimateCatalog.GetClimateForSettlement(settlement);
        }

        public static ClimateRegionType GetClimateForVillage(Village village)
        {
            return WeatherClimateCatalog.GetClimateForVillage(village);
        }

        private static bool HasExpectedRegionSet()
        {
            foreach (ClimateRegionType climateRegion in WeatherClimateCatalog.AllClimateRegions)
            {
                if (!Regions.ContainsKey(WeatherClimateCatalog.GetRegionKey(climateRegion)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetLegacyWeatherForClimate(Dictionary<string, WeatherRegion> legacyRegions, ClimateRegionType climateRegion, out WeatherType weather)
        {
            foreach (var legacyRegion in legacyRegions.Values)
            {
                ClimateRegionType legacyClimate = WeatherClimateCatalog.GetClimateForCultureId(legacyRegion.CultureId);
                if (legacyClimate == climateRegion)
                {
                    weather = legacyRegion.CurrentWeather;
                    return true;
                }
            }

            weather = WeatherType.Clear;
            return false;
        }
    }
}
