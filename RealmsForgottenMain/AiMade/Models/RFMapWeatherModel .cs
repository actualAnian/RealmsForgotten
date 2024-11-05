using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TaleWorlds.CampaignSystem.ComponentInterfaces.MapWeatherModel;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Models
{
    public class RFMapWeatherModel : DefaultMapWeatherModel
    {
        private readonly WeatherEvent[] _adodWeatherDataCache = new WeatherEvent[1024];
        private readonly float _harshWeatherTreshhold = 0.95f;
        private readonly float _mildWeatherTreshhold = 0.85f;

        // Define the winter region using the coordinates you provided
        private readonly Vec2[] _winterRegionCoordinates = new Vec2[]
        {
        new Vec2(938.89f, 910.84f),  // Point 1
        new Vec2(1390.89f, 1080.86f), // Point 2
        new Vec2(1527.73f, 1604.16f), // Point 3
        new Vec2(85.27f, 1597.24f),   // Point 4
        new Vec2(587.75f, 1133.55f)   // Point 5
        };

        private readonly Vec2[] _dorneCoordinates = new Vec2[]
        {
        new Vec2(464, 881),
        new Vec2(758, 878),
        new Vec2(786, 759),
        new Vec2(385, 720)
        };

        private readonly Vec2[] _valeCoordinates = new Vec2[]
        {
        new Vec2(980, 542),
        new Vec2(1525, 724),
        new Vec2(986, 68),
        new Vec2(1585, 71)
        };

        private bool _isGlobalWinter;
        private CampaignTime _globalWinterStartTime;
        private CampaignTime _nextGlobalWinterTime;

        public RFMapWeatherModel()
        {
            _isGlobalWinter = false;
            _globalWinterStartTime = CampaignTime.Zero;
            _nextGlobalWinterTime = CampaignTime.YearsFromNow(6);
        }

        public override AtmosphereInfo GetAtmosphereModel(Vec3 pos)
        {
            var atmo = base.GetAtmosphereModel(pos);
            ValueTuple<CampaignTime.Seasons, bool, float, float> data = GetSeasonRainAndSnowDataForOpeningMission(pos.AsVec2);
            atmo.InterpolatedAtmosphereName = GetSelectedAtmosphereId(data.Item1, data.Item2, data.Item3, data.Item4);
            atmo.TimeInfo.Season = (int)data.Item1;
            return atmo;
        }

        public override WeatherEvent UpdateWeatherForPosition(Vec2 position, CampaignTime ct)
        {
            CheckGlobalWinter(ct);

            if (IsInRegion(position, _dorneCoordinates))
            {
                return WeatherEvent.Clear;
            }

            if (IsInRegion(position, _valeCoordinates))
            {
                return DetermineWeatherForVale(position);
            }

            if (_isGlobalWinter)
            {
                return DetermineWeather(position, CampaignTime.Seasons.Winter);
            }
            else if (IsNorthOfWinterThreshold(position))
            {
                return DetermineWeather(position, CampaignTime.Seasons.Winter);
            }
            else
            {
                CampaignTime.Seasons currentSeason = CampaignTime.Now.GetSeasonOfYear;
                if (currentSeason == CampaignTime.Seasons.Winter)
                {
                    currentSeason = CampaignTime.Seasons.Summer;
                }
                return DetermineWeather(position, currentSeason);
            }
        }

        private void CheckGlobalWinter(CampaignTime currentTime)
        {
            if (_isGlobalWinter && (currentTime.ElapsedYearsUntilNow - _globalWinterStartTime.ElapsedYearsUntilNow) >= 2)
            {
                _isGlobalWinter = false;
                _nextGlobalWinterTime = CampaignTime.YearsFromNow(4);
            }
            else if (!_isGlobalWinter && currentTime >= _nextGlobalWinterTime && currentTime.GetSeasonOfYear == CampaignTime.Seasons.Spring)
            {
                _isGlobalWinter = true;
                _globalWinterStartTime = currentTime;
            }
        }

        public void StartGlobalWinter()
        {
            _isGlobalWinter = true;
            _globalWinterStartTime = CampaignTime.Now;
        }

        public void EndGlobalWinter()
        {
            _isGlobalWinter = false;
            _nextGlobalWinterTime = CampaignTime.YearsFromNow(6);
        }

        public void SetWeatherAtPosition(Vec2 position, WeatherEvent weatherType)
        {
            int xIndex, yIndex;
            GetNodePositionForWeather(position, out xIndex, out yIndex);
            _adodWeatherDataCache[yIndex * DefaultWeatherNodeDimension + xIndex] = weatherType;
        }

        // Method to check if a position is inside the winter region
        private bool IsNorthOfWinterThreshold(Vec2 position)
        {
            return IsInRegion(position, _winterRegionCoordinates);
        }

        // General method to check if a position is in a region defined by coordinates (polygonal region)
        private bool IsInRegion(Vec2 position, Vec2[] regionCoordinates)
        {
            int i, j = regionCoordinates.Length - 1;
            bool oddNodes = false;
            for (i = 0; i < regionCoordinates.Length; i++)
            {
                if (regionCoordinates[i].Y < position.Y && regionCoordinates[j].Y >= position.Y ||
                    regionCoordinates[j].Y < position.Y && regionCoordinates[i].Y >= position.Y)
                {
                    if (regionCoordinates[i].X + (position.Y - regionCoordinates[i].Y) /
                        (regionCoordinates[j].Y - regionCoordinates[i].Y) *
                        (regionCoordinates[j].X - regionCoordinates[i].X) < position.X)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }
            return oddNodes;
        }

        private WeatherEvent DetermineWeatherForVale(Vec2 position)
        {
            WeatherEvent weather = WeatherEvent.Clear;
            var rng = MBRandom.RandomFloatRanged(0f, 1f);
            if (rng > _harshWeatherTreshhold)
            {
                weather = SetWeatherForPosition(position, WeatherEvent.Blizzard);
            }
            else if (rng > _mildWeatherTreshhold)
            {
                weather = SetWeatherForPosition(position, WeatherEvent.HeavyRain);
            }
            else
            {
                weather = SetWeatherForPosition(position, WeatherEvent.Snowy);
            }
            return weather;
        }

        private WeatherEvent DetermineWeather(Vec2 position, CampaignTime.Seasons season)
        {
            WeatherEvent weather = WeatherEvent.Clear;
            var rng = MBRandom.RandomFloatRanged(0f, 1f);
            if (rng > _harshWeatherTreshhold)
            {
                if (season == CampaignTime.Seasons.Winter)
                {
                    weather = SetWeatherForPosition(position, WeatherEvent.Blizzard);
                }
                else
                {
                    weather = SetWeatherForPosition(position, WeatherEvent.HeavyRain);
                }
            }
            else if (rng > _mildWeatherTreshhold)
            {
                if (season == CampaignTime.Seasons.Winter)
                {
                    weather = SetWeatherForPosition(position, WeatherEvent.Snowy);
                }
                else
                {
                    weather = SetWeatherForPosition(position, WeatherEvent.LightRain);
                }
            }
            else
            {
                weather = SetWeatherForPosition(position, WeatherEvent.Clear);
            }
            return weather;
        }

        private WeatherEvent SetWeatherForPosition(in Vec2 position, WeatherEvent weather)
        {
            int xIndex;
            int yIndex;
            GetNodePositionForWeather(position, out xIndex, out yIndex);
            _adodWeatherDataCache[yIndex * DefaultWeatherNodeDimension + xIndex] = weather;
            return _adodWeatherDataCache[yIndex * DefaultWeatherNodeDimension + xIndex];
        }

        public override WeatherEvent GetWeatherEventInPosition(Vec2 pos)
        {
            int xIndex;
            int yIndex;
            GetNodePositionForWeather(pos, out xIndex, out yIndex);
            return _adodWeatherDataCache[yIndex * DefaultWeatherNodeDimension + xIndex];
        }

        private string GetSelectedAtmosphereId(CampaignTime.Seasons selectedSeason, bool isRaining, float rainValue, float snowValue)
        {
            string result = "semicloudy_field_battle";
            if (Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsFortification || Settlement.CurrentSettlement.IsVillage))
            {
                result = "semicloudy_empire";
            }
            if (selectedSeason == CampaignTime.Seasons.Winter)
            {
                if (snowValue >= 0.85f)
                {
                    result = "dense_snowy";
                }
                else
                {
                    result = "semi_snowy";
                }
            }
            else
            {
                if (rainValue > 0.6f)
                {
                    result = "wet";
                }
                if (isRaining)
                {
                    if (rainValue >= 0.85f)
                    {
                        result = "dense_rainy";
                    }
                    else
                    {
                        result = "semi_rainy";
                    }
                }
            }
            return result;
        }
        private ValueTuple<CampaignTime.Seasons, bool, float, float> GetSeasonRainAndSnowDataForOpeningMission(Vec2 position)
        {
            CampaignTime.Seasons seasons = IsNorthOfWinterThreshold(position) ? CampaignTime.Seasons.Winter : CampaignTime.Now.GetSeasonOfYear;
            if (!IsNorthOfWinterThreshold(position) && seasons == CampaignTime.Seasons.Winter)
            {
                seasons = CampaignTime.Seasons.Summer;
            }
            WeatherEvent weatherEventInPosition = GetWeatherEventFromSurroundingNodes(position);
            float rainDensity = 0f;
            float snowDensity = 0.85f;
            bool isRaining = false;
            switch (weatherEventInPosition)
            {
                case WeatherEvent.LightRain:
                    rainDensity = 0.7f;
                    break;
                case WeatherEvent.HeavyRain:
                    isRaining = true;
                    rainDensity = 0.85f + MBRandom.RandomFloatRanged(0f, 0.14999998f);
                    break;
                case WeatherEvent.Snowy:
                    seasons = CampaignTime.Seasons.Winter;
                    rainDensity = 0.55f;
                    snowDensity = 0.55f + MBRandom.RandomFloatRanged(0f, 0.3f);
                    break;
                case WeatherEvent.Blizzard:
                    seasons = CampaignTime.Seasons.Winter;
                    rainDensity = 0.85f;
                    snowDensity = 0.85f;
                    break;
            }
            return new ValueTuple<CampaignTime.Seasons, bool, float, float>(seasons, isRaining, rainDensity, snowDensity);
        }

        private Vec2 GetNodePositionForWeather(Vec2 pos, out int xIndex, out int yIndex)
        {
            if (Campaign.Current.MapSceneWrapper != null)
            {
                Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
                float xSize = terrainSize.X / DefaultWeatherNodeDimension;
                float ySize = terrainSize.Y / DefaultWeatherNodeDimension;
                xIndex = (int)(pos.x / xSize);
                yIndex = (int)(pos.y / ySize);
                float a = xIndex * xSize;
                float b = yIndex * ySize;
                return new Vec2(a, b);
            }
            xIndex = 0;
            yIndex = 0;
            return Vec2.Zero;
        }

        private WeatherEvent GetWeatherEventFromSurroundingNodes(Vec2 position)
        {
            WeatherEvent weather = WeatherEvent.Clear;
            int xIndex;
            int yIndex;
            GetNodePositionForWeather(position, out xIndex, out yIndex);

            int xMin = Math.Max(0, xIndex - 1);
            int xMax = Math.Min(xIndex + 1, DefaultWeatherNodeDimension);
            int yMin = Math.Max(0, yIndex - 1);
            int yMax = Math.Min(yIndex + 1, DefaultWeatherNodeDimension);

            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    weather = _adodWeatherDataCache[y * DefaultWeatherNodeDimension + x];
                    if (weather != WeatherEvent.Clear) return weather;
                }
            }
            return weather;
        }
    }
}