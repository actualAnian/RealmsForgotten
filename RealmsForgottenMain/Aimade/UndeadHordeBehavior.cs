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
    public class UndeadHordeBehavior : CampaignBehaviorBase
    {
        private const int SpawnIntervalDays = 130;
        private const float GrowthFactor = 0.10f;
        private List<string> settlementIds = new List<string>
        {
            "town_S1", "town_S3", "town_S4", "town_S6"
        };

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
            // Sync the last spawn day and cumulative growth variables
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
            // No need to register OnNewGameTick as we handle it in DailyTickEvent
        }

        private void OnDailyTick()
        {
            CheckAndSpawnBanditParties();
        }

        private void CheckAndSpawnBanditParties()
        {
            if (CampaignTime.Now.GetDayOfYear % SpawnIntervalDays == 0)
            {
                SpawnBanditParties();
            }
        }

        private void SpawnBanditParties()
        {
            cumulativeGrowth += GrowthFactor; // Increase the growth factor by 10% each time
            foreach (var settlementId in settlementIds)
            {
                var settlement = Settlement.Find(settlementId);
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
                        // Print a message that the bandit party has been spawned
                        InformationManager.DisplayMessage(new InformationMessage($"A Nomadic Horde party has been seen near {settlement.Name}."));
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Settlement with ID {settlementId} not found.", Colors.Red));
                }
            }
        }

        private MobileParty CreateBanditParty(Settlement settlement)
        {
            Clan banditClan = Clan.BanditFactions.FirstOrDefault(clan => clan.StringId == "cs_undead_horde");
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
                // Apply the cumulative growth factor to the number of troops
                int adjustedNumber = (int)(banditTroop.Number * cumulativeGrowth);
                troopRoster.AddToCounts(troop, adjustedNumber);
            }

            banditParty.InitializeMobilePartyAroundPosition(troopRoster, TroopRoster.CreateDummyTroopRoster(), settlement.Position2D, 50f, 10f);
            banditParty.SetCustomName(new TextObject("Nomadic Horde"));
            banditParty.Aggressiveness = 10f;

            return banditParty;
        }

        private IEnumerable<(CharacterObject Character, int Number)> GetBanditTroops()
        {
            // Define the bandit troop types and their counts
            var banditTroops = new List<(CharacterObject, int)>
            {
                (CharacterObject.Find("cs_undead_bandits_bandit"), 120),
                (CharacterObject.Find("cs_undead_bandits_raiders"), 70),
                (CharacterObject.Find("cs_undead_bandits_chief"), 25),
                (CharacterObject.Find("cs_undead_bandits_boss"), 1),
            };
            return banditTroops;
        }

        private void EngageNearbyEnemies(MobileParty banditParty)
        {
            // Get nearby enemy parties and set them as targets
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
