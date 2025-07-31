using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Managers
{
    public static class WeatherRegionManager
    {
        public static Dictionary<string, WeatherRegion> Regions = new();

        public static void InitializeRegions()
        {
            string[] cultures = new string[]
            {
                "aserai", "battania", "empire", "khuzait", "sturgia", "vlandia",
                "aqarun", "dwarf", "urkhai", "grimwatch", "Katogai", "tharnmar",
                "south_realm", "west_realm", "giant", "wulf"
            };

            foreach (var culture in cultures)
            {
                if (!Regions.ContainsKey(culture))
                {
                    Regions[culture] = new WeatherRegion(culture);
                }
            }
        }

        public static void RotateAllWeathers()
        {
            foreach (var region in Regions.Values)
                region.RotateWeather();
        }

        public static WeatherType GetWeatherForCulture(string cultureId)
        {
            // This logic can be improved to handle sub-cultures like empire_w, empire_s etc.
            if (cultureId.Contains("empire")) cultureId = "empire";

            if (Regions.TryGetValue(cultureId, out var region))
                return region.CurrentWeather;
            return WeatherType.Clear;
        }
    }
}
