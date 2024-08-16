using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Managers
{
    namespace RealmsForgotten.AiMade.Managers
    {
        public class BanditPartyManager : CampaignBehaviorBase
        {
            private const int MaxBanditPartiesPerClan = 10;
            private const int MaxTotalBanditParties = 50;

            // Fields to sync across game sessions
            private int _totalBanditParties;
            private Dictionary<Clan, int> _banditClanPartyCounts;
            private CampaignTime _lastDailyTick;

            public BanditPartyManager()
            {
                _totalBanditParties = 0;
                _banditClanPartyCounts = new Dictionary<Clan, int>();
                _lastDailyTick = CampaignTime.Now;
            }

            public override void RegisterEvents()
            {
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            }

            public override void SyncData(IDataStore dataStore)
            {
                // Sync the total number of bandit parties
                dataStore.SyncData("_totalBanditParties", ref _totalBanditParties);

                // Sync the dictionary tracking the number of bandit parties per clan
                dataStore.SyncData("_banditClanPartyCounts", ref _banditClanPartyCounts);

                // Sync the last time the daily tick occurred
                dataStore.SyncData("_lastDailyTick", ref _lastDailyTick);
            }

            private void OnDailyTick()
            {
                EnsureBanditParties();
                _lastDailyTick = CampaignTime.Now;
            }

            public void EnsureBanditParties()
            {
                int totalBanditParties = GetTotalBanditPartyCount();

                if (totalBanditParties < MaxTotalBanditParties)
                {
                    List<Clan> banditClans = Clan.BanditFactions?.ToList();

                    if (banditClans == null || !banditClans.Any())
                        return;

                    foreach (Clan banditClan in banditClans)
                    {
                        if (banditClan == null)
                            continue;

                        int currentBanditParties = GetBanditPartyCount(banditClan);

                        if (currentBanditParties < MaxBanditPartiesPerClan)
                        {
                            int partiesToSpawn = MaxBanditPartiesPerClan - currentBanditParties;
                            SpawnBanditParties(banditClan, partiesToSpawn);
                        }
                    }
                }
            }

            private int GetBanditPartyCount(Clan banditClan)
            {
                return MobileParty.All?.Count(party => party.ActualClan == banditClan && party.IsBandit) ?? 0;
            }

            private int GetTotalBanditPartyCount()
            {
                return MobileParty.All?.Count(party => party.IsBandit) ?? 0;
            }

            private void SpawnBanditParties(Clan banditClan, int numberOfPartiesToSpawn)
            {
                List<Hideout> hideouts = Hideout.All?.Where(h => h.MapFaction == banditClan.MapFaction).ToList();

                if (hideouts == null || hideouts.Count == 0)
                    return;

                for (int i = 0; i < numberOfPartiesToSpawn; i++)
                {
                    Hideout targetHideout = hideouts.GetRandomElement();
                    if (targetHideout != null)
                    {
                        SpawnBanditParty(banditClan, targetHideout);
                    }
                }
            }

            private void SpawnBanditParty(Clan banditClan, Hideout targetHideout)
            {
                if (banditClan == null || targetHideout == null)
                    return;

                MobileParty banditParty = BanditPartyComponent.CreateBanditParty(banditClan.StringId, banditClan, targetHideout, true);
                if (banditParty == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("ERROR: Failed to create bandit party.", Colors.Red));
                    return;
                }

                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();

                var banditTroops = new List<(string troopId, int count)>
            {
                ("looter", 30),   
                ("sea_raider", 20), 
                ("forest_bandits", 15),
                ("cs_looters", 20),
                ("mountain_bandits", 20),
                ("desert_bandits", 20),
                ("steppe_bandits", 20),
                ("gorakthar_giants", 20),
                ("orguz_raiders", 20),
                ("trolls_raiders", 20),
                ("urkrish", 20),
                ("athas_enslavers", 20),
                ("athas_enslavers_big", 20),
                ("deserted_military", 20),
                ("vagabonds_army", 20),
                ("arena_warriors_army", 20),
                ("deserted_military", 20),
                ("cs_athascultists", 20),
                ("cs_nasorian_deserters", 20),
                ("cs_sea_outlaws", 20),
            };

                foreach (var (troopId, count) in banditTroops)
                {
                    var troop = CharacterObject.Find(troopId);
                    if (troop != null)
                    {
                        troopRoster.AddToCounts(troop, count);
                    }
                }

                banditParty.InitializeMobilePartyAroundPosition(troopRoster, TroopRoster.CreateDummyTroopRoster(), targetHideout.Settlement.Position2D, 50f, 10f);
                if (banditParty.Ai != null && targetHideout.Settlement != null)
                {
                    banditParty.Ai.SetMoveGoToSettlement(targetHideout.Settlement);
                }
            }
        }
    }
}