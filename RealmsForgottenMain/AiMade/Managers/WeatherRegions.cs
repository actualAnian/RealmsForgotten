using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Managers
{
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
       
        private static Random _rng = new();

        public WeatherRegion(string cultureId)
        {
            CultureId = cultureId;
            RotateWeather();
        }

        public void RotateWeather()
        {
            var weatherRoll = _rng.Next(100);

            // Culturas com chance de enchente
            if (CultureId == "tharnmar" || CultureId == "giant")
            {
                if (weatherRoll < 50) CurrentWeather = WeatherType.Rainy;
                else if (weatherRoll < 80) CurrentWeather = WeatherType.Flooding;
                else if (weatherRoll < 95) CurrentWeather = WeatherType.Abundance;
                else CurrentWeather = WeatherType.Clear;
            }
            // Culturas frias
            else if (CultureId == "sturgia" || CultureId == "dwarf" || CultureId == "urkhai")
            {
                if (weatherRoll < 70) CurrentWeather = WeatherType.Blizzard;
                else if (weatherRoll < 90) CurrentWeather = WeatherType.Abundance;
                else CurrentWeather = WeatherType.Clear;
            }
            // Culturas desérticas
            else if (CultureId == "aserai" || CultureId == "aqarun")
            {
                if (weatherRoll < 70) CurrentWeather = WeatherType.Drought;
                else if (weatherRoll < 90) CurrentWeather = WeatherType.Abundance;
                else CurrentWeather = WeatherType.Clear;
            }
            // Culturas de floresta e chuva
            else if (CultureId == "battania" || CultureId == "vlandia" || CultureId == "grimwatch")
            {
                if (weatherRoll < 60) CurrentWeather = WeatherType.Rainy;
                else if (weatherRoll < 85) CurrentWeather = WeatherType.Abundance;
                else CurrentWeather = WeatherType.Clear;
            }
            // Outras (misc.)
            else
            {
                if (weatherRoll < 40) CurrentWeather = WeatherType.Rainy;
                else if (weatherRoll < 70) CurrentWeather = WeatherType.Abundance;
                else CurrentWeather = WeatherType.Clear;
            }
        }
    }
}