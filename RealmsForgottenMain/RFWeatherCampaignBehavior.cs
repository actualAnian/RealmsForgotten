using RealmsForgotten.AiMade.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace RealmsForgotten
{
    public class RFWeatherCampaignBehavior : CampaignBehaviorBase
    {
        private int _daysUntilNextRotation = 10;

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
            // We also need to save the weather data itself
            dataStore.SyncData("WeatherRegions", ref WeatherRegionManager.Regions);
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            // FIX: This line is now active. It will run once at the start of a new game.
            WeatherRegionManager.InitializeRegions();
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
          
            if (WeatherRegionManager.Regions == null || !WeatherRegionManager.Regions.Any())
            {
               
                WeatherRegionManager.InitializeRegions();
            }
        }

        private void OnDailyTick()
        {
            // Initialize regions if loading a save that doesn't have them yet.
            if (WeatherRegionManager.Regions == null || !WeatherRegionManager.Regions.Any())
            {
                WeatherRegionManager.InitializeRegions();
            }

            _daysUntilNextRotation--;

            if (_daysUntilNextRotation <= 0)
            {
                // FIX: This line is now active. It will change the weather every 20 days.
                WeatherRegionManager.RotateAllWeathers();
                _daysUntilNextRotation = 20;

                var weatherReports = new List<string>();

                foreach (var region in WeatherRegionManager.Regions.Values)
                {
                    if (region.CurrentWeather != WeatherType.Clear)
                    {
                        var weatherName = region.CurrentWeather.ToString();

                        var potentialTowns = Settlement.All
                            .Where(s => s.IsTown && s.Culture?.StringId == region.CultureId)
                            .ToList();

                        Settlement representativeTown = null;
                        if (potentialTowns.Any())
                        {
                            representativeTown = potentialTowns[MBRandom.RandomInt(potentialTowns.Count)];
                        }

                        string locationName = representativeTown != null ? $"The lands around {representativeTown.Name}" : $"The {region.CultureId} region";

                        weatherReports.Add($"• {locationName} are experiencing {weatherName}.");
                    }
                }

                if (weatherReports.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Recent climate changes reported across the realm:\n");
                    foreach (var report in weatherReports)
                    {
                        sb.AppendLine(report);
                    }

                    InformationManager.ShowInquiry(new InquiryData(
                        "Climate Events", sb.ToString(), true, false, "Understood", null, null, null));
                }
            }
        }

        // ... (OnPartyDailyTick and OnSettlementTick methods remain the same as your version) ...
        private void OnPartyDailyTick(MobileParty party)
        {
            if (!party.IsActive || party.IsBandit || party.IsMilitia || party.LeaderHero == null)
                return;

            var cultureId = party.LeaderHero.Culture.StringId;
            var weather = WeatherRegionManager.GetWeatherForCulture(cultureId);

            switch (weather)
            {
                case WeatherType.Blizzard:
                    party.MoraleExplained.Add(cultureId.Contains("sturgia") ? -2f : -5f, new TextObject("Snow storm"));
                    break;
                case WeatherType.Drought:
                    party.MoraleExplained.Add(cultureId.Contains("aserai") ? -1f : -3f, new TextObject("Drought"));
                    break;
                case WeatherType.Rainy:
                    party.MoraleExplained.Add(-2f, new TextObject("Rainstorm"));
                    break;
                case WeatherType.Flooding:
                    party.MoraleExplained.Add(-4f, new TextObject("Flooded roads"));
                    break;
                case WeatherType.Abundance:
                    party.MoraleExplained.Add(2f, new TextObject("Favorable weather"));
                    break;
            }
        }

        private void OnSettlementTick(Settlement settlement)
        {
            if (settlement == null || settlement.Culture == null || !settlement.IsFortification)
                return;

            if (MBRandom.RandomFloat >= 0.4f)
                return;

            var cultureId = settlement.Culture.StringId;
            var weather = WeatherRegionManager.GetWeatherForCulture(cultureId);

            if (settlement.Town != null)
            {
                switch (weather)
                {
                    case WeatherType.Blizzard:
                        settlement.Town.Prosperity -= cultureId.Contains("sturgia") ? 0.5f : 1f;
                        if (settlement.IsCastle) settlement.Town.Loyalty -= 0.5f;
                        break;
                    case WeatherType.Drought:
                        settlement.Town.Prosperity -= cultureId.Contains("aserai") ? 0.5f : 1f;
                        if (settlement.IsCastle) settlement.Town.Loyalty -= 0.5f;
                        break;
                    case WeatherType.Flooding:
                        settlement.Town.Prosperity = Math.Max(0, settlement.Town.Prosperity - 2f);
                        break;
                    case WeatherType.Abundance:
                        settlement.Town.Prosperity += 1f;
                        if (settlement.IsCastle) settlement.Town.Loyalty += 0.3f;
                        break;
                }
            }

            if (settlement.Village != null)
            {
                switch (weather)
                {
                    case WeatherType.Blizzard:
                    case WeatherType.Drought:
                    case WeatherType.Flooding:
                        settlement.Village.Hearth = Math.Max(0, settlement.Village.Hearth - 30f);
                        break;
                    case WeatherType.Abundance:
                        settlement.Village.Hearth += 20f;
                        break;
                }
            }
        }
    }
}

