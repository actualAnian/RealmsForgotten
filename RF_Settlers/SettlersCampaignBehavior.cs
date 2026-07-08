using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RF_Settlers
{
    /// <summary>
    /// World-state system: kingdoms send out settler caravans that pitch camps
    /// on their frontier. Camps grow in population over time; a mature camp is
    /// the seed of a new village (phase C converts it via the Player Settlement
    /// XML-injection technique). Attacking a camp is a plain field battle
    /// against its villager inhabitants.
    /// </summary>
    public class SettlersCampaignBehavior : CampaignBehaviorBase
    {
        private const float SpawnChancePerKingdomPerDay = 0.04f;
        private const int MaxActivePerKingdom = 1;
        private const int MaxTotalCampsAndCaravans = 8;
        private const int SettlerPartySize = 25;
        private const float FrontierDistanceMin = 8f;
        private const float FrontierDistanceMax = 18f;
        private const float ArrivalDistance = 2f;
        private const float PopulationGrowthPerDay = 2.5f;
        private const float CampMaturePopulation = 120f;
        // Settlers feed themselves: without grain in the roster the party
        // starves (morale collapse, attrition) and silently wastes away —
        // vanilla keeps its scripted civilian parties alive the same way
        // (grain top-up on the daily tick).
        private const int CaravanGrainStock = 30;
        private const int CampMinGrainStock = 20;
        private const int CampMaxDefenders = 60;
        // A kingdom does not send its colonists out unprotected: a small
        // militia escort travels with the caravan and stays as the camp's
        // fighting core (villagers alone fold to the first looter band).
        private const int EscortMeleeMilitia = 6;
        private const int EscortRangedMilitia = 4;

        /// <summary>Needed by SubModule.RegisterSubModuleObjects, which runs
        /// before any campaign event and must reach this behavior's store.</summary>
        public static SettlersCampaignBehavior Instance { get; private set; }

        private List<SettlerVillageRecord> _villageRecords = new();

        public SettlersCampaignBehavior()
        {
            Instance = this;
        }

        public IReadOnlyList<SettlerVillageRecord> VillageRecords => _villageRecords;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
        }

        /// <summary>Lifecycle transitions (caravan→camp, camp→village, orphan
        /// dispersal) destroy parties on purpose; those must not read as
        /// "wiped out". Register the party id here BEFORE the planned
        /// DestroyPartyAction.</summary>
        public static readonly HashSet<string> PlannedRemovals = new();

        /// <summary>Camps and caravans die to enemies, bandits or attrition —
        /// never silently: the player always learns what happened to them.</summary>
        private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyer)
        {
            if (party?.StringId != null && PlannedRemovals.Remove(party.StringId))
            {
                return;
            }

            string what = party?.PartyComponent switch
            {
                SettlerCampComponent => "{=rf_settlers_camp_lost}The settler camp of {KINGDOM} has been wiped out{?HAS_DESTROYER} by {DESTROYER}{?}{\\?}.",
                SettlerPartyComponent => "{=rf_settlers_caravan_lost}The settler caravan of {KINGDOM} has been wiped out{?HAS_DESTROYER} by {DESTROYER}{?}{\\?}.",
                _ => null
            };
            if (what == null)
            {
                return;
            }

            Kingdom kingdom = party.PartyComponent is SettlerCampComponent campComponent
                ? campComponent.Kingdom
                : ((SettlerPartyComponent)party.PartyComponent).Kingdom;

            TextObject message = new(what);
            message.SetTextVariable("KINGDOM", kingdom?.Name ?? new TextObject("{=rf_settler_unknown}Wandering"));
            message.SetTextVariable("HAS_DESTROYER", destroyer?.Name != null ? 1 : 0);
            message.SetTextVariable("DESTROYER", destroyer?.Name ?? TextObject.GetEmpty());
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFFC87A5Au)));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("RF_Settlers_VillageRecords", ref _villageRecords);
            _villageRecords ??= new List<SettlerVillageRecord>();
        }

        /// <summary>Reads the village records straight out of the save store in
        /// RegisterSubModuleObjects, BEFORE campaign deserialization — the XML
        /// must be re-injected at that point for save references to resolve.</summary>
        public void LoadEarlySync(IDataStore dataStore)
        {
            try
            {
                if (dataStore != null && !dataStore.IsSaving)
                {
                    dataStore.SyncData("RF_Settlers_VillageRecords", ref _villageRecords);
                }
            }
            catch (System.Exception exception)
            {
                Debug.Print($"[RF_Settlers] LoadEarlySync failed: {exception}");
            }

            _villageRecords ??= new List<SettlerVillageRecord>();
        }

        private static IEnumerable<MobileParty> SettlerParties()
        {
            return MobileParty.All.Where(p => p?.PartyComponent is SettlerPartyComponent && p.IsActive);
        }

        private static IEnumerable<MobileParty> CampParties()
        {
            return MobileParty.All.Where(p => p?.PartyComponent is SettlerCampComponent && p.IsActive);
        }

        // ── Spawning settler caravans ────────────────────────────────────────

        private void OnDailyTick()
        {
            SettlersVillageFounder.RetryPendingVillages(_villageRecords);
            GrowCamps();

            int total = SettlerParties().Count() + CampParties().Count();
            if (total >= MaxTotalCampsAndCaravans)
            {
                return;
            }

            foreach (Kingdom kingdom in Kingdom.All.Where(k => !k.IsEliminated))
            {
                if (MBRandom.RandomFloat >= SpawnChancePerKingdomPerDay)
                {
                    continue;
                }

                int active = SettlerParties().Count(p => ((SettlerPartyComponent)p.PartyComponent).Kingdom == kingdom)
                    + CampParties().Count(p => ((SettlerCampComponent)p.PartyComponent).Kingdom == kingdom);
                if (active >= MaxActivePerKingdom)
                {
                    continue;
                }

                TrySpawnSettlerParty(kingdom);
            }
        }

        private void TrySpawnSettlerParty(Kingdom kingdom)
        {
            Settlement originTown = kingdom.Settlements
                .Where(s => s.IsTown)
                .GetRandomElementInefficiently();
            CharacterObject villager = kingdom.Culture?.Villager;
            if (originTown == null || villager == null || kingdom.RulingClan == null)
            {
                return;
            }

            if (!TryFindFrontierSpot(kingdom, out CampaignVec2 target))
            {
                return;
            }

            TroopRoster members = TroopRoster.CreateDummyTroopRoster();
            members.AddToCounts(villager, SettlerPartySize);
            AddMilitiaEscort(members, kingdom);

            MobileParty party = MobileParty.CreateParty(
                $"rf_settlers_{kingdom.StringId}_{CampaignTime.Now.ToMilliseconds}",
                new SettlerPartyComponent(kingdom, originTown, target));
            party.ActualClan = kingdom.RulingClan;
            party.InitializeMobilePartyAroundPosition(
                members, TroopRoster.CreateDummyTroopRoster(), originTown.GatePosition, 1f);
            party.ItemRoster.AddToCounts(DefaultItems.Grain, CaravanGrainStock);
            party.SetMoveGoToPoint(target, MobileParty.NavigationType.Default);

            TextObject message = new("{=rf_settlers_departed}Settlers have set out from {TOWN} to found a new home for {KINGDOM}.");
            message.SetTextVariable("TOWN", originTown.Name);
            message.SetTextVariable("KINGDOM", kingdom.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFF9BC8A0u)));
        }

        private static void AddMilitiaEscort(TroopRoster members, Kingdom kingdom)
        {
            CharacterObject melee = kingdom.Culture?.MeleeMilitiaTroop;
            CharacterObject ranged = kingdom.Culture?.RangedMilitiaTroop;
            if (melee != null)
            {
                members.AddToCounts(melee, EscortMeleeMilitia);
            }

            if (ranged != null)
            {
                members.AddToCounts(ranged, EscortRangedMilitia);
            }
        }

        private static bool TryFindFrontierSpot(Kingdom kingdom, out CampaignVec2 result)
        {
            result = default;

            Settlement borderVillage = kingdom.Settlements
                .Where(s => s.IsVillage)
                .GetRandomElementInefficiently();
            if (borderVillage == null)
            {
                return false;
            }

            // Push outward: away from the kingdom's demographic center, so camps
            // tend toward the frontier instead of the heartland.
            Vec2 center = Vec2.Zero;
            int count = 0;
            foreach (Settlement s in kingdom.Settlements)
            {
                center += new Vec2(s.Position.X, s.Position.Y);
                count++;
            }

            if (count == 0)
            {
                return false;
            }

            center *= 1f / count;
            Vec2 villagePosition = new(borderVillage.Position.X, borderVillage.Position.Y);
            Vec2 direction = villagePosition - center;
            direction = direction.IsValid && direction.LengthSquared > 0.01f
                ? direction.Normalized()
                : Vec2.FromRotation(MBRandom.RandomFloatRanged(0f, 6.2831f));

            var mapSceneWrapper = Campaign.Current.MapSceneWrapper;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float distance = MBRandom.RandomFloatRanged(FrontierDistanceMin, FrontierDistanceMax);
                Vec2 jitter = Vec2.FromRotation(direction.RotationInRadians + MBRandom.RandomFloatRanged(-0.8f, 0.8f));
                CampaignVec2 candidate = new(villagePosition + jitter * distance, isOnLand: true);

                var face = mapSceneWrapper.GetFaceIndex(candidate);
                if (face.IsValid()
                    && Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(
                        mapSceneWrapper.GetFaceTerrainType(face), MobileParty.NavigationType.Default))
                {
                    result = candidate;
                    return true;
                }
            }

            return false;
        }

        // ── Arrival: caravan becomes a camp ──────────────────────────────────

        private void OnHourlyTick()
        {
            foreach (MobileParty party in SettlerParties().ToList())
            {
                var component = (SettlerPartyComponent)party.PartyComponent;
                if (party.MapEvent != null || party.CurrentSettlement != null)
                {
                    continue;
                }

                if (party.Position.Distance(component.TargetPosition) > ArrivalDistance)
                {
                    // Nudge the AI in case something distracted it.
                    party.SetMoveGoToPoint(component.TargetPosition, MobileParty.NavigationType.Default);
                    continue;
                }

                FoundCamp(party, component);
            }
        }

        private void FoundCamp(MobileParty settlerParty, SettlerPartyComponent component)
        {
            Kingdom kingdom = component.Kingdom;
            if (kingdom == null || kingdom.IsEliminated || kingdom.RulingClan == null)
            {
                DestroyPartyAction.Apply(null, settlerParty);
                return;
            }

            // Only the civilians count as founding population — the militia
            // escort is protection, not settlers.
            int population = 0;
            foreach (TroopRosterElement element in settlerParty.MemberRoster.GetTroopRoster())
            {
                if (element.Character?.Occupation == Occupation.Villager)
                {
                    population += element.Number;
                }
            }

            TroopRoster members = TroopRoster.CreateDummyTroopRoster();
            members.Add(settlerParty.MemberRoster);
            CampaignVec2 position = settlerParty.Position;

            PlannedRemovals.Add(settlerParty.StringId);
            DestroyPartyAction.Apply(null, settlerParty);

            MobileParty camp = MobileParty.CreateParty(
                $"rf_settler_camp_{kingdom.StringId}_{CampaignTime.Now.ToMilliseconds}",
                new SettlerCampComponent(kingdom, component.OriginTown, population * 3f));
            camp.ActualClan = kingdom.RulingClan;
            camp.InitializeMobilePartyAroundPosition(
                members, TroopRoster.CreateDummyTroopRoster(), position, 0.5f);
            camp.ItemRoster.AddToCounts(DefaultItems.Grain, CampMinGrainStock);
            camp.Ai.DisableAi();

            // Name the nearest settlement so the player can find the new camp
            // icon on the map.
            Settlement nearest = SettlementHelper.FindNearestSettlementToPoint(
                position, s => s.IsTown || s.IsCastle || s.IsVillage);
            TextObject message = new("{=rf_settlers_camped}{KINGDOM} settlers have pitched camp on the frontier, near {NEAREST}.");
            message.SetTextVariable("KINGDOM", kingdom.Name);
            message.SetTextVariable("NEAREST", nearest?.Name ?? new TextObject("{=rf_settlers_unknown_place}uncharted lands"));
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFF9BC8A0u)));
        }

        // ── Camp life ────────────────────────────────────────────────────────

        private void GrowCamps()
        {
            foreach (MobileParty camp in CampParties().ToList())
            {
                var component = (SettlerCampComponent)camp.PartyComponent;
                if (component.Kingdom == null || component.Kingdom.IsEliminated)
                {
                    // Orphaned by their kingdom's fall: the camp disperses.
                    PlannedRemovals.Add(camp.StringId);
                    DestroyPartyAction.Apply(null, camp);
                    continue;
                }

                component.Population += PopulationGrowthPerDay;

                // Settlers farm their own food: keep the stock from ever
                // running dry (the vanilla scripted-civilian-party idiom).
                int grain = camp.ItemRoster.GetItemNumber(DefaultItems.Grain);
                if (grain < CampMinGrainStock)
                {
                    camp.ItemRoster.AddToCounts(DefaultItems.Grain, CampMinGrainStock - grain);
                }

                // The garrison grows with the population, so a maturing camp
                // is no longer the trivial 25-villager snack it was at birth.
                CharacterObject campVillager = component.Kingdom.Culture?.Villager;
                int targetDefenders = MathF.Min(CampMaxDefenders, (int)(component.Population / 3f));
                int currentDefenders = camp.MemberRoster.TotalManCount;
                if (campVillager != null && currentDefenders < targetDefenders)
                {
                    camp.MemberRoster.AddToCounts(campVillager, targetDefenders - currentDefenders);
                }

                if (!component.MatureAnnounced && component.Population >= CampMaturePopulation)
                {
                    // Phase C: the mature camp becomes a PENDING village record.
                    // Its settlement XML travels in the save and is injected on
                    // the next session load (RegisterSubModuleObjects), where
                    // the engine's own load pipeline gives it visuals, nameplate
                    // and navigation; the founder then seeds its economy and
                    // removes this camp.
                    component.MatureAnnounced = TryCreatePendingVillage(camp, component);
                }
            }
        }

        // ── Phase C: camp → village ──────────────────────────────────────────

        private bool TryCreatePendingVillage(MobileParty camp, SettlerCampComponent component)
        {
            Kingdom kingdom = component.Kingdom;

            // A village must bind to a fortification of its kingdom; without
            // one nearby the camp keeps growing and retries the next day.
            Settlement bound = SettlementHelper.FindNearestSettlementToPoint(
                camp.Position, s => (s.IsTown || s.IsCastle) && s.MapFaction == kingdom);
            if (bound == null)
            {
                return false;
            }

            string stringId = $"rf_settler_village_{_villageRecords.Count + 1}_{kingdom.StringId}";
            TextObject nameTemplate = new("{=rf_settler_village_name}New {ORIGIN}");
            nameTemplate.SetTextVariable("ORIGIN",
                component.OriginTown?.Name ?? kingdom.Culture?.Name ?? kingdom.Name);
            string displayName = nameTemplate.ToString();

            Vec2 position = new(camp.Position.X, camp.Position.Y);
            if (!SettlementXmlFactory.TryBuildVillageXml(kingdom, position, bound, stringId, displayName, out string xml))
            {
                return false;
            }

            SettlerVillageRecord record = new()
            {
                StringId = stringId,
                DisplayName = displayName,
                SettlementXml = xml,
                PrefabId = "rf_settler_village_icon_" + (kingdom.Culture?.StringId ?? "generic"),
                CampPartyId = camp.StringId,
                KingdomId = kingdom.StringId,
                Established = false
            };
            _villageRecords.Add(record);
            Patches.SettlersMapScenePatch.RegisterVillagePrefab(record.StringId, record.PrefabId);

            // Found the village RIGHT NOW, before the player's eyes. If the
            // live path cannot run at this moment, the record stays pending:
            // it retries daily and materializes on the next load at the latest.
            if (!SettlersVillageFounder.TryFoundVillageLive(record))
            {
                TextObject message = new("{=rf_settlers_mature}The settlers of {KINGDOM} have begun raising the village of {VILLAGE}.");
                message.SetTextVariable("KINGDOM", kingdom.Name);
                message.SetTextVariable("VILLAGE", displayName);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFF9BC8A0u)));
            }

            return true;
        }

        // ── Player interaction ───────────────────────────────────────────────

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Villages injected this load get their one-time economy seeding;
            // records that failed injection stay pending for the next load.
            SettlersVillageFounder.FinalizePendingVillages(_villageRecords);

            starter.AddDialogLine(
                "rf_settler_party_greet", "start", "close_window",
                "{=rf_settler_party_greet}We travel to break new ground for {SETTLER_KINGDOM}. Gods willing, there will be a village where none stood before.",
                IsSettlerPartyConversation, null);

            starter.AddDialogLine(
                "rf_settler_camp_greet", "start", "close_window",
                "{=rf_settler_camp_greet}Welcome to our camp, traveller. {CAMP_POPULATION} souls call it home already — one day this will be a true village of {SETTLER_KINGDOM}.",
                IsSettlerCampConversation, null);

            // Approaching the camp opens this menu (SettlerCampEncounterPatch
            // reroutes DoMeetingInternal here) with the culture's village
            // backdrop; conversation is one of its options.
            starter.AddGameMenu(
                "rf_settler_camp",
                "{=rf_settler_camp_desc}The tents of {SETTLER_KINGDOM} settlers spread across the frontier here. {CAMP_POPULATION} souls live under canvas, breaking ground for what they hope will one day be a true village.",
                SettlerCampMenuInit);

            starter.AddGameMenuOption(
                "rf_settler_camp", "rf_settler_camp_talk",
                "{=rf_settler_camp_talk}Talk to the settlers",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                    return true;
                },
                args => OpenCampConversation());

            starter.AddGameMenuOption(
                "rf_settler_camp", "rf_settler_camp_give_supplies",
                "{=rf_settler_camp_give_supplies}Donate supplies",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return true;
                },
                args => DonateSupplies());

            starter.AddGameMenuOption(
                "rf_settler_camp", "rf_settler_camp_give_troops",
                "{=rf_settler_camp_give_troops}Donate troops",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;
                    return true;
                },
                args => DonateTroops());

            starter.AddGameMenuOption(
                "rf_settler_camp", "rf_settler_camp_leave",
                "{=rf_settler_camp_leave}Leave",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => PlayerEncounter.Finish(true),
                true);
        }

        private void SettlerCampMenuInit(MenuCallbackArgs args)
        {
            SettlerCampComponent component =
                PlayerEncounter.EncounteredParty?.MobileParty?.PartyComponent as SettlerCampComponent;

            // Always give the menu SOME backdrop, even if the component lookup
            // fails — a blank background reads as a bug.
            args.MenuContext.SetBackgroundMeshName(GetCampBackgroundMesh(component?.Kingdom));
            if (component == null)
            {
                return;
            }

            MBTextManager.SetTextVariable("SETTLER_KINGDOM",
                component.Kingdom?.Name ?? new TextObject("{=rf_settler_unknown}Wandering"));
            MBTextManager.SetTextVariable("CAMP_POPULATION", (int)component.Population);
        }

        /// <summary>Backdrop image (placeholder until custom art exists). Game
        /// menus take WAIT meshes, not gui_bg_* panels: borrow the village
        /// wait image of the kingdom's culture from any live settlement, then
        /// the culture's encounter backdrop, then the vanilla fallback.</summary>
        private static string GetCampBackgroundMesh(Kingdom kingdom)
        {
            Settlement sample = Settlement.All.FirstOrDefault(s =>
                s.IsVillage && s.Culture == kingdom?.Culture
                && !string.IsNullOrEmpty(s.SettlementComponent?.WaitMeshName));
            if (sample != null)
            {
                return sample.SettlementComponent.WaitMeshName;
            }

            string encounterMesh = kingdom?.Culture?.EncounterBackgroundMesh;
            return !string.IsNullOrEmpty(encounterMesh) ? encounterMesh : "wait_fallback";
        }

        /// <summary>Opens the item-transfer screen against the camp's stores —
        /// donated food extends the settlers' stock (the daily top-up only
        /// fills UP TO the minimum; gifts above it are kept).</summary>
        private static void DonateSupplies()
        {
            MobileParty camp = PlayerEncounter.EncounteredParty?.MobileParty;
            if (camp != null)
            {
                InventoryScreenHelper.OpenScreenAsInventoryOf(PartyBase.MainParty, camp.Party);
            }
        }

        /// <summary>Opens the troop-donation screen; donated troops join the
        /// camp roster and fight in its defense.</summary>
        private static void DonateTroops()
        {
            MobileParty camp = PlayerEncounter.EncounteredParty?.MobileParty;
            if (camp != null)
            {
                PartyScreenHelper.OpenScreenAsDonateTroops(camp);
            }
        }

        private static void OpenCampConversation()
        {
            PartyBase party = PlayerEncounter.EncounteredParty;
            if (party == null)
            {
                return;
            }

            PlayerEncounter.SetMeetingDone();
            Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
            CharacterObject partner = ConversationHelper.GetConversationCharacterPartyLeader(party);
            if (partner == null)
            {
                return;
            }

            CampaignMapConversation.OpenConversation(
                new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true),
                new ConversationCharacterData(partner, party, noHorse: true));
        }

        private static bool IsSettlerPartyConversation()
        {
            if (MobileParty.ConversationParty?.PartyComponent is not SettlerPartyComponent component)
            {
                return false;
            }

            MBTextManager.SetTextVariable("SETTLER_KINGDOM", component.Kingdom?.Name ?? new TextObject("{=rf_settler_unknown}Wandering"));
            return true;
        }

        private static bool IsSettlerCampConversation()
        {
            if (MobileParty.ConversationParty?.PartyComponent is not SettlerCampComponent component)
            {
                return false;
            }

            MBTextManager.SetTextVariable("SETTLER_KINGDOM", component.Kingdom?.Name ?? new TextObject("{=rf_settler_unknown}Wandering"));
            MBTextManager.SetTextVariable("CAMP_POPULATION", (int)component.Population);
            return true;
        }
    }
}
