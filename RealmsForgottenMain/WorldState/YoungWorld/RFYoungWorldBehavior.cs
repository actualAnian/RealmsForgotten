using System;
using System.Collections.Generic;
using System.IO;
using RF_warsystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.WorldState.YoungWorld
{
    /// <summary>
    /// World-state system: "Young World". Applied ONCE, at the creation of a new
    /// campaign and only while the MCM toggle is on, it rolls the map back to a
    /// rough founding age — fortifications start at wall level 1 (one capital per
    /// kingdom at level 2) and prosperity/hearth begin low — so the realms grow
    /// up over play instead of starting fully developed.
    ///
    /// Everything is runtime and additive: no map XML is touched. With the toggle
    /// off the behavior does nothing and the game is identical to vanilla RF. It
    /// keys off <see cref="CampaignEvents.OnNewGameCreatedEvent"/>, which fires
    /// only for new campaigns, and a persisted <c>_youngWorldApplied</c> flag, so
    /// a loaded save is never re-processed.
    /// </summary>
    public class RFYoungWorldBehavior : CampaignBehaviorBase
    {
        // ── Young World application ──────────────────────────────────────────
        private const float ProsperityFactor = 0.4f;
        private const float TownProsperityFloor = 400f;
        private const float CastleProsperityFloor = 150f;
        private const float HearthFloor = 120f;
        private const int CapitalWallLevel = 2;
        private const int DefaultWallLevel = 1;

        // ── Mitigations (Entregável D.2): food / militia floors ──────────────
        private const int FloorPeriodDays = 50;
        private const float FoodFloor = 100f;
        private const float TownMilitiaFloor = 50f;
        private const float CastleMilitiaFloor = 25f;

        private static RFYoungWorldBehavior? _instance;

        private bool _youngWorldApplied;

        // Session-only (deliberately NOT in SyncData): set when the young world is
        // applied during THIS session's campaign creation, so the wall levels can
        // be re-asserted once after all other new-game setup has run.
        private bool _appliedThisSession;
        private HashSet<string> _capitals = new();

        public RFYoungWorldBehavior()
        {
            _instance = this;
        }

        /// <summary>
        /// True only when the Young World toggle is on AND it was actually applied
        /// to this campaign. This is what gates the mitigations and what is pushed
        /// to the war director — a normal campaign (or one created with the toggle
        /// off) never reports active even if the toggle is flipped on later.
        /// </summary>
        public static bool IsActive => _instance != null && _instance._youngWorldApplied && RFWorldSettings.YoungWorld;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_rfYoungWorldApplied", ref _youngWorldApplied);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Sync the war-director bridge every launch: on a young campaign it
            // stays calm for its first days; on any other it is forced off so the
            // calm never leaks across saves in the same session.
            RFYoungWorldWarBridge.YoungWorldActive = IsActive;

            // Vanilla's BuildDevelopmentsAtGameStart() listens to the SAME
            // OnNewGameCreatedEvent we do and rolls BuildingType.VarianceChance to
            // LevelUp() every building below level 3 — Fortifications included. If
            // its listener runs after ours it silently promotes young-world walls
            // (1 -> 2, capitals 2 -> 3), which shows up as scattered settlements
            // wearing the wrong map icon. Session launch happens after ALL new-game
            // setup, so re-assert the intended levels once, here.
            if (_appliedThisSession)
            {
                _appliedThisSession = false;
                ReassertWallLevels();
            }
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            // OnNewGameCreatedEvent only fires for brand-new campaigns, so this
            // Logged BEFORE the guard: this line is what distinguishes "the event
            // never fired" from "the toggle read false" from "it actually ran".
            RFLogger.Log($"[YoungWorld] OnNewGameCreated fired. toggle={RFWorldSettings.YoungWorld} alreadyApplied={_youngWorldApplied}");

            // never runs on a loaded save; the flag guards against any double-call.
            if (!RFWorldSettings.YoungWorld || _youngWorldApplied)
            {
                return;
            }

            ApplyYoungWorld();
        }

        private void ApplyYoungWorld()
        {
            _capitals = DetermineCapitals();

            int forts = 0;
            int missing = 0;
            int failed = 0;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null)
                {
                    continue;
                }

                // Per-settlement isolation: ONE bad settlement (a modded component
                // with a surprising shape) must not abort the pass and leave every
                // settlement after it untouched at its map-XML level.
                try
                {
                    // Vanilla-component guard: RF custom settlements (homesteads,
                    // future monasteries, settler villages) live in Settlement.All
                    // but are not real towns/castles/villages — skip them.
                    if ((settlement.IsTown || settlement.IsCastle) && settlement.Town != null)
                    {
                        forts++;
                        int level = _capitals.Contains(settlement.StringId) ? CapitalWallLevel : DefaultWallLevel;
                        int before = settlement.Town.GetWallLevel();
                        bool found = SetWallLevel(settlement, level);
                        int after = settlement.Town.GetWallLevel();
                        if (!found)
                        {
                            missing++;
                        }

                        // One line per fortification: this is what proves whether the
                        // wall actually moved, and to what.
                        RFLogger.Log($"[YoungWorld] {settlement.StringId,-22} {(settlement.IsCastle ? "castle" : "town  ")} wall {before} -> {after} (wanted {level}{(_capitals.Contains(settlement.StringId) ? ", CAPITAL" : string.Empty)}){(found ? string.Empty : " [NO FORTIFICATION BUILDING]")}");

                        float floor = settlement.IsCastle ? CastleProsperityFloor : TownProsperityFloor;
                        settlement.Town.Prosperity = MathF.Max(floor, settlement.Town.Prosperity * ProsperityFactor);

                        // The map icon (1→2→3 towers) follows the wall level natively.
                        settlement.Party?.SetVisualAsDirty();
                    }
                    else if (settlement.IsVillage && settlement.Village != null)
                    {
                        settlement.Village.Hearth = MathF.Max(HearthFloor, settlement.Village.Hearth * ProsperityFactor);
                    }
                }
                catch (Exception exception)
                {
                    failed++;
                    RFLogger.Log($"[YoungWorld] FAILED on {settlement.StringId}: {exception}");
                }
            }

            _youngWorldApplied = true;
            _appliedThisSession = true;
            RFYoungWorldWarBridge.YoungWorldActive = IsActive;
            RFLogger.Log($"[YoungWorld] Applied. fortifications={forts} capitals={_capitals.Count} missingFortificationBuilding={missing} failed={failed} capitalIds=[{string.Join(", ", _capitals)}]");

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=RF_World_YoungWorld_Applied}Young World: the realms begin in a rough age.").ToString(),
                Color.FromUint(0xFFC8B87Au)));
        }

        /// <summary>
        /// Second pass, once, after every other new-game system has run: force the
        /// intended wall level back onto each fortification and log anything that
        /// had drifted. This is what makes the young world's walls immune to
        /// vanilla's random building-variance promotions at game start.
        /// </summary>
        private void ReassertWallLevels()
        {
            int corrected = 0;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || !(settlement.IsTown || settlement.IsCastle) || settlement.Town == null)
                {
                    continue;
                }

                bool isCapital = _capitals.Contains(settlement.StringId);
                int desired = isCapital ? CapitalWallLevel : DefaultWallLevel;
                int actual = settlement.Town.GetWallLevel();
                if (actual == desired)
                {
                    continue;
                }

                if (SetWallLevel(settlement, desired))
                {
                    settlement.Party?.SetVisualAsDirty();
                    corrected++;
                    RFLogger.Log($"[YoungWorld] Wall drift corrected on {settlement.StringId}: {actual} -> {desired}{(isCapital ? " (capital)" : string.Empty)}");
                }
            }

            RFLogger.Log($"[YoungWorld] Wall re-assert complete. corrected={corrected}");
        }

        // ── Diagnostics ──────────────────────────────────────────────────────
        // Own log file: the shared RFLogger is compiled off (Enabled = false), and
        // these lines are the only way to tell "this settlement is a capital at
        // level 2 by design" apart from "something promoted its walls behind us".
        // Written once per campaign creation, best-effort.
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_YoungWorld.log");

        private static class RFLogger
        {
            internal static void Log(string message)
            {
                try
                {
                    string? directory = Path.GetDirectoryName(LogPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    RealmsForgotten.Diagnostics.RFLogSwitchboard.Append(
                        RealmsForgotten.Diagnostics.RFLogSwitchboard.YoungWorld, LogPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
                catch
                {
                    // Diagnostics must never break campaign creation.
                }
            }
        }

        /// <summary>
        /// One capital per kingdom: the town (not castle) with the highest
        /// prosperity, tie-broken by StringId so the choice is deterministic.
        /// Kingdoms with no town get no capital bump (all their forts stay level 1).
        /// </summary>
        private static HashSet<string> DetermineCapitals()
        {
            var capitals = new HashSet<string>();
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == null || kingdom.IsEliminated)
                {
                    continue;
                }

                Settlement? capital = null;
                float bestProsperity = float.MinValue;
                foreach (Town fief in kingdom.Fiefs)
                {
                    Settlement? settlement = fief?.Settlement;
                    if (settlement == null || !settlement.IsTown || settlement.Town == null)
                    {
                        continue;
                    }

                    float prosperity = settlement.Town.Prosperity;
                    if (capital == null
                        || prosperity > bestProsperity
                        || (prosperity == bestProsperity
                            && string.CompareOrdinal(settlement.StringId, capital.StringId) < 0))
                    {
                        capital = settlement;
                        bestProsperity = prosperity;
                    }
                }

                if (capital != null)
                {
                    capitals.Add(capital.StringId);
                }
            }

            return capitals;
        }

        /// <summary>
        /// Sets the settlement's wall level. Returns false when the settlement has
        /// no fortification building at all — in that case its wall level cannot be
        /// changed and it keeps whatever the map XML gave it, which is worth
        /// logging because the map icon will not match the young world.
        /// </summary>
        private static bool SetWallLevel(Settlement settlement, int level)
        {
            foreach (Building building in settlement.Town.Buildings)
            {
                bool isFortification =
                    (settlement.IsTown && building.BuildingType == DefaultBuildingTypes.SettlementFortifications)
                    || (settlement.IsCastle && building.BuildingType == DefaultBuildingTypes.CastleFortifications);
                if (isFortification)
                {
                    // GetWallLevel() reads this building's CurrentLevel directly,
                    // so this IS the wall level. The setter refreshes the level
                    // mask (tower visuals) on its own.
                    building.CurrentLevel = level;
                    return true;
                }
            }

            return false;
        }

        // ── Mitigations (Entregável D.2) ─────────────────────────────────────
        private void OnDailyTick()
        {
            if (!IsActive)
            {
                return;
            }

            // CampaignStartTime.ElapsedDaysUntilNow is the correct elapsed-days
            // source (CampaignTime.Now.ElapsedDaysUntilNow is always 0 here).
            int days = (int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow;
            if (days >= FloorPeriodDays)
            {
                return;
            }

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || !(settlement.IsTown || settlement.IsCastle) || settlement.Town == null)
                {
                    continue;
                }

                if (settlement.Town.FoodStocks < FoodFloor)
                {
                    settlement.Town.FoodStocks = FoodFloor;
                }

                float militiaFloor = settlement.IsCastle ? CastleMilitiaFloor : TownMilitiaFloor;
                if (settlement.Militia < militiaFloor)
                {
                    settlement.Militia = militiaFloor;
                }
            }
        }
    }
}
