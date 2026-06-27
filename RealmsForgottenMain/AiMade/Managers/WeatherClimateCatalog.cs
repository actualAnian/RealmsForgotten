using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.Managers
{
    internal enum WeatherCommodityKind
    {
        Generic,
        FoodCrop,
        Fish,
        Livestock,
        Mineral,
        TextileRaw,
        Timber,
        LuxuryFood,
        Industrial
    }

    public static class WeatherClimateCatalog
    {
        private static readonly HashSet<string> WinterNordSettlementIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "town_nord4",
            "town_nord5",
            "town_nord6",
            "castle_nord5",
            "castle_nord6",
            "castle_nord7"
        };

        public static IEnumerable<ClimateRegionType> AllClimateRegions =>
            Enum.GetValues(typeof(ClimateRegionType)).Cast<ClimateRegionType>();

        public static string GetRegionKey(ClimateRegionType climateRegion) => climateRegion.ToString();

        public static ClimateRegionType GetClimateForCultureId(string cultureId)
        {
            string normalized = NormalizeCultureId(cultureId);

            if (normalized.Contains("giant"))
                return ClimateRegionType.TropicalRainforest;
            if (normalized.Contains("south_realm"))
                return ClimateRegionType.Savannah;
            if (normalized.Contains("north_realm"))
                return ClimateRegionType.SavannahTemperateTransition;
            if (normalized.Contains("west_realm"))
                return ClimateRegionType.DryTemperate;
            if (normalized.Contains("khuzait") || normalized.Contains("katogai") || normalized.Contains("khatogai"))
                return ClimateRegionType.FullSteppe;
            if (normalized.Contains("sturgia") || normalized.Contains("dwarf") || normalized.Contains("urkhai"))
                return ClimateRegionType.PermaWinter;
            if (normalized.Contains("elvean"))
                return ClimateRegionType.SacredBalancedMountain;
            if (normalized.Contains("tharnmar"))
                return ClimateRegionType.SwampTemperate;
            if (normalized.Contains("wulf"))
                return ClimateRegionType.CentralEuropeanTemperate;
            if (normalized.Contains("grimwatch"))
                return ClimateRegionType.MildAtlanticTemperate;
            if (normalized.Contains("nasorian"))
                return ClimateRegionType.HotTemperatePlains;
            if (normalized.Contains("aserai") || normalized.Contains("aqarun"))
                return ClimateRegionType.HarshDesert;
            if (normalized.Contains("nord"))
                return ClimateRegionType.SemiAridCoast;
            if (normalized.Contains("battania"))
                return ClimateRegionType.MildAtlanticTemperate;
            if (normalized.Contains("vlandia"))
                return ClimateRegionType.CentralEuropeanTemperate;
            if (normalized.Contains("empire"))
                return ClimateRegionType.CentralEuropeanTemperate;

            return ClimateRegionType.CentralEuropeanTemperate;
        }

        public static ClimateRegionType GetClimateForSettlement(Settlement settlement)
        {
            if (settlement == null)
                return ClimateRegionType.CentralEuropeanTemperate;

            if (!string.IsNullOrWhiteSpace(settlement.StringId) && WinterNordSettlementIds.Contains(settlement.StringId))
                return ClimateRegionType.WinterNord;

            return GetClimateForCultureId(settlement.Culture?.StringId ?? settlement.OwnerClan?.Culture?.StringId ?? string.Empty);
        }

        public static ClimateRegionType GetClimateForVillage(Village village)
        {
            if (village == null)
                return ClimateRegionType.CentralEuropeanTemperate;

            return GetClimateForSettlement(village.Settlement);
        }

        public static WeatherType RollWeather(ClimateRegionType climateRegion, Random rng)
        {
            int roll = rng.Next(100);

            return climateRegion switch
            {
                ClimateRegionType.TropicalRainforest => Roll(roll, (28, WeatherType.Rainy), (12, WeatherType.Flooding), (20, WeatherType.Abundance), (40, WeatherType.Clear)),
                ClimateRegionType.Savannah => Roll(roll, (18, WeatherType.Drought), (18, WeatherType.Abundance), (14, WeatherType.Rainy), (50, WeatherType.Clear)),
                ClimateRegionType.SavannahTemperateTransition => Roll(roll, (12, WeatherType.Drought), (18, WeatherType.Abundance), (18, WeatherType.Rainy), (52, WeatherType.Clear)),
                ClimateRegionType.DryTemperate => Roll(roll, (12, WeatherType.Drought), (15, WeatherType.Abundance), (18, WeatherType.Rainy), (55, WeatherType.Clear)),
                ClimateRegionType.FullSteppe => Roll(roll, (18, WeatherType.Drought), (15, WeatherType.Abundance), (10, WeatherType.Rainy), (57, WeatherType.Clear)),
                ClimateRegionType.PermaWinter => Roll(roll, (35, WeatherType.Blizzard), (8, WeatherType.Abundance), (57, WeatherType.Clear)),
                ClimateRegionType.SacredBalancedMountain => Roll(roll, (2, WeatherType.Blizzard), (1, WeatherType.Drought), (1, WeatherType.Flooding), (12, WeatherType.Rainy), (24, WeatherType.Abundance), (60, WeatherType.Clear)),
                ClimateRegionType.SwampTemperate => Roll(roll, (25, WeatherType.Rainy), (10, WeatherType.Flooding), (15, WeatherType.Abundance), (50, WeatherType.Clear)),
                ClimateRegionType.CentralEuropeanTemperate => Roll(roll, (2, WeatherType.Blizzard), (3, WeatherType.Drought), (2, WeatherType.Flooding), (22, WeatherType.Rainy), (16, WeatherType.Abundance), (55, WeatherType.Clear)),
                ClimateRegionType.MildAtlanticTemperate => Roll(roll, (3, WeatherType.Drought), (2, WeatherType.Flooding), (18, WeatherType.Rainy), (17, WeatherType.Abundance), (60, WeatherType.Clear)),
                ClimateRegionType.HotTemperatePlains => Roll(roll, (10, WeatherType.Drought), (20, WeatherType.Rainy), (18, WeatherType.Abundance), (52, WeatherType.Clear)),
                ClimateRegionType.HarshDesert => Roll(roll, (30, WeatherType.Drought), (8, WeatherType.Abundance), (2, WeatherType.Rainy), (60, WeatherType.Clear)),
                ClimateRegionType.SemiAridCoast => Roll(roll, (10, WeatherType.Drought), (18, WeatherType.Rainy), (17, WeatherType.Abundance), (55, WeatherType.Clear)),
                ClimateRegionType.WinterNord => Roll(roll, (32, WeatherType.Blizzard), (6, WeatherType.Rainy), (7, WeatherType.Abundance), (55, WeatherType.Clear)),
                _ => WeatherType.Clear
            };
        }

        public static float GetPartyMoraleDelta(WeatherType weather, ClimateRegionType climate)
        {
            return weather switch
            {
                WeatherType.Blizzard => IsWinterClimate(climate) ? -1f : -3f,
                WeatherType.Drought => IsDryClimate(climate) ? -0.75f : -2f,
                WeatherType.Rainy => IsWetClimate(climate) ? -0.5f : -1f,
                WeatherType.Flooding => -2f,
                WeatherType.Abundance => 1f,
                _ => 0f
            };
        }

        public static float GetTownProsperityDelta(Town town, WeatherType weather, ClimateRegionType climate)
        {
            if (town == null)
                return 0f;

            return weather switch
            {
                WeatherType.Blizzard => IsWinterClimate(climate) ? -0.2f : -0.4f,
                WeatherType.Drought => IsDryClimate(climate) ? -0.2f : -0.4f,
                WeatherType.Rainy => IsDryClimate(climate) ? 0.1f : 0f,
                WeatherType.Flooding => -0.8f,
                WeatherType.Abundance => 0.35f,
                _ => 0f
            };
        }

        public static float GetTownLoyaltyDelta(Town town, WeatherType weather, ClimateRegionType climate)
        {
            if (town == null || !town.Settlement.IsCastle)
                return 0f;

            return weather switch
            {
                WeatherType.Blizzard => -0.15f,
                WeatherType.Drought => -0.15f,
                WeatherType.Flooding => -0.1f,
                WeatherType.Abundance => 0.1f,
                _ => 0f
            };
        }

        public static float GetVillageHearthDelta(Village village, WeatherType weather, ClimateRegionType climate)
        {
            if (village == null)
                return 0f;

            float scale = 1f + village.GetHearthLevel() * 0.15f;
            float delta = weather switch
            {
                WeatherType.Blizzard => -8f,
                WeatherType.Drought => IsDryClimate(climate) ? -6f : -8f,
                WeatherType.Rainy => IsDryClimate(climate) ? 3f : 0f,
                WeatherType.Flooding => -10f,
                WeatherType.Abundance => 6f,
                _ => 0f
            };

            return delta * scale;
        }

        public static float GetVillageProductionMultiplier(Village village, ItemObject item, WeatherType weather, ClimateRegionType climate)
        {
            if (village == null || item == null)
                return 1f;

            WeatherCommodityKind kind = ClassifyCommodity(item);
            float multiplier = 1f;

            switch (weather)
            {
                case WeatherType.Blizzard:
                    multiplier *= kind switch
                    {
                        WeatherCommodityKind.FoodCrop => 0.65f,
                        WeatherCommodityKind.Fish => 0.75f,
                        WeatherCommodityKind.Livestock => 0.8f,
                        WeatherCommodityKind.Timber => 0.9f,
                        WeatherCommodityKind.Mineral => 0.9f,
                        _ => 0.9f
                    };
                    break;

                case WeatherType.Drought:
                    multiplier *= kind switch
                    {
                        WeatherCommodityKind.FoodCrop => 0.72f,
                        WeatherCommodityKind.LuxuryFood => 0.7f,
                        WeatherCommodityKind.Livestock => 0.85f,
                        WeatherCommodityKind.Fish => 0.95f,
                        WeatherCommodityKind.Mineral => 0.98f,
                        _ => 0.92f
                    };
                    break;

                case WeatherType.Rainy:
                    if (IsDryClimate(climate))
                    {
                        multiplier *= kind switch
                        {
                            WeatherCommodityKind.FoodCrop => 1.12f,
                            WeatherCommodityKind.Livestock => 1.05f,
                            WeatherCommodityKind.Fish => 1.08f,
                            _ => 1f
                        };
                    }
                    else if (kind == WeatherCommodityKind.Fish)
                    {
                        multiplier *= 1.05f;
                    }
                    break;

                case WeatherType.Flooding:
                    multiplier *= kind switch
                    {
                        WeatherCommodityKind.FoodCrop => 0.75f,
                        WeatherCommodityKind.Livestock => 0.82f,
                        WeatherCommodityKind.Mineral => 0.9f,
                        WeatherCommodityKind.TextileRaw => 0.85f,
                        WeatherCommodityKind.Fish => 1.1f,
                        _ => 0.9f
                    };
                    break;

                case WeatherType.Abundance:
                    multiplier *= kind switch
                    {
                        WeatherCommodityKind.FoodCrop => 1.22f,
                        WeatherCommodityKind.LuxuryFood => 1.15f,
                        WeatherCommodityKind.Livestock => 1.1f,
                        WeatherCommodityKind.Fish => 1.1f,
                        WeatherCommodityKind.Mineral => 1.03f,
                        _ => 1.05f
                    };
                    break;
            }

            multiplier *= climate switch
            {
                ClimateRegionType.HarshDesert when kind == WeatherCommodityKind.FoodCrop => 0.9f,
                ClimateRegionType.PermaWinter when kind == WeatherCommodityKind.FoodCrop => 0.85f,
                ClimateRegionType.WinterNord when kind == WeatherCommodityKind.FoodCrop => 0.9f,
                ClimateRegionType.SacredBalancedMountain => 1.05f,
                _ => 1f
            };

            return Clamp(multiplier, 0.35f, 1.5f);
        }

        public static float GetVillageFoodStockDelta(Village village, WeatherType weather, ClimateRegionType climate)
        {
            if (village == null || village.VillageState != Village.VillageStates.Normal)
                return 0f;

            WeatherCommodityKind kind = ClassifyCommodity(village.VillageType.PrimaryProduction);
            float scale = 0.6f + village.GetHearthLevel() * 0.25f;
            float delta = 0f;

            switch (weather)
            {
                case WeatherType.Blizzard:
                    delta = kind == WeatherCommodityKind.FoodCrop || kind == WeatherCommodityKind.Fish ? -1.8f : -0.8f;
                    break;
                case WeatherType.Drought:
                    delta = kind == WeatherCommodityKind.FoodCrop || kind == WeatherCommodityKind.LuxuryFood ? -2f : (kind == WeatherCommodityKind.Livestock ? -1.1f : -0.4f);
                    break;
                case WeatherType.Rainy:
                    if (IsDryClimate(climate) && (kind == WeatherCommodityKind.FoodCrop || kind == WeatherCommodityKind.Livestock))
                        delta = 0.9f;
                    break;
                case WeatherType.Flooding:
                    delta = kind == WeatherCommodityKind.Fish ? 0.4f : -2.2f;
                    break;
                case WeatherType.Abundance:
                    delta = kind == WeatherCommodityKind.FoodCrop || kind == WeatherCommodityKind.Fish || kind == WeatherCommodityKind.Livestock ? 1.5f : 0.4f;
                    break;
            }

            return delta * scale;
        }

        public static float GetTownWeatherFoodDelta(Town town, WeatherType weather, ClimateRegionType climate)
        {
            if (town == null)
                return 0f;

            return weather switch
            {
                WeatherType.Blizzard => IsWinterClimate(climate) ? -0.45f : -0.8f,
                WeatherType.Drought => IsDryClimate(climate) ? -0.35f : -0.6f,
                WeatherType.Rainy => IsWetClimate(climate) ? -0.1f : 0f,
                WeatherType.Flooding => -0.8f,
                WeatherType.Abundance => 0.45f,
                _ => 0f
            };
        }

        public static float GetWeatherPriceMultiplier(Settlement marketSettlement, ItemObject item)
        {
            if (marketSettlement == null || item == null)
                return 1f;

            ClimateRegionType climate = GetClimateForSettlement(marketSettlement);
            WeatherType weather = WeatherRegionManager.GetWeatherForSettlement(marketSettlement);
            WeatherCommodityKind kind = ClassifyCommodity(item);
            float factor = 1f;

            switch (weather)
            {
                case WeatherType.Blizzard:
                    factor = kind switch
                    {
                        WeatherCommodityKind.FoodCrop => 1.15f,
                        WeatherCommodityKind.Fish => 1.12f,
                        WeatherCommodityKind.Livestock => 1.1f,
                        WeatherCommodityKind.Timber => 1.08f,
                        _ => 1.03f
                    };
                    break;
                case WeatherType.Drought:
                    factor = kind switch
                    {
                        WeatherCommodityKind.FoodCrop => IsDryClimate(climate) ? 1.18f : 1.12f,
                        WeatherCommodityKind.LuxuryFood => 1.15f,
                        WeatherCommodityKind.Livestock => 1.08f,
                        _ => 1.02f
                    };
                    break;
                case WeatherType.Rainy:
                    factor = IsDryClimate(climate)
                        ? kind switch
                        {
                            WeatherCommodityKind.FoodCrop => 0.96f,
                            WeatherCommodityKind.Livestock => 0.98f,
                            _ => 1f
                        }
                        : kind == WeatherCommodityKind.FoodCrop ? 1.02f : 1f;
                    break;
                case WeatherType.Flooding:
                    factor = kind switch
                    {
                        WeatherCommodityKind.FoodCrop => 1.12f,
                        WeatherCommodityKind.Livestock => 1.08f,
                        WeatherCommodityKind.Mineral => 1.05f,
                        WeatherCommodityKind.Industrial => 1.08f,
                        WeatherCommodityKind.Fish => 0.97f,
                        _ => 1.04f
                    };
                    break;
                case WeatherType.Abundance:
                    factor = kind switch
                    {
                        WeatherCommodityKind.FoodCrop => 0.88f,
                        WeatherCommodityKind.LuxuryFood => 0.9f,
                        WeatherCommodityKind.Livestock => 0.94f,
                        WeatherCommodityKind.Fish => 0.92f,
                        _ => 0.97f
                    };
                    break;
            }

            if (climate == ClimateRegionType.SacredBalancedMountain)
            {
                factor = 1f + (factor - 1f) * 0.5f;
            }

            return Clamp(factor, 0.75f, 1.35f);
        }

        public static bool IsDryClimate(ClimateRegionType climate)
        {
            return climate == ClimateRegionType.Savannah
                || climate == ClimateRegionType.SavannahTemperateTransition
                || climate == ClimateRegionType.DryTemperate
                || climate == ClimateRegionType.FullSteppe
                || climate == ClimateRegionType.HarshDesert
                || climate == ClimateRegionType.SemiAridCoast;
        }

        public static bool IsWetClimate(ClimateRegionType climate)
        {
            return climate == ClimateRegionType.TropicalRainforest
                || climate == ClimateRegionType.SwampTemperate
                || climate == ClimateRegionType.CentralEuropeanTemperate
                || climate == ClimateRegionType.MildAtlanticTemperate
                || climate == ClimateRegionType.HotTemperatePlains;
        }

        public static bool IsWinterClimate(ClimateRegionType climate)
        {
            return climate == ClimateRegionType.PermaWinter || climate == ClimateRegionType.WinterNord;
        }

        private static WeatherCommodityKind ClassifyCommodity(ItemObject item)
        {
            if (item == null)
                return WeatherCommodityKind.Generic;

            ItemCategory category = item.ItemCategory;
            if (category == DefaultItemCategories.Fish)
                return WeatherCommodityKind.Fish;
            if (item.IsAnimal
                || category == DefaultItemCategories.Sheep
                || category == DefaultItemCategories.Cow
                || category == DefaultItemCategories.Horse
                || category == DefaultItemCategories.WarHorse
                || category == DefaultItemCategories.PackAnimal)
                return WeatherCommodityKind.Livestock;
            if (category == DefaultItemCategories.Iron
                || category == DefaultItemCategories.Clay
                || category == DefaultItemCategories.Silver)
                return WeatherCommodityKind.Mineral;
            if (category == DefaultItemCategories.Cotton)
                return WeatherCommodityKind.TextileRaw;
            if (category != null && category.StringId != null && category.StringId.IndexOf("wood", StringComparison.OrdinalIgnoreCase) >= 0)
                return WeatherCommodityKind.Timber;
            if (category == DefaultItemCategories.Tools || category == DefaultItemCategories.Pottery)
                return WeatherCommodityKind.Industrial;
            if (item.IsFood)
            {
                if (category == DefaultItemCategories.DateFruit || category == DefaultItemCategories.Olives)
                    return WeatherCommodityKind.LuxuryFood;

                return WeatherCommodityKind.FoodCrop;
            }

            return WeatherCommodityKind.Generic;
        }

        private static WeatherType Roll(int roll, params (int chance, WeatherType weather)[] weights)
        {
            int cumulative = 0;
            foreach (var (chance, weather) in weights)
            {
                cumulative += chance;
                if (roll < cumulative)
                    return weather;
            }

            return weights.Length > 0 ? weights[weights.Length - 1].weather : WeatherType.Clear;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static string NormalizeCultureId(string cultureId)
        {
            if (string.IsNullOrWhiteSpace(cultureId))
                return string.Empty;

            string normalized = cultureId.Trim().ToLowerInvariant();
            if (normalized.StartsWith("culture."))
                normalized = normalized.Substring("culture.".Length);
            return normalized;
        }
    }
}
