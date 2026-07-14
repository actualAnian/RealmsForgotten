using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Helpers;
using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RF_Settlers
{
    /// <summary>
    /// Turns a mature settler camp into a real village. The LIVE path (the
    /// Player Settlement recipe) creates the settlement mid-session — the
    /// player sees the village appear where the camp stood, no reload needed:
    /// XML injected under a temporary NewCampaign loading type, visual created
    /// through SettlementVisualManager (which routes into our MapScene prefab
    /// patch), the OnGameCreated/AfterInitialized/OnFinishLoadState triple run
    /// manually, and the nameplate pushed into the live nameplates VM. If any
    /// precondition is missing (e.g. the map view is not around), the record
    /// simply stays pending: it retries on the next daily tick and, as a last
    /// resort, materializes on the next session load (FinalizePendingVillages),
    /// where the engine's own load pipeline does the visual work.
    /// </summary>
    public static class SettlersVillageFounder
    {
        private const int StartingVillageGold = 3000;
        private const float StartingMilitia = 15f;
        private const int StartingTradeTax = 100;

        // ── Live founding (mid-session, no reload) ───────────────────────────

        public static bool TryFoundVillageLive(SettlerVillageRecord record)
        {
            if (record == null || record.Established || string.IsNullOrEmpty(record.SettlementXml))
            {
                return false;
            }

            try
            {
                if (Campaign.Current == null || MBObjectManager.Instance == null
                    || MapScreen.Instance == null || SettlementVisualManager.Current == null)
                {
                    return false;
                }

                Patches.SettlersMapScenePatch.RegisterVillagePrefab(record.StringId, record.PrefabId, record.VillageTypeId);

                XmlDocument document = new();
                document.LoadXml(record.SettlementXml);
                AsCampaignGameLoadingType(Campaign.GameLoadingType.NewCampaign,
                    () => MBObjectManager.Instance.LoadXml(document));

                Settlement settlement = MBObjectManager.Instance.GetObject<Settlement>(record.StringId);
                if (settlement?.Village == null)
                {
                    return false;
                }

                settlement.IsVisible = true;
                settlement.IsInspected = true;
                settlement.Party.SetLevelMaskIsDirty();
                settlement.Party.SetVisualAsDirty();
                AccessTools.Method(typeof(SettlementVisualManager), "AddNewPartyVisualForParty")
                    ?.Invoke(SettlementVisualManager.Current, new object[] { settlement.Party });

                settlement.OnGameCreated();
                settlement.AfterInitialized();
                settlement.OnFinishLoadState();

                // The level mask (which of the prefab's level_1/2/3 children
                // shows — hearth 120 → level_1, the small starting hamlet) is
                // applied on the VISUAL's tick when the dirty flag is set. The
                // earlier SetLevelMaskIsDirty ran before the visual existed;
                // set it again now that it does, or all levels render stacked.
                settlement.Party.SetLevelMaskIsDirty();

                EnsureCampaignRegistries(settlement);
                SeedVillage(settlement);
                // Mark established IMMEDIATELY after the economic seeding: if a
                // later best-effort step (nameplate/camp cleanup) throws, the
                // retry must NOT re-run SeedVillage and grant +3000 gold again.
                record.Established = true;
                DestroyCampParty(record.CampPartyId);
                TryAddNameplateLive(settlement);
                AnnounceFounded(settlement);
                return true;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Live founding failed for {record.StringId} (will retry, then fall back to next load): {exception}");
                return false;
            }
        }

        /// <summary>Camps whose live founding failed (e.g. matured while the
        /// map view was unavailable) retry once a day.</summary>
        public static void RetryPendingVillages(List<SettlerVillageRecord> records)
        {
            if (records == null)
            {
                return;
            }

            foreach (SettlerVillageRecord record in records)
            {
                if (!record.Established)
                {
                    TryFoundVillageLive(record);
                }
            }
        }

        // ── Load-path founding (fallback) ────────────────────────────────────

        /// <summary>Runs at session launch. Villages whose XML was injected in
        /// RegisterSubModuleObjects already have visuals/nameplate/navigation
        /// from the engine's load pipeline; brand-new (pending) ones only need
        /// their one-time seeding here.</summary>
        public static void FinalizePendingVillages(List<SettlerVillageRecord> records)
        {
            if (records == null)
            {
                return;
            }

            foreach (SettlerVillageRecord record in records)
            {
                if (record.Established)
                {
                    continue;
                }

                try
                {
                    FinalizeRecord(record);
                }
                catch (Exception exception)
                {
                    Debug.Print($"[RF_Settlers] Failed to finalize village {record.StringId}: {exception}");
                }
            }
        }

        private static void FinalizeRecord(SettlerVillageRecord record)
        {
            Settlement settlement = MBObjectManager.Instance?.GetObject<Settlement>(record.StringId);
            if (settlement?.Village == null)
            {
                // XML injection failed this session; the record stays pending
                // and is retried live (daily) and again on the next load.
                return;
            }

            settlement.IsVisible = true;
            settlement.IsInspected = true;
            settlement.Party.SetLevelMaskIsDirty();
            settlement.Party.SetVisualAsDirty();

            // On the load path the engine builds these registries itself after
            // our early XML injection; the call below only fills whatever is
            // missing (every step checks before adding).
            EnsureCampaignRegistries(settlement);
            SeedVillage(settlement);
            // Mark established right after seeding so a failure below never
            // re-grants the village's starting gold on the next load.
            record.Established = true;
            DestroyCampParty(record.CampPartyId);
            AnnounceFounded(settlement);
        }

        // ── Shared pieces ────────────────────────────────────────────────────

        /// <summary>
        /// Full AI integration for a village born mid-timeline. Several vanilla
        /// registries are SNAPSHOTS built once at session start, so a live
        /// settlement stays invisible to the systems that iterate them:
        /// Campaign._villages (behind Village.All — trade/AI scans), the owner
        /// clan's settlement caches (Clan/Kingdom.Settlements — recruitment
        /// visits, raid target picks) and the hourly/daily settlement TICKERS
        /// (village production, hearth growth, volunteer refresh). Everything
        /// dynamic (Campaign.Settlements, distance cache — lazy with pathfind
        /// fallback) already works. Each step checks membership first, so the
        /// load path (where the engine builds these itself) is a no-op.
        /// </summary>
        private static void EnsureCampaignRegistries(Settlement settlement)
        {
            try
            {
                Village village = settlement.Village;
                if (AccessTools.Field(typeof(Campaign), "_villages")?.GetValue(Campaign.Current) is MBList<Village> villages
                    && !villages.Contains(village))
                {
                    villages.Add(village);
                }

                Clan ownerClan = settlement.OwnerClan;
                if (ownerClan != null && !ownerClan.Settlements.Contains(settlement))
                {
                    // Internal vanilla hook: fills Clan._villagesCache and
                    // _settlementsCache and propagates to the kingdom's lists.
                    AccessTools.Method(typeof(Clan), "OnBoundVillageAdded")
                        ?.Invoke(ownerClan, new object[] { village });
                }

                object tickManager = AccessTools.Field(typeof(Campaign), "_campaignPeriodicEventManager")
                    ?.GetValue(Campaign.Current);
                if (tickManager != null)
                {
                    AddToSettlementTicker(tickManager, "_hourlyTickSettlementTicker", settlement);
                    AddToSettlementTicker(tickManager, "_dailyTickSettlementTicker", settlement);
                }
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Campaign registry integration failed for {settlement.StringId}: {exception}");
            }
        }

        private static void AddToSettlementTicker(object tickManager, string tickerFieldName, Settlement settlement)
        {
            object ticker = AccessTools.Field(tickManager.GetType(), tickerFieldName)?.GetValue(tickManager);
            if (ticker == null)
            {
                return;
            }

            // Both tickers share the same shuffled snapshot list; the Contains
            // check makes the second call a no-op.
            if (AccessTools.Field(ticker.GetType(), "_list")?.GetValue(ticker) is MBList<Settlement> list
                && !list.Contains(settlement))
            {
                list.Add(settlement);
            }
        }

        /// <summary>One-time economy/people seeding: vanilla only wires these
        /// on daily ticks or game creation, so a village born mid-timeline
        /// needs them pushed once (reflection where members are private — the
        /// same accessors the Player Settlement mod uses).</summary>
        private static void SeedVillage(Settlement settlement)
        {
            Village village = settlement.Village;
            village.ChangeGold(StartingVillageGold);
            settlement.Militia = StartingMilitia;
            village.TradeTaxAccumulated = StartingTradeTax;

            CreateNotables(settlement);
            AssignTradeBound(village);
            NotifyVanillaBehaviors(settlement);
        }

        private static void AnnounceFounded(Settlement settlement)
        {
            // Name the nearest existing settlement so the player can locate
            // the new village icon on the map.
            Settlement nearest = SettlementHelper.FindNearestSettlementToPoint(
                settlement.Position,
                s => s != settlement && (s.IsTown || s.IsCastle || s.IsVillage));
            TextObject message = new("{=rf_settlers_founded}The settlers have founded the village of {VILLAGE}, near {NEAREST}.");
            message.SetTextVariable("VILLAGE", settlement.Name);
            message.SetTextVariable("NEAREST", nearest?.Name ?? new TextObject("{=rf_settlers_unknown_place}uncharted lands"));
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFF9BC8A0u)));
        }

        private static void AsCampaignGameLoadingType(Campaign.GameLoadingType loadingType, Action action)
        {
            FieldInfo field = AccessTools.Field(typeof(Campaign), "_gameLoadingType");
            object original = field.GetValue(Campaign.Current);
            try
            {
                field.SetValue(Campaign.Current, loadingType);
                action();
            }
            finally
            {
                field.SetValue(Campaign.Current, original);
            }
        }

        /// <summary>The nameplates VM is built once at map-screen creation, so
        /// a settlement born mid-session must be pushed into it by hand. Best
        /// effort: on failure the plate simply appears on the next load, while
        /// the village itself is already fully functional.</summary>
        private static void TryAddNameplateLive(Settlement settlement)
        {
            try
            {
                GauntletMapSettlementNameplateView view = MapScreen.Instance?.GetMapView<GauntletMapSettlementNameplateView>();
                if (view == null)
                {
                    return;
                }

                if (AccessTools.Field(typeof(GauntletMapSettlementNameplateView), "_dataSource")
                        ?.GetValue(view) is not SettlementNameplatesVM nameplates)
                {
                    return;
                }

                GameEntity strategicEntity = SettlementVisualManager.Current.GetSettlementVisual(settlement)?.StrategicEntity;
                if (strategicEntity == null)
                {
                    return;
                }

                SettlementNameplateVM nameplate = new(
                    settlement, strategicEntity,
                    MapScreen.Instance.MapCameraView.Camera,
                    MapScreen.Instance.FastMoveCameraToPosition);
                AccessTools.Method(typeof(SettlementNameplatesVM), "AddNameplate")
                    ?.Invoke(nameplates, new object[] { nameplate });
                nameplate.RefreshDynamicProperties(forceUpdate: true);
                nameplate.RefreshRelationStatus();
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Live nameplate injection failed (plate appears on next load): {exception}");
            }
        }

        private static void CreateNotables(Settlement settlement)
        {
            if (settlement.Notables != null && settlement.Notables.Count > 0)
            {
                return;
            }

            int headmen = Campaign.Current.Models.NotableSpawnModel
                .GetTargetNotableCountForSettlement(settlement, Occupation.Headman);
            for (int i = 0; i < headmen; i++)
            {
                HeroCreator.CreateNotable(Occupation.Headman, settlement);
            }

            int ruralNotables = Campaign.Current.Models.NotableSpawnModel
                .GetTargetNotableCountForSettlement(settlement, Occupation.RuralNotable);
            for (int i = 0; i < ruralNotables; i++)
            {
                HeroCreator.CreateNotable(Occupation.RuralNotable, settlement);
            }
        }

        private static void AssignTradeBound(Village village)
        {
            Settlement tradeTown = SettlementHelper.FindNearestSettlementToSettlement(
                village.Settlement, MobileParty.NavigationType.Default,
                s => s.IsTown && s.Town.MapFaction == village.Settlement.MapFaction);
            if (tradeTown == null)
            {
                tradeTown = SettlementHelper.FindNearestSettlementToSettlement(
                    village.Settlement, MobileParty.NavigationType.Default,
                    s => s.IsTown && !s.Town.MapFaction.IsAtWarWith(village.Settlement.MapFaction));
            }

            if (tradeTown != null)
            {
                AccessTools.Property(typeof(Village), "TradeBound")?.SetValue(village, tradeTown);
            }
        }

        private static void NotifyVanillaBehaviors(Settlement settlement)
        {
            VillageGoodProductionCampaignBehavior production =
                Campaign.Current.GetCampaignBehavior<VillageGoodProductionCampaignBehavior>();
            if (production != null)
            {
                AccessTools.Method(typeof(VillageGoodProductionCampaignBehavior), "TickProductions")
                    ?.Invoke(production, new object[] { settlement, true });
            }

            RecruitmentCampaignBehavior recruitment =
                Campaign.Current.GetCampaignBehavior<RecruitmentCampaignBehavior>();
            if (recruitment != null)
            {
                AccessTools.Method(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")
                    ?.Invoke(recruitment, new object[] { settlement });
            }
        }

        private static void DestroyCampParty(string campPartyId)
        {
            if (string.IsNullOrEmpty(campPartyId))
            {
                return;
            }

            foreach (MobileParty party in MobileParty.All)
            {
                if (party?.StringId == campPartyId && party.IsActive)
                {
                    // Planned lifecycle removal — not a "wiped out" report.
                    SettlersCampaignBehavior.PlannedRemovals.Add(party.StringId);
                    DestroyPartyAction.Apply(null, party);
                    return;
                }
            }
        }
    }
}
