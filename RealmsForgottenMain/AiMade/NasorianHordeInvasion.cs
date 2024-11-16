using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    internal class NasorianHordeInvasion : CampaignBehaviorBase
    {
        private const int SpawnIntervalDays = 20;
        private const float GrowthFactor = 0.10f;
        private List<Settlement> towns;
        private int lastSpawnDay;
        private float cumulativeGrowth = 1.0f; // Start with no growth

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("lastSpawnDay", ref lastSpawnDay);
            dataStore.SyncData("cumulativeGrowth", ref cumulativeGrowth);
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            Initialize();
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            Initialize();
        }

        private void Initialize()
        {
            towns = Settlement.All.Where(s => s.IsTown).ToList();

            if (lastSpawnDay == 0)
            {
                lastSpawnDay = (int)CampaignTime.Now.ToDays; // Use ToDays to get the current day as a float and cast to int
            }
        }

        private void OnDailyTick()
        {
            InformationManager.DisplayMessage(new InformationMessage("Daily Tick Triggered", Colors.Green)); // Debug message
            CheckAndSpawnBanditParties();
        }

        private void CheckAndSpawnBanditParties()
        {
            int currentDay = (int)CampaignTime.Now.ToDays; // Use ToDays for current day

            if (currentDay - lastSpawnDay >= SpawnIntervalDays)
            {
                SpawnBanditParties();
                lastSpawnDay = currentDay;
            }
        }
        private void SpawnBanditParties()
        {
            cumulativeGrowth += GrowthFactor;

            if (towns == null || !towns.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("No towns available for bandit spawning.", Colors.Red));
                return;
            }

            Random rnd = new Random();
            Settlement settlement = towns[rnd.Next(towns.Count)];

            if (settlement != null)
            {
                var banditParty = CreateBanditParty(settlement);
                if (banditParty != null)
                {
                    banditParty.Position2D = settlement.Position2D;
                    if (banditParty.Ai != null)
                    {
                        EngageNearbyEnemies(banditParty);
                    }
                    InformationManager.DisplayMessage(new InformationMessage($"A Nasorian Horde party has been seen near {settlement.Name}."));
                }
            }
        }

        private MobileParty CreateBanditParty(Settlement settlement)
        {
            Clan banditClan = Clan.BanditFactions.FirstOrDefault(clan => clan.StringId == "cs_nasorian_deserters");
            if (banditClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: BANDIT CLAN NOT FOUND.", Colors.Red));
                return null;
            }

            MobileParty banditParty = BanditPartyComponent.CreateBanditParty(banditClan.StringId, banditClan, settlement.Hideout, true);
            if (banditParty == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: Failed to create bandit party.", Colors.Red));
                return null;
            }

            TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
            var banditTroops = GetBanditTroops();

            foreach (var banditTroop in banditTroops)
            {
                CharacterObject troop = CharacterObject.Find(banditTroop.Character.StringId);
                if (troop == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Troop with ID {banditTroop.Character.StringId} not found."));
                    continue;
                }

                int adjustedNumber = (int)(banditTroop.Number * cumulativeGrowth);
                troopRoster.AddToCounts(troop, adjustedNumber);
            }

            banditParty.InitializeMobilePartyAroundPosition(troopRoster, TroopRoster.CreateDummyTroopRoster(), settlement.Position2D, 50f, 10f);
            banditParty.SetCustomName(new TextObject("Nasorian Horde"));
            banditParty.Aggressiveness = 10f;

            return banditParty;
        }

        private IEnumerable<(CharacterObject Character, int Number)> GetBanditTroops()
        {
            return new List<(CharacterObject, int)>
            {
                (CharacterObject.Find("cs_nasorian_deserters_bandits_bandit"), 15),
                (CharacterObject.Find("cs_nasorian_deserters_bandits_raider"), 10),
                (CharacterObject.Find("cs_nasorian_deserters_bandits_chief"), 5),
                (CharacterObject.Find("cs_nasorian_deserters_bandits_boss"), 1),
            };
        }

        private void EngageNearbyEnemies(MobileParty banditParty)
        {
            List<MobileParty> nearbyEnemyParties = MobileParty.All
                .Where(p => (p.IsLordParty || IsVillagerParty(p) || p.IsCaravan || p.IsBandit) && p.MapFaction.IsAtWarWith(banditParty.MapFaction))
                .OrderBy(p => p.Position2D.DistanceSquared(banditParty.Position2D))
                .ToList();

            if (nearbyEnemyParties.Count > 0)
            {
                MobileParty target = nearbyEnemyParties.First();
                banditParty.Ai.SetMoveEngageParty(target);
            }
        }

        private bool IsVillagerParty(MobileParty party)
        {
            return party.PartyComponent is VillagerPartyComponent;
        }
    }
}
