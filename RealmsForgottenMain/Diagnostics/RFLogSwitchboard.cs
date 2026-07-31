using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace RealmsForgotten.Diagnostics
{
    /// <summary>
    /// One place that decides whether any RF diagnostic file gets written.
    ///
    /// Why: the mod grew a dozen independent log writers, several with no switch
    /// at all — a player could accumulate tens of megabytes in
    /// Documents\...\Configs\ModLogs without ever asking for diagnostics. The
    /// switchboard gives every log a key, a default of OFF, an MCM toggle
    /// (see <see cref="RFLogSettings"/>) and a size cap.
    ///
    /// Satellite modules (RF_Settlers, RF_ResourceZones, RF_Enlistment) are not
    /// referenced by RealmsForgottenMain, so their switches are PUSHED into
    /// their own static fields by reflection at session launch — the same idiom
    /// the Young World war bridge uses. Modules that ARE referenced
    /// (RF_BattleAI, RF_warsystem) are pushed directly where they expose a field.
    /// </summary>
    public static class RFLogSwitchboard
    {
        // Keys are stable strings: they name the log file, not the class that
        // writes it, so a refactor on either side cannot silently unhook a
        // toggle from its file.
        public const string CampaignAiTrace = "campaign_ai_trace";
        public const string WarSystemTrace = "war_system_trace";
        public const string KingdomObjectives = "kingdom_objectives";
        public const string FactionEconomy = "faction_economy";
        public const string YoungWorld = "young_world";
        public const string Capitulation = "capitulation";
        public const string ResourceZones = "resource_zones";
        public const string Settlers = "settlers";
        public const string Enlistment = "enlistment";
        public const string BattleAiRuntime = "battle_ai_runtime";
        public const string BattleAiMemory = "battle_ai_memory";
        public const string BattleAiTactics = "battle_ai_tactics";
        public const string BattleAiTrap = "battle_ai_trap";
        public const string Perf = "perf";

        /// <summary>Per-file ceiling. Past it the file is rotated to .1 (one
        /// generation only), so a forgotten toggle costs at most 2x this.</summary>
        private const long MaxLogBytes = 8L * 1024L * 1024L;

        private static readonly Dictionary<string, long> WriteCounters = new(StringComparer.Ordinal);

        public static bool IsEnabled(string key)
        {
            return RFLogSettings.IsLogEnabled(key);
        }

        /// <summary>
        /// Guarded append: honours the toggle, creates the folder, rotates at
        /// the size cap, and never throws. Every RF log writer should funnel
        /// here instead of calling File.AppendAllText itself.
        /// </summary>
        public static void Append(string key, string path, string line)
        {
            if (!IsEnabled(key))
            {
                return;
            }

            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Stat the file every 64 writes rather than every line.
                WriteCounters.TryGetValue(path, out long count);
                WriteCounters[path] = count + 1;
                if ((count & 63L) == 0L)
                {
                    var info = new FileInfo(path);
                    if (info.Exists && info.Length > MaxLogBytes)
                    {
                        string rolled = path + ".1";
                        if (File.Exists(rolled))
                        {
                            File.Delete(rolled);
                        }
                        File.Move(path, rolled);
                    }
                }

                File.AppendAllText(path, line);
            }
            catch
            {
                // Diagnostics must never take a campaign down.
            }
        }

        public static string GetLogPath(string fileName)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord", "Configs", "ModLogs", fileName);
        }

        /// <summary>
        /// Pushes the current switch states into the satellite modules. Called on
        /// session launch and whenever the settings change, so a mid-session
        /// toggle takes effect without a restart.
        /// </summary>
        public static void PushToModules()
        {
            // Referenced projects: direct, no reflection.
            RF_BattleAI.BattleAILogSwitches.RuntimeTrace = IsEnabled(BattleAiRuntime);
            RF_BattleAI.BattleAILogSwitches.AdaptiveMemory = IsEnabled(BattleAiMemory);
            RF_BattleAI.BattleAILogSwitches.Tactics = IsEnabled(BattleAiTactics);
            RF_BattleAI.BattleAILogSwitches.BanditTrap = IsEnabled(BattleAiTrap);
            RF_warsystem.Diagnostics.RFWarSystemTraceLog.RuntimeEnabled = IsEnabled(WarSystemTrace);
            RF_warsystem.Diagnostics.RFPerfProbe.RuntimeEnabled = IsEnabled(Perf);

            PushStatic("RF_Settlers.SettlersLog, RF_Settlers", "LogEnabled", IsEnabled(Settlers));
            PushStatic("RF_ResourceZones.ResourceZonesCampaignBehavior, RF_ResourceZones", "LogEnabled", IsEnabled(ResourceZones));
            PushStatic("RF_Enlistment.RFEnlistmentDebug, RF_Enlistment", "RuntimeEnabled", IsEnabled(Enlistment));
            PushStatic("RF_Enlistment.RFCommanderDecisionDebug, RF_Enlistment", "RuntimeEnabled", IsEnabled(Enlistment));
        }

        private static void PushStatic(string qualifiedTypeName, string fieldName, bool value)
        {
            try
            {
                Type? type = Type.GetType(qualifiedTypeName) ?? FindLoadedType(qualifiedTypeName);
                FieldInfo? field = type?.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(null, value);
                }
            }
            catch
            {
                // A module that is not installed simply has no switch to push.
            }
        }

        private static Type? FindLoadedType(string qualifiedTypeName)
        {
            string[] parts = qualifiedTypeName.Split(',');
            if (parts.Length < 2)
            {
                return null;
            }
            string typeName = parts[0].Trim();
            string assemblyName = parts[1].Trim();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                try
                {
                    return assembly.GetType(typeName);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}
