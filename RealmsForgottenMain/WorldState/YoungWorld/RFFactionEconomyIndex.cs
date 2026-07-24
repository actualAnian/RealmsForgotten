using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.WorldState.YoungWorld
{
    /// <summary>
    /// Faction Economy Index — PHASE 1: measurement and telemetry only.
    ///
    /// Once a day it estimates each kingdom's economic strength from the resource
    /// zones its clans hold (tier × richness of active deposits, plus recent
    /// caravan flow, minus exhausted/idle deposits), normalises the raw scores to
    /// 0–100 across the living kingdoms, stores them, and — when the telemetry
    /// toggle is on — writes one line per kingdom per day to a log file. It has NO
    /// gameplay effect on its own; the optional ±1 tier adjustment reads the index
    /// through <see cref="GetIef"/>, which returns 50 (neutral) whenever a kingdom
    /// has no measurement yet.
    ///
    /// RealmsForgottenMain does not reference RF_ResourceZones, so the zone data is
    /// read through null-safe reflection (same idiom as Homesteads'
    /// HomesteadWorldTiesBehavior): if the module is absent every kingdom simply
    /// scores neutral and nothing breaks.
    /// </summary>
    public class RFFactionEconomyIndex : CampaignBehaviorBase
    {
        private const int NeutralIef = 50;
        private const float CaravanWindowDays = 20f;

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_IEF.log");

        private static RFFactionEconomyIndex? _instance;

        private Dictionary<string, int> _iefByKingdom = new(StringComparer.Ordinal);

        public RFFactionEconomyIndex()
        {
            _instance = this;
        }

        /// <summary>
        /// Latest measured index (0–100) for a kingdom, or 50 (neutral) if it has
        /// not been measured yet or the system is inactive. This is the only
        /// public read surface — the ±1 tier adjustment consumes it.
        /// </summary>
        public static int GetIef(Kingdom kingdom)
        {
            if (_instance == null || kingdom == null)
            {
                return NeutralIef;
            }

            string key = GetKingdomKey(kingdom);
            return _instance._iefByKingdom.TryGetValue(key, out int value) ? value : NeutralIef;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_rfIefByKingdom", ref _iefByKingdom);
            _iefByKingdom ??= new Dictionary<string, int>(StringComparer.Ordinal);
        }

        private void OnDailyTick()
        {
            // Skip all work unless something actually consumes the index — the
            // telemetry log, or the tier-cap ±1 adjustment.
            if (!RFWorldSettings.IefTelemetry && !RFWorldSettings.IefMilitary)
            {
                return;
            }

            List<ZoneSnapshot> zones = ReadZones();

            // Aggregate per kingdom.
            var raw = new Dictionary<string, KingdomEconomy>(StringComparer.Ordinal);
            float now = (float)CampaignTime.Now.ToDays;
            foreach (ZoneSnapshot zone in zones)
            {
                Kingdom? kingdom = zone.Owner?.Kingdom;
                if (kingdom == null)
                {
                    continue;
                }

                string key = GetKingdomKey(kingdom);
                if (!raw.TryGetValue(key, out KingdomEconomy economy))
                {
                    economy = new KingdomEconomy();
                }

                int richness = zone.Richness > 0 ? zone.Richness : 1;
                economy.OwnedZones++;
                if (zone.Producing)
                {
                    economy.ZoneScore += zone.Tier * richness;
                    economy.ActiveZones++;
                    if (now - zone.LastCaravanDay <= CaravanWindowDays && zone.LastCaravanDay > 0f)
                    {
                        economy.CaravanFlow += zone.Tier;
                    }
                }
                else
                {
                    economy.ExhaustedPenalty += zone.Tier;
                }

                raw[key] = economy;
            }

            // Raw score and the day's maximum, for normalisation.
            var rawScore = new Dictionary<string, float>(StringComparer.Ordinal);
            float maxRaw = 0f;
            foreach (KeyValuePair<string, KingdomEconomy> pair in raw)
            {
                float score = Math.Max(0f, pair.Value.ZoneScore + pair.Value.CaravanFlow - pair.Value.ExhaustedPenalty);
                rawScore[pair.Key] = score;
                if (score > maxRaw)
                {
                    maxRaw = score;
                }
            }

            // Normalise and store. With no economic signal at all, everyone is
            // neutral rather than zero (avoids a spurious global penalty).
            _iefByKingdom.Clear();
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == null || kingdom.IsEliminated)
                {
                    continue;
                }

                string key = GetKingdomKey(kingdom);
                int ief;
                if (maxRaw <= 0f)
                {
                    ief = NeutralIef;
                }
                else
                {
                    float score = rawScore.TryGetValue(key, out float s) ? s : 0f;
                    ief = (int)Math.Round(score / maxRaw * 100f);
                    if (ief < 0) ief = 0;
                    else if (ief > 100) ief = 100;
                }

                _iefByKingdom[key] = ief;

                if (RFWorldSettings.IefTelemetry)
                {
                    raw.TryGetValue(key, out KingdomEconomy economy);
                    WriteTelemetry(now, kingdom, ief, rawScore.TryGetValue(key, out float rs) ? rs : 0f, economy);
                }
            }
        }

        // ── Telemetry ────────────────────────────────────────────────────────
        private static void WriteTelemetry(float day, Kingdom kingdom, int ief, float rawScore, KingdomEconomy economy)
        {
            // CaravanFlow is a proxy: LastCaravanDay only records the MOST RECENT
            // delivery, so a zone counts at most once per 20-day window (true
            // per-window delivery counts are not exposed by RF_ResourceZones).
            string line = string.Join("|", new[]
            {
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                ((int)day).ToString(CultureInfo.InvariantCulture),
                kingdom.StringId,
                kingdom.Name?.ToString() ?? string.Empty,
                ief.ToString(CultureInfo.InvariantCulture),
                rawScore.ToString("0.##", CultureInfo.InvariantCulture),
                economy.ZoneScore.ToString("0.##", CultureInfo.InvariantCulture),
                economy.CaravanFlow.ToString("0.##", CultureInfo.InvariantCulture) + "(approx)",
                economy.ExhaustedPenalty.ToString("0.##", CultureInfo.InvariantCulture),
                economy.OwnedZones.ToString(CultureInfo.InvariantCulture),
                economy.ActiveZones.ToString(CultureInfo.InvariantCulture),
            }) + Environment.NewLine;

            try
            {
                string? directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                RealmsForgotten.Diagnostics.RFLogSwitchboard.Append(
                    RealmsForgotten.Diagnostics.RFLogSwitchboard.FactionEconomy, LogPath, line);
            }
            catch
            {
                // Telemetry is best-effort only — never let logging break a tick.
            }
        }

        // ── RF_ResourceZones reflection bridge (null-safe) ───────────────────
        private static bool _zonesResolved;
        private static PropertyInfo? _instanceProp;
        private static FieldInfo? _recordsField;
        private static FieldInfo? _ownerField;
        private static FieldInfo? _tierField;
        private static FieldInfo? _richnessField;
        private static FieldInfo? _exhaustedField;
        private static FieldInfo? _idleField;
        private static FieldInfo? _lastCaravanField;

        private List<ZoneSnapshot> ReadZones()
        {
            var result = new List<ZoneSnapshot>();
            ResolveZonesApi();

            object? behavior = _instanceProp?.GetValue(null);
            if (behavior == null || _recordsField == null)
            {
                return result;
            }

            try
            {
                if (_recordsField.GetValue(behavior) is not IEnumerable records)
                {
                    return result;
                }

                foreach (object? record in records)
                {
                    if (record == null)
                    {
                        continue;
                    }

                    // Field infos are resolved lazily off the first real record.
                    ResolveRecordFields(record.GetType());
                    if (_ownerField == null || _tierField == null)
                    {
                        break;
                    }

                    var snapshot = new ZoneSnapshot
                    {
                        Owner = _ownerField.GetValue(record) as Clan,
                        Tier = _tierField.GetValue(record) as int? ?? 1,
                        Richness = _richnessField?.GetValue(record) as int? ?? 0,
                        LastCaravanDay = _lastCaravanField?.GetValue(record) as float? ?? -1000f,
                    };

                    float exhaustedUntil = _exhaustedField?.GetValue(record) as float? ?? 0f;
                    float idleUntil = _idleField?.GetValue(record) as float? ?? 0f;
                    float now = (float)CampaignTime.Now.ToDays;
                    snapshot.Producing = exhaustedUntil <= now && idleUntil <= now;

                    result.Add(snapshot);
                }
            }
            catch
            {
                // A shape mismatch disables the bridge for this call rather than
                // ever breaking the tick.
            }

            return result;
        }

        private static void ResolveZonesApi()
        {
            if (_zonesResolved)
            {
                return;
            }

            _zonesResolved = true;
            try
            {
                Type? type = FindLoadedType("ResourceZonesCampaignBehavior", "RF_ResourceZones");
                if (type != null)
                {
                    _instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    _recordsField = type.GetField("_records", BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }
            catch
            {
                // Leave everything null — ReadZones degrades to empty.
            }
        }

        private static void ResolveRecordFields(Type recordType)
        {
            if (_ownerField != null)
            {
                return;
            }

            _ownerField = recordType.GetField("OwnerClan", BindingFlags.Public | BindingFlags.Instance);
            _tierField = recordType.GetField("Tier", BindingFlags.Public | BindingFlags.Instance);
            _richnessField = recordType.GetField("Richness", BindingFlags.Public | BindingFlags.Instance);
            _exhaustedField = recordType.GetField("ExhaustedUntilDay", BindingFlags.Public | BindingFlags.Instance);
            _idleField = recordType.GetField("IdleUntilDay", BindingFlags.Public | BindingFlags.Instance);
            _lastCaravanField = recordType.GetField("LastCaravanDay", BindingFlags.Public | BindingFlags.Instance);
        }

        private static Type? FindLoadedType(string simpleName, string assemblyHint)
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string name = assembly.GetName().Name ?? string.Empty;
                    if (!name.StartsWith(assemblyHint, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        foreach (Type type in assembly.GetTypes())
                        {
                            if (type.Name == simpleName)
                            {
                                return type;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore a single unreadable assembly.
                    }
                }
            }
            catch
            {
                // Ignore — no bridge.
            }

            return null;
        }

        private static string GetKingdomKey(Kingdom kingdom)
        {
            return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
        }

        private struct ZoneSnapshot
        {
            public Clan? Owner;
            public int Tier;
            public int Richness;
            public float LastCaravanDay;
            public bool Producing;
        }

        private struct KingdomEconomy
        {
            public float ZoneScore;
            public float CaravanFlow;
            public float ExhaustedPenalty;
            public int OwnedZones;
            public int ActiveZones;
        }
    }
}
