using RealmsForgotten.AiMade.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten
{
    public class RFWeatherCampaignBehavior : CampaignBehaviorBase
    {
        private int _daysUntilNextRotation = 15;
        private const int RotationDays = 30;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnPartyDailyTick);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnSettlementTick);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_daysUntilNextRotation", ref _daysUntilNextRotation);
            dataStore.SyncData("WeatherRegions", ref WeatherRegionManager.Regions);
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            WeatherRegionManager.InitializeRegions();
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            WeatherRegionManager.EnsureInitialized();
        }

        private void OnDailyTick()
        {
            WeatherRegionManager.EnsureInitialized();
            _daysUntilNextRotation--;

            if (_daysUntilNextRotation > 0)
                return;

            WeatherRegionManager.RotateAllWeathers();
            _daysUntilNextRotation = RotationDays;
            ShowWeatherRotationReport();
        }

        private void OnPartyDailyTick(MobileParty party)
        {
            if (!party.IsActive || party.IsBandit || party.IsMilitia || party.LeaderHero == null)
                return;

            ClimateRegionType climate = WeatherClimateCatalog.GetClimateForCultureId(party.LeaderHero.Culture?.StringId ?? string.Empty);
            WeatherType weather = WeatherRegionManager.GetWeatherForCulture(party.LeaderHero.Culture?.StringId ?? string.Empty);
            float moraleDelta = WeatherClimateCatalog.GetPartyMoraleDelta(weather, climate);
            if (moraleDelta != 0f)
            {
                party.MoraleExplained.Add(moraleDelta, new TextObject(GetWeatherText(weather)));
            }
        }

        private void OnSettlementTick(Settlement settlement)
        {
            if (settlement?.Culture == null)
                return;

            if (MBRandom.RandomFloat >= 0.15f)
                return;

            ClimateRegionType climate = WeatherRegionManager.GetClimateForSettlement(settlement);
            WeatherType weather = WeatherRegionManager.GetWeatherForSettlement(settlement);

            if (settlement.Town != null)
            {
                float prosperityDelta = WeatherClimateCatalog.GetTownProsperityDelta(settlement.Town, weather, climate);
                float loyaltyDelta = WeatherClimateCatalog.GetTownLoyaltyDelta(settlement.Town, weather, climate);

                if (prosperityDelta != 0f)
                {
                    settlement.Town.Prosperity = Math.Max(0f, settlement.Town.Prosperity + prosperityDelta);
                }

                if (loyaltyDelta != 0f)
                {
                    settlement.Town.Loyalty += loyaltyDelta;
                }
            }

            if (settlement.Village != null)
            {
                float hearthDelta = WeatherClimateCatalog.GetVillageHearthDelta(settlement.Village, weather, climate);
                if (hearthDelta != 0f)
                {
                    settlement.Village.Hearth = Math.Max(0f, settlement.Village.Hearth + hearthDelta);
                }
            }
        }

        private void ShowWeatherRotationReport()
        {
            var weatherReports = new List<string>();

            foreach (var region in WeatherRegionManager.Regions.Values)
            {
                if (region.CurrentWeather == WeatherType.Clear)
                    continue;

                Settlement representativeSettlement = Settlement.All
                    .Where(s => s.IsTown || s.IsCastle)
                    .FirstOrDefault(s => WeatherClimateCatalog.GetClimateForSettlement(s) == region.ClimateRegion);

                string locationName = representativeSettlement != null
                    ? $"The lands around {representativeSettlement.Name}"
                    : region.ClimateRegion.ToString();

                weatherReports.Add($"- {locationName} are experiencing {region.CurrentWeather}.");
            }

            if (!weatherReports.Any())
                return;

            var sb = new StringBuilder();
            sb.AppendLine("Recent climate changes reported across the realm:\n");
            foreach (var report in weatherReports)
            {
                sb.AppendLine(report);
            }

            InformationManager.ShowInquiry(new InquiryData("Climate Events", sb.ToString(), true, false, "Understood", null, null, null));
        }

        private static string GetWeatherText(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Blizzard => "Snow storm",
                WeatherType.Drought => "Drought",
                WeatherType.Rainy => "Rainstorm",
                WeatherType.Flooding => "Flooded roads",
                WeatherType.Abundance => "Favorable weather",
                _ => "Weather"
            };
        }
    }
}
