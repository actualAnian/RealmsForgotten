using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using Helpers;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RF_ResourceZones
{
    /// <summary>
    /// Core of the resource-zones system (PLANO_RESOURCE_ZONES.md). Zones are
    /// stationary clan-owned parties spawned from the content manifest:
    /// - daily production into the party's ItemRoster (gold mines accrue denars),
    /// - caravans hauling batches to the bound town's market (revenue → owner),
    /// - capture through ordinary battles (MapEventEnded transfers ownership),
    /// - a game menu for the peaceful visit (status/collect/donate/upgrade).
    /// </summary>
    public class ResourceZonesCampaignBehavior : CampaignBehaviorBase
    {
        private const string MenuId = "rf_resource_zone";

        public static ResourceZonesCampaignBehavior? Instance { get; private set; }

        /// <summary>Master runtime switch for ALL dynamic zone behaviour
        /// (production, caravans, capture handling, AI hunts, war greed). When
        /// false the zones just sit inert on the map — nothing is spawned,
        /// despawned or saved differently, so toggling it is completely
        /// save-safe. Flipped by the rf_zones.enable/disable console commands
        /// for A/B crash isolation without touching the manifest file.</summary>
        public static bool RuntimeEnabled = true;

        private List<ResourceZoneRecord> _records = new();

        private readonly Dictionary<string, ResourceZoneDefinition> _definitions = new();
        private readonly Dictionary<string, MobileParty> _zoneParties = new();

        public ResourceZonesCampaignBehavior()
        {
            Instance = this;
        }

        public ResourceZoneRecord? GetRecord(string zoneId)
        {
            return _records.FirstOrDefault(record => record.ZoneId == zoneId);
        }

        // ── A/B disable flag (persisted next to the logs, never in the save) ──

        private static string DisableFlagPath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord", "Configs", "rf_resource_zones.disabled");
        }

        public static bool DisableFlagFileExists()
        {
            try { return System.IO.File.Exists(DisableFlagPath()); }
            catch { return false; }
        }

        /// <summary>Persists the enable/disable choice and applies it live.
        /// Returns a human-readable status for the console.</summary>
        public static string SetRuntimeEnabled(bool enabled)
        {
            RuntimeEnabled = enabled;
            try
            {
                string path = DisableFlagPath();
                if (enabled)
                {
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                else
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                    System.IO.File.WriteAllText(path, "resource zones disabled for A/B crash isolation");
                }
            }
            catch (Exception exception)
            {
                return $"Flag write failed ({exception.Message}); the setting applies THIS session only.";
            }

            return enabled
                ? "Resource zones ENABLED. Reload the save to respawn/reactivate the mines."
                : "Resource zones DISABLED and will stay dormant across loads. Reload to test.";
        }

        /// <summary>Live zones (record + active party), for the ambition layer.</summary>
        public List<(ResourceZoneRecord Record, MobileParty Party)> GetLiveZones()
        {
            List<(ResourceZoneRecord, MobileParty)> result = new();
            foreach (ResourceZoneRecord record in _records)
            {
                if (_definitions.ContainsKey(record.ZoneId)
                    && _zoneParties.TryGetValue(record.ZoneId, out MobileParty? party)
                    && party != null && party.IsActive)
                {
                    result.Add((record, party));
                }
            }
            return result;
        }

        /// <summary>
        /// Live-registers a zone authored mid-session by rf_zones.mark_zone: the
        /// definition joins the manifest set and the party spawns immediately,
        /// so the author sees the tent appear without reloading the session.
        /// </summary>
        public void RegisterRuntimeZone(ResourceZoneDefinition definition)
        {
            _definitions[definition.Id] = definition;
            ResourceZoneRecord? record = GetRecord(definition.Id);
            if (record == null)
            {
                record = new ResourceZoneRecord { ZoneId = definition.Id, OwnerClan = null };
                _records.Add(record);
            }

            EnsureZoneParty(record, definition);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        }

        /// <summary>Narrative flavor for F3.5: when a war starts against a
        /// kingdom whose zones the attacker strongly covets, say so — the
        /// player learns that mines now start wars.</summary>
        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (faction1 is not Kingdom attacker || faction2 is not Kingdom defender)
            {
                return;
            }

            if (ResourceZoneGreedEvaluator.GetGreed(attacker, defender) < 0.5f)
            {
                return;
            }

            TextObject message = new("{=rf_zone_greed_war}The mines of {DEFENDER} glitter in the eyes of {ATTACKER} — a war of greed begins.");
            message.SetTextVariable("ATTACKER", attacker.Name);
            message.SetTextVariable("DEFENDER", defender.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFFD8B863u)));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("rfResourceZoneRecords", ref _records);
            _records ??= new List<ResourceZoneRecord>();
        }

        // ── Session bootstrap ────────────────────────────────────────────────

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddGameMenus(starter);

            // A/B crash isolation: rf_zones.disable persists a flag file so the
            // whole system stays dormant across loads WITHOUT touching the
            // manifest or the save (no spawns, no despawns, records untouched).
            RuntimeEnabled = !DisableFlagFileExists();
            if (!RuntimeEnabled)
            {
                Debug.Print("[RF_ResourceZones] Runtime DISABLED (rf_zones.disable flag present) — zones dormant this session.");
                return;
            }

            _definitions.Clear();
            foreach (ResourceZoneDefinition definition in ResourceZoneManifest.Load())
            {
                _definitions[definition.Id] = definition;
            }

            RelinkZoneParties();

            foreach (ResourceZoneDefinition definition in _definitions.Values)
            {
                ResourceZoneRecord? record = GetRecord(definition.Id);
                if (record == null)
                {
                    record = new ResourceZoneRecord { ZoneId = definition.Id, OwnerClan = null };
                    _records.Add(record);
                }

                EnsureZoneParty(record, definition);
            }

            // F3.5: wealth differentials now weigh on WAR DECLARATIONS — the
            // planner asks this provider how much each kingdom covets another's
            // zones (RFWarDecisionPlannerBehavior.GetWarSelectionBias).
            RF_warsystem.RFWarExternalIntentApi.SetResourceGreedProvider(ResourceZoneGreedEvaluator.GetGreed);

            // Grand designs: Nasoria's wealth and the Dwarf/Urkhai mountain war
            // now count the zones (KingdomObjectiveService, via reflection).
            ResourceZoneObjectiveBridge.Install();
        }

        private void RelinkZoneParties()
        {
            _zoneParties.Clear();
            List<MobileParty> orphans = new();
            foreach (MobileParty party in MobileParty.All)
            {
                if (party.PartyComponent is not ResourceZonePartyComponent component
                    || string.IsNullOrEmpty(component.ZoneId))
                {
                    continue;
                }

                // A zone whose slot was removed/renamed in the manifest is an
                // orphan: despawn the camp (its record stays dormant in the
                // save, harmless). Keeps re-authoring slots consequence-free.
                if (!_definitions.ContainsKey(component.ZoneId))
                {
                    orphans.Add(party);
                    continue;
                }

                // DEDUP: an earlier bug (or a mid-session re-spawn race) could
                // leave two parties for one zone — the empty ghost the player
                // saw at "0" troops next to the real garrison. Keep the stronger
                // one, despawn the rest.
                if (_zoneParties.TryGetValue(component.ZoneId, out MobileParty? existing) && existing != null)
                {
                    MobileParty weaker = party.MemberRoster.TotalManCount >= existing.MemberRoster.TotalManCount
                        ? existing : party;
                    MobileParty stronger = weaker == existing ? party : existing;
                    _zoneParties[component.ZoneId] = stronger;
                    orphans.Add(weaker);
                    stronger.Ai.DisableAi();
                    continue;
                }

                _zoneParties[component.ZoneId] = party;
                // AI state is runtime-only: re-hold every zone after load so
                // the garrison never wanders off its deposit.
                party.Ai.DisableAi();
            }

            foreach (MobileParty orphan in orphans)
            {
                Debug.Print($"[RF_ResourceZones] Despawning orphan zone party '{orphan.StringId}' (slot no longer in manifest).");
                DestroyPartyAction.Apply(null, orphan);
            }
        }

        // ── Zone party lifecycle ─────────────────────────────────────────────

        private void EnsureZoneParty(ResourceZoneRecord record, ResourceZoneDefinition definition)
        {
            if (_zoneParties.TryGetValue(record.ZoneId, out MobileParty? existing)
                && existing != null && existing.IsActive)
            {
                return;
            }

            // Belt-and-braces against duplicate spawns: if a party for this zone
            // already lives on the map (dict missed it after a load), adopt it
            // instead of creating a second one.
            foreach (MobileParty candidate in MobileParty.All)
            {
                if (candidate.IsActive
                    && candidate.PartyComponent is ResourceZonePartyComponent existingComponent
                    && existingComponent.ZoneId == record.ZoneId)
                {
                    _zoneParties[record.ZoneId] = candidate;
                    record.PartyId = candidate.StringId;
                    candidate.Ai.DisableAi();
                    return;
                }
            }

            CampaignVec2 position;
            if (definition.HasAbsolutePosition)
            {
                position = new CampaignVec2(new Vec2(definition.PosX, definition.PosY), isOnLand: true);
            }
            else
            {
                Settlement? anchor = Settlement.Find(definition.AnchorSettlementId);
                if (anchor == null)
                {
                    Debug.Print($"[RF_ResourceZones] Zone '{definition.Id}' skipped: anchor settlement '{definition.AnchorSettlementId}' not found.");
                    return;
                }
                position = anchor.GatePosition + new Vec2(definition.OffsetX, definition.OffsetY);
            }

            Clan? owner = record.OwnerClan;
            bool banditHeld = owner == null || owner.IsEliminated;
            if (banditHeld)
            {
                owner = FindBanditClan();
                record.OwnerClan = null; // bandit-held zones stay "unowned" in the record
            }

            CharacterObject? garrisonTroop = ResolveGarrisonTroop(banditHeld ? owner : record.OwnerClan);
            if (garrisonTroop == null)
            {
                Debug.Print($"[RF_ResourceZones] Zone '{definition.Id}' skipped: no garrison troop resolvable.");
                return;
            }

            TroopRoster members = TroopRoster.CreateDummyTroopRoster();
            int garrisonSize = banditHeld
                ? ResourceZoneRules.InitialBanditGarrison
                : ResourceZoneRules.PostCaptureGarrison;
            members.AddToCounts(garrisonTroop, garrisonSize);

            MobileParty party = MobileParty.CreateParty(
                $"rf_zone_party_{definition.Id}_{(long)CampaignTime.Now.ToMilliseconds}",
                new ResourceZonePartyComponent(definition.Id, definition.Name, definition.Type));
            party.ActualClan = banditHeld ? owner : record.OwnerClan;
            party.InitializeMobilePartyAroundPosition(
                members, TroopRoster.CreateDummyTroopRoster(), position, 0.5f);
            party.ItemRoster.AddToCounts(DefaultItems.Grain, 10);
            party.Ai.DisableAi();

            record.PartyId = party.StringId;
            _zoneParties[record.ZoneId] = party;
        }

        private static Clan? FindBanditClan()
        {
            return Clan.BanditFactions.FirstOrDefault(clan => !clan.IsEliminated && clan.Culture != null)
                   ?? Clan.BanditFactions.FirstOrDefault();
        }

        private static CharacterObject? ResolveGarrisonTroop(Clan? clan)
        {
            return clan?.Culture?.BasicTroop
                   ?? clan?.Culture?.BanditBoss
                   ?? MBObjectManager.Instance?.GetObject<CharacterObject>("looter");
        }

        // ── Daily economy ────────────────────────────────────────────────────

        private void OnDailyTick()
        {
            if (!RuntimeEnabled)
            {
                return;
            }

            foreach (ResourceZoneRecord record in _records)
            {
                if (!_definitions.TryGetValue(record.ZoneId, out ResourceZoneDefinition? definition))
                {
                    continue; // slot removed from the manifest — dormant, not deleted
                }

                EnsureZoneParty(record, definition);
                if (!_zoneParties.TryGetValue(record.ZoneId, out MobileParty? party)
                    || party == null || !party.IsActive)
                {
                    continue;
                }

                Produce(record, definition, party);
                RegenerateGarrison(record, party);
                TryDispatchCaravan(record, definition, party);
            }
        }

        private static void Produce(ResourceZoneRecord record, ResourceZoneDefinition definition, MobileParty party)
        {
            // Lazy richness roll: covers freshly created records AND records
            // from saves made before the deposit mechanic existed.
            if (record.Richness <= 0)
            {
                record.Richness = ResourceZoneRules.RollRichness();
                record.ReserveUnits = ResourceZoneRules.ReserveCapacity(definition.Type, record.Richness);
            }

            float today = (float)CampaignTime.Now.ToDays;
            if (record.ExhaustedUntilDay > 0f)
            {
                if (today < record.ExhaustedUntilDay)
                {
                    return; // prospectors still reopening the works
                }

                record.ExhaustedUntilDay = 0f;
                record.ReserveUnits = ResourceZoneRules.ReserveCapacity(definition.Type, record.Richness);
                NotifyPlayerOwner(record, party,
                    "{=rf_zone_reopened}The deposit at {ZONE} has been reopened — production resumes.",
                    0xFF9BC8A0u);
            }

            int units = (int)(ResourceZoneRules.UnitsPerDay(definition.Type, record.Tier)
                              * ResourceZoneRules.RichnessYieldMultiplier(record.Richness));
            if (units <= 0)
            {
                return;
            }

            units = Math.Min(units, record.ReserveUnits);
            record.ReserveUnits -= units;

            if (definition.Type == ResourceZoneType.Gold)
            {
                // Gold mines coin wealth directly; the owner collects at the menu.
                record.StoredGold += units;
            }
            else
            {
                ItemObject? item = ResourceZoneRules.ProducedItem(definition.Type);
                if (item != null)
                {
                    party.ItemRoster.AddToCounts(item, units);
                }
            }

            if (record.ReserveUnits <= 0)
            {
                record.ExhaustedUntilDay = today + ResourceZoneRules.RecoveryDays(record.Richness);
                NotifyPlayerOwner(record, party,
                    "{=rf_zone_exhausted}The deposit at {ZONE} is exhausted — the workers need {DAYS} days to reopen the vein.",
                    0xFFC8A66Eu,
                    ResourceZoneRules.RecoveryDays(record.Richness));
            }
        }

        private static void NotifyPlayerOwner(ResourceZoneRecord record, MobileParty party, string template, uint color, int days = 0)
        {
            if (record.OwnerClan != Clan.PlayerClan)
            {
                return;
            }

            TextObject message = new(template);
            message.SetTextVariable("ZONE", party.Name);
            message.SetTextVariable("DAYS", days);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(color)));
        }

        private void RegenerateGarrison(ResourceZoneRecord record, MobileParty party)
        {
            // The player reinforces by donating; AI and bandit owners trickle.
            if (record.OwnerClan == Clan.PlayerClan)
            {
                return;
            }

            int cap = ResourceZoneRules.GarrisonCap(record.Tier);
            if (party.MemberRoster.TotalManCount >= cap)
            {
                return;
            }

            CharacterObject? troop = ResolveGarrisonTroop(record.OwnerClan ?? party.ActualClan);
            if (troop != null)
            {
                int missing = cap - party.MemberRoster.TotalManCount;
                party.MemberRoster.AddToCounts(troop, Math.Min(ResourceZoneRules.GarrisonRegenPerDay, missing));
            }
        }

        // ── Caravans ─────────────────────────────────────────────────────────

        private void TryDispatchCaravan(ResourceZoneRecord record, ResourceZoneDefinition definition, MobileParty zoneParty)
        {
            Clan? owner = record.OwnerClan;
            if (owner == null || owner.IsEliminated || owner.IsBanditFaction)
            {
                return; // bandit squatters hoard, they don't trade
            }

            if (!string.IsNullOrEmpty(record.ActiveCaravanPartyId))
            {
                return;
            }

            if (CampaignTime.Now.ToDays - record.LastCaravanDay < ResourceZoneRules.CaravanCooldownDays)
            {
                return;
            }

            ItemObject? item = ResourceZoneRules.ProducedItem(definition.Type);
            if (item == null)
            {
                return; // gold mines have no goods to haul
            }

            int load = ResourceZoneRules.CaravanLoad(record.Tier);
            if (zoneParty.ItemRoster.GetItemNumber(item) < load)
            {
                return;
            }

            Settlement? town = ResolveBoundTown(definition, zoneParty);
            if (town?.Town == null || town.IsUnderSiege)
            {
                return;
            }

            CharacterObject? escortTroop = ResolveGarrisonTroop(owner);
            if (escortTroop == null)
            {
                return;
            }

            TroopRoster escort = TroopRoster.CreateDummyTroopRoster();
            escort.AddToCounts(escortTroop, 4 * record.Tier);

            MobileParty caravan = MobileParty.CreateParty(
                $"rf_zone_caravan_{record.ZoneId}_{(long)CampaignTime.Now.ToMilliseconds}",
                new ResourceCaravanPartyComponent(record.ZoneId, definition.Name, town));
            caravan.ActualClan = owner;
            caravan.InitializeMobilePartyAroundPosition(
                escort, TroopRoster.CreateDummyTroopRoster(), zoneParty.Position, 0.5f);

            zoneParty.ItemRoster.AddToCounts(item, -load);
            caravan.ItemRoster.AddToCounts(item, load);
            caravan.ItemRoster.AddToCounts(DefaultItems.Grain, 6);
            caravan.SetMoveGoToSettlement(town, MobileParty.NavigationType.All, false);

            record.ActiveCaravanPartyId = caravan.StringId;
            record.LastCaravanDay = (float)CampaignTime.Now.ToDays;
        }

        private static Settlement? ResolveBoundTown(ResourceZoneDefinition definition, MobileParty zoneParty)
        {
            Clan? boundOwnerClan = Instance?.GetRecord(definition.Id)?.OwnerClan;
            if (!string.IsNullOrEmpty(definition.BoundTownId))
            {
                Settlement? bound = Settlement.Find(definition.BoundTownId);
                // The authored bound town holds only while it isn't at war with
                // the zone's CURRENT owner — after a capture the caravans must
                // re-route instead of marching into an enemy gate.
                if (bound?.Town != null
                    && (boundOwnerClan == null || bound.MapFaction == null
                        || !bound.MapFaction.IsAtWarWith(boundOwnerClan.MapFaction)))
                {
                    return bound;
                }
            }

            // No usable bound town: nearest FRIENDLY market first — a caravan
            // ordered into a town at war with its owner would be turned away at
            // the gates (or eaten on the way in). Fall back to nearest of any
            // allegiance only when the owner is at war with every neighbour.
            Clan? owner = boundOwnerClan;
            Settlement? friendly = SettlementHelper.FindNearestSettlementToPoint(
                zoneParty.Position,
                s => s.IsTown && (owner == null || s.MapFaction == null || !s.MapFaction.IsAtWarWith(owner.MapFaction)));
            return friendly
                   ?? SettlementHelper.FindNearestSettlementToPoint(zoneParty.Position, s => s.IsTown);
        }

        private void OnHourlyTick()
        {
            if (!RuntimeEnabled)
            {
                return;
            }

            // Cheap upkeep: keep caravans on course (engine short-term behaviors
            // love to override goto orders — the bandit-rally lesson) and keep
            // zone garrisons pinned to their deposit.
            foreach (ResourceZoneRecord record in _records)
            {
                // A zone beaten in battle respawns within the hour under its
                // new owner — the capture should feel immediate, not next-day.
                if ((!_zoneParties.TryGetValue(record.ZoneId, out MobileParty? zoneParty)
                        || zoneParty == null || !zoneParty.IsActive)
                    && _definitions.TryGetValue(record.ZoneId, out ResourceZoneDefinition? definition))
                {
                    EnsureZoneParty(record, definition);
                    _zoneParties.TryGetValue(record.ZoneId, out zoneParty);
                }

                if (zoneParty != null && zoneParty.IsActive && zoneParty.MapEvent == null)
                {
                    zoneParty.Ai.DisableAi();
                }

                if (string.IsNullOrEmpty(record.ActiveCaravanPartyId))
                {
                    continue;
                }

                MobileParty? caravan = FindPartyById(record.ActiveCaravanPartyId);
                if (caravan == null || !caravan.IsActive)
                {
                    record.ActiveCaravanPartyId = string.Empty; // lost on the road
                    continue;
                }

                if (caravan.PartyComponent is ResourceCaravanPartyComponent component
                    && component.TargetTown != null
                    && caravan.MapEvent == null
                    && caravan.CurrentSettlement == null)
                {
                    caravan.SetMoveGoToSettlement(component.TargetTown, MobileParty.NavigationType.All, false);
                }
            }
        }

        private static MobileParty? FindPartyById(string partyId)
        {
            return MobileParty.All.FirstOrDefault(party => party.StringId == partyId);
        }

        private void OnSettlementEntered(MobileParty? party, Settlement settlement, Hero? hero)
        {
            if (party?.PartyComponent is not ResourceCaravanPartyComponent component)
            {
                return;
            }

            if (settlement.Town == null)
            {
                // Sheltering in a castle/village mid-route: keep the caravan
                // alive, the hourly tick re-issues the goto to its town.
                return;
            }

            ResourceZoneRecord? record = GetRecord(component.ZoneId);
            SellCaravanGoods(party, settlement, record);

            if (record != null && record.ActiveCaravanPartyId == party.StringId)
            {
                record.ActiveCaravanPartyId = string.Empty;
            }

            DestroyPartyAction.Apply(null, party);
        }

        private static void SellCaravanGoods(MobileParty caravan, Settlement town, ResourceZoneRecord? record)
        {
            int revenue = 0;
            foreach (ItemRosterElement element in caravan.ItemRoster.ToList())
            {
                if (element.EquipmentElement.Item == null || element.Amount <= 0
                    || element.EquipmentElement.Item == DefaultItems.Grain)
                {
                    continue;
                }

                int price;
                try
                {
                    price = town.Town.GetItemPrice(element.EquipmentElement);
                }
                catch
                {
                    price = element.EquipmentElement.Item.Value;
                }

                revenue += price * element.Amount;
                town.ItemRoster.AddToCounts(element.EquipmentElement, element.Amount);
                caravan.ItemRoster.AddToCounts(element.EquipmentElement, -element.Amount);
            }

            Clan? owner = record?.OwnerClan;
            Hero? recipient = owner?.Leader;
            if (revenue <= 0 || recipient == null)
            {
                return;
            }

            GiveGoldAction.ApplyBetweenCharacters(null, recipient, revenue, disableNotification: true);
            if (owner == Clan.PlayerClan)
            {
                TextObject message = new("{=rf_zone_caravan_sold}Your caravan from {ZONE} sold its goods in {TOWN} for {GOLD} denars.");
                message.SetTextVariable("ZONE", record!.ZoneId);
                message.SetTextVariable("TOWN", town.Name);
                message.SetTextVariable("GOLD", revenue);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFFD8B863u)));
            }
        }

        // ── Capture ──────────────────────────────────────────────────────────

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            foreach (PartyBase involved in mapEvent.InvolvedParties.ToList())
            {
                if (involved?.MobileParty?.PartyComponent is not ResourceZonePartyComponent component)
                {
                    continue;
                }

                ResourceZoneRecord? record = GetRecord(component.ZoneId);
                if (record == null)
                {
                    continue;
                }

                // Side by membership — PartyBase.Side may already be cleared
                // during map-event teardown.
                bool onAttackerSide = mapEvent.AttackerSide.Parties.Any(p => p.Party == involved);
                BattleSideEnum zoneSide = onAttackerSide ? BattleSideEnum.Attacker : BattleSideEnum.Defender;
                if (mapEvent.WinningSide == zoneSide || mapEvent.WinningSide == BattleSideEnum.None)
                {
                    continue; // defended (or draw) — nothing changes hands
                }

                PartyBase? victorLeader = mapEvent.GetLeaderParty(mapEvent.WinningSide);
                Clan? newOwner = victorLeader?.MobileParty?.ActualClan
                                 ?? victorLeader?.LeaderHero?.Clan;

                Clan? previousOwner = record.OwnerClan;
                record.OwnerClan = newOwner != null && !newOwner.IsBanditFaction ? newOwner : null;
                record.StoredGold = 0;          // coffers are part of the plunder
                record.ActiveCaravanPartyId = string.Empty;
                record.UpgradeProgressDays = 0; // growth restarts under the new master

                _zoneParties.Remove(component.ZoneId); // the beaten party is gone; daily tick respawns it

                AnnounceCapture(component, record, previousOwner);
            }
        }

        private static void AnnounceCapture(ResourceZonePartyComponent component, ResourceZoneRecord record, Clan? previousOwner)
        {
            TextObject message;
            if (record.OwnerClan == Clan.PlayerClan)
            {
                message = new TextObject("{=rf_zone_captured_player}{ZONE} is now yours! Garrison it and its riches will flow to your clan.");
            }
            else if (previousOwner == Clan.PlayerClan)
            {
                message = new TextObject("{=rf_zone_lost_player}You have lost {ZONE}!");
            }
            else
            {
                message = new TextObject("{=rf_zone_captured}{ZONE} has changed hands.");
            }

            message.SetTextVariable("ZONE", ((PartyComponent)component).Name);
            InformationManager.DisplayMessage(new InformationMessage(
                message.ToString(),
                record.OwnerClan == Clan.PlayerClan ? Color.FromUint(0xFF9BC8A0u) : Color.FromUint(0xFFC86E6Eu)));
        }

        // ── Menu ─────────────────────────────────────────────────────────────

        private void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                MenuId,
                "{=rf_zone_menu_desc}{ZONE_NAME} — a {ZONE_TYPE} operation on a {ZONE_RICHNESS}, tier {ZONE_TIER}. {ZONE_OWNER_LINE} {ZONE_RESERVE_LINE} The stockpile holds {ZONE_STOCK} and {ZONE_GARRISON} men stand guard.{ZONE_GOLD_LINE}",
                ZoneMenuInit);

            starter.AddGameMenuOption(
                MenuId, "rf_zone_collect",
                "{=rf_zone_collect}Collect stored gold ({ZONE_GOLD}{GOLD_ICON})",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    ResourceZoneRecord? record = CurrentRecord();
                    return record != null && record.OwnerClan == Clan.PlayerClan && record.StoredGold > 0;
                },
                args => CollectGold());

            starter.AddGameMenuOption(
                MenuId, "rf_zone_donate",
                "{=rf_zone_donate}Reinforce the garrison",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    return CurrentRecord()?.OwnerClan == Clan.PlayerClan;
                },
                args => DonateTroops());

            starter.AddGameMenuOption(
                MenuId, "rf_zone_upgrade",
                "{=rf_zone_upgrade}Upgrade ({UPGRADE_GOLD}{GOLD_ICON} + {UPGRADE_WOOD} hardwood)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    ResourceZoneRecord? record = CurrentRecord();
                    if (record == null || record.OwnerClan != Clan.PlayerClan
                        || record.Tier >= ResourceZoneRules.MaxTier)
                    {
                        return false;
                    }

                    if (!CanAffordUpgrade(record, out TextObject? reason))
                    {
                        args.IsEnabled = false;
                        args.Tooltip = reason;
                    }
                    return true;
                },
                args => UpgradeZone());

            starter.AddGameMenuOption(
                MenuId, "rf_zone_attack",
                "{=rf_zone_attack}Attack the guards and seize the mine",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Raid;
                    if (!CanAttackCurrentZone())
                    {
                        return false;
                    }

                    // Warn when the strike would start a war.
                    ResourceZoneRecord? record = CurrentRecord();
                    IFaction? ownerFaction = record?.OwnerClan?.MapFaction;
                    if (ownerFaction != null
                        && ownerFaction != Hero.MainHero.MapFaction
                        && !ownerFaction.IsAtWarWith(Hero.MainHero.MapFaction))
                    {
                        TextObject warn = new("{=rf_zone_attack_war}Seizing this mine will declare war on {FACTION}.");
                        warn.SetTextVariable("FACTION", ownerFaction.Name);
                        args.Tooltip = warn;
                    }
                    return true;
                },
                args => AttackZone());

            starter.AddGameMenuOption(
                MenuId, "rf_zone_leave",
                "{=rf_zone_leave}Leave",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => PlayerEncounter.Finish(true),
                true);
        }

        /// <summary>The player may storm any mine that isn't their own —
        /// brigand-held, enemy, OR a realm at peace. Seizing a peaceful realm's
        /// mine is an act of war, and the attack DECLARES it (like raiding a
        /// village), rather than being forbidden.</summary>
        private bool CanAttackCurrentZone()
        {
            ResourceZoneRecord? record = CurrentRecord();
            return record != null && record.OwnerClan != Clan.PlayerClan;
        }

        private void AttackZone()
        {
            if (PlayerEncounter.Current == null)
            {
                return;
            }

            // Storming a mine held by a realm still at peace is a declaration of
            // war (player-initiated hostility), exactly like raiding one of its
            // villages — better emergent politics than a hard block.
            ResourceZoneRecord? record = CurrentRecord();
            IFaction? ownerFaction = record?.OwnerClan?.MapFaction;
            if (ownerFaction != null
                && ownerFaction != Hero.MainHero.MapFaction
                && !ownerFaction.IsAtWarWith(Hero.MainHero.MapFaction))
            {
                DeclareWarAction.ApplyByPlayerHostility(Hero.MainHero.MapFaction, ownerFaction);
            }

            // Convert the peaceful visit into a battle against the garrison —
            // the same idiom QuestHelper uses for its hideout storm. Winning
            // routes through OnMapEventEnded, which transfers ownership.
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
        }

        private ResourceZonePartyComponent? CurrentComponent()
        {
            return PlayerEncounter.EncounteredParty?.MobileParty?.PartyComponent as ResourceZonePartyComponent;
        }

        private ResourceZoneRecord? CurrentRecord()
        {
            ResourceZonePartyComponent? component = CurrentComponent();
            return component == null ? null : GetRecord(component.ZoneId);
        }

        private void ZoneMenuInit(MenuCallbackArgs args)
        {
            args.MenuContext.SetBackgroundMeshName(GetZoneBackgroundMesh());

            ResourceZonePartyComponent? component = CurrentComponent();
            ResourceZoneRecord? record = CurrentRecord();
            if (component == null || record == null)
            {
                return;
            }

            MobileParty? party = PlayerEncounter.EncounteredParty?.MobileParty;
            _definitions.TryGetValue(component.ZoneId, out ResourceZoneDefinition? definition);

            MBTextManager.SetTextVariable("ZONE_NAME", ((PartyComponent)component).Name);
            MBTextManager.SetTextVariable("ZONE_TYPE", component.ZoneType.ToString());
            MBTextManager.SetTextVariable("ZONE_TIER", record.Tier);
            MBTextManager.SetTextVariable("ZONE_RICHNESS",
                ResourceZoneRules.RichnessLabel(record.Richness > 0 ? record.Richness : 2));

            TextObject reserveLine;
            float today = (float)CampaignTime.Now.ToDays;
            if (record.ExhaustedUntilDay > today)
            {
                reserveLine = new TextObject("{=rf_zone_reserve_exhausted}The deposit is EXHAUSTED — the vein reopens in {DAYS} days.");
                reserveLine.SetTextVariable("DAYS", (int)Math.Ceiling(record.ExhaustedUntilDay - today));
            }
            else if (definition != null && record.Richness > 0)
            {
                float dailyYield = ResourceZoneRules.UnitsPerDay(definition.Type, record.Tier)
                                   * ResourceZoneRules.RichnessYieldMultiplier(record.Richness);
                int daysLeft = dailyYield > 0f ? (int)Math.Ceiling(record.ReserveUnits / dailyYield) : 0;
                reserveLine = new TextObject("{=rf_zone_reserve_ok}The deposit holds roughly {DAYS} more days of work.");
                reserveLine.SetTextVariable("DAYS", daysLeft);
            }
            else
            {
                reserveLine = new TextObject("{=rf_zone_reserve_unknown}The deposit has yet to be surveyed.");
            }
            MBTextManager.SetTextVariable("ZONE_RESERVE_LINE", reserveLine);

            TextObject ownerLine = record.OwnerClan != null
                ? new TextObject("{=rf_zone_owner_line}It is worked in the name of {OWNER}.")
                : new TextObject("{=rf_zone_owner_bandits}Brigands squat here, bleeding the deposit.");
            ownerLine.SetTextVariable("OWNER", record.OwnerClan?.Name ?? new TextObject(""));
            MBTextManager.SetTextVariable("ZONE_OWNER_LINE", ownerLine);

            int stock = 0;
            if (definition != null && party != null)
            {
                ItemObject? item = ResourceZoneRules.ProducedItem(definition.Type);
                stock = item != null ? party.ItemRoster.GetItemNumber(item) : 0;
            }
            TextObject stockText = new("{=rf_zone_stock}{COUNT} goods");
            stockText.SetTextVariable("COUNT", stock);
            MBTextManager.SetTextVariable("ZONE_STOCK", stockText);
            MBTextManager.SetTextVariable("ZONE_GARRISON", party?.MemberRoster.TotalManCount ?? 0);

            TextObject goldLine = record.StoredGold > 0
                ? new TextObject("{=rf_zone_gold_line} The strongbox holds {GOLD} denars.")
                : new TextObject("");
            goldLine.SetTextVariable("GOLD", record.StoredGold);
            MBTextManager.SetTextVariable("ZONE_GOLD_LINE", goldLine);

            MBTextManager.SetTextVariable("ZONE_GOLD", record.StoredGold);
            MBTextManager.SetTextVariable("UPGRADE_GOLD", ResourceZoneRules.UpgradeGoldCost(record.Tier));
            MBTextManager.SetTextVariable("UPGRADE_WOOD", ResourceZoneRules.UpgradeWoodCost(record.Tier));
        }

        private static string GetZoneBackgroundMesh()
        {
            // Same placeholder strategy as the settler camp menu.
            Settlement? sample = Settlement.All.FirstOrDefault(s =>
                s.IsVillage && !string.IsNullOrEmpty(s.SettlementComponent?.WaitMeshName));
            return sample?.SettlementComponent?.WaitMeshName ?? "wait_fallback";
        }

        private void CollectGold()
        {
            ResourceZoneRecord? record = CurrentRecord();
            if (record == null || record.StoredGold <= 0)
            {
                return;
            }

            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, record.StoredGold, disableNotification: false);
            record.StoredGold = 0;
            GameMenu.SwitchToMenu(MenuId);
        }

        private void DonateTroops()
        {
            MobileParty? party = PlayerEncounter.EncounteredParty?.MobileParty;
            if (party != null)
            {
                PartyScreenHelper.OpenScreenAsDonateTroops(party);
            }
        }

        private bool CanAffordUpgrade(ResourceZoneRecord record, out TextObject? reason)
        {
            int goldCost = ResourceZoneRules.UpgradeGoldCost(record.Tier);
            int woodCost = ResourceZoneRules.UpgradeWoodCost(record.Tier);

            if (Hero.MainHero.Gold < goldCost)
            {
                reason = new TextObject("{=rf_zone_upgrade_no_gold}Not enough denars.");
                return false;
            }

            if (MobileParty.MainParty.ItemRoster.GetItemNumber(DefaultItems.HardWood) < woodCost)
            {
                reason = new TextObject("{=rf_zone_upgrade_no_wood}Not enough hardwood in your inventory.");
                return false;
            }

            reason = null;
            return true;
        }

        private void UpgradeZone()
        {
            ResourceZoneRecord? record = CurrentRecord();
            if (record == null || record.Tier >= ResourceZoneRules.MaxTier
                || !CanAffordUpgrade(record, out _))
            {
                return;
            }

            int goldCost = ResourceZoneRules.UpgradeGoldCost(record.Tier);
            int woodCost = ResourceZoneRules.UpgradeWoodCost(record.Tier);

            Hero.MainHero.ChangeHeroGold(-goldCost);
            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.HardWood, -woodCost);
            record.Tier++;

            TextObject message = new("{=rf_zone_upgraded}The operation grows: tier {TIER} reached.");
            message.SetTextVariable("TIER", record.Tier);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFF9BC8A0u)));
            GameMenu.SwitchToMenu(MenuId);
        }
    }
}
