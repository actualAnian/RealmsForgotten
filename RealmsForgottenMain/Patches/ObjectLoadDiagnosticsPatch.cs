using HarmonyLib;
using RealmsForgotten.AiMade;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Patches
{
    internal static class MBEquipmentRosterAddDiagnosticsPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MBEquipmentRoster), "AddEquipmentRoster", new[] { typeof(MBEquipmentRoster), typeof(Equipment.EquipmentType) });
        }

        private static Exception? Finalizer(Exception? __exception, MBEquipmentRoster __instance, MBEquipmentRoster equipmentRoster, Equipment.EquipmentType equipmentType)
        {
            if (__exception == null)
            {
                return null;
            }

            string targetId = SafeId(__instance);
            string sourceId = SafeId(equipmentRoster);
            string message =
                $"[RF EquipRoster.Add] {__exception.GetType().FullName}: {__exception.Message} | " +
                $"target={targetId} | source={sourceId} | equipmentType={equipmentType} | " +
                $"targetNull={(__instance == null)} | sourceNull={(equipmentRoster == null)} | " +
                $"targetSummary={SummarizeRoster(__instance)} | sourceSummary={SummarizeRoster(equipmentRoster)}";

            try
            {
                RFLogger.Log(message);
            }
            catch
            {
                // Fall through to direct logging below.
            }

            ObjectLoadDiagnosticsFileSink.SafeWriteDiagnostic(message);
            Debug.WriteLine(message);

            return new InvalidOperationException(message, __exception);
        }

        private static string SafeId(MBEquipmentRoster? roster)
        {
            if (roster == null)
            {
                return "<null>";
            }

            try
            {
                return roster.StringId ?? "<null-id>";
            }
            catch
            {
                return "<unreadable>";
            }
        }

        private static string SummarizeRoster(MBEquipmentRoster? roster)
        {
            if (roster == null)
            {
                return "<null>";
            }

            try
            {
                FieldInfo? field = typeof(MBEquipmentRoster).GetField("_equipments", BindingFlags.Instance | BindingFlags.NonPublic);
                object? raw = field?.GetValue(roster);
                if (raw is not IEnumerable enumerable)
                {
                    return "equipments=<unreadable>";
                }

                int count = 0;
                int battle = 0;
                int civilian = 0;
                int stealth = 0;
                int nullItems = 0;

                foreach (object? item in enumerable)
                {
                    count++;
                    if (item == null)
                    {
                        nullItems++;
                        continue;
                    }

                    if (item is Equipment eq)
                    {
                        if (eq.IsBattle)
                        {
                            battle++;
                        }

                        if (eq.IsCivilian)
                        {
                            civilian++;
                        }

                        if (eq.IsStealth)
                        {
                            stealth++;
                        }
                    }
                }

                return $"count={count},battle={battle},civilian={civilian},stealth={stealth},nullItems={nullItems}";
            }
            catch (Exception ex)
            {
                return $"<summary-failed:{ex.GetType().Name}>";
            }
        }
    }

    internal static class MBObjectManagerLoadXmlDiagnosticsPatch
    {
        private static MethodBase? TargetMethod()
        {
            Type? t = AccessTools.TypeByName("TaleWorlds.Core.MBObjectManagerExtensions");
            if (t == null)
            {
                return null;
            }

            return AccessTools.Method(t, "LoadXML", new[] { typeof(MBObjectManager), typeof(string), typeof(bool), typeof(string), typeof(bool) });
        }

        private static Exception? Finalizer(Exception? __exception, string id, bool isDevelopment, string gameType, bool skipXmlFilterForEditor)
        {
            if (__exception == null)
            {
                return null;
            }

            string message =
                $"[RF LoadXML] {__exception.GetType().FullName}: {__exception.Message} | " +
                $"xmlId={id} | isDevelopment={isDevelopment} | gameType={gameType} | skipXmlFilterForEditor={skipXmlFilterForEditor}";

            try
            {
                RFLogger.Log(message);
            }
            catch
            {
                // Fall through to direct logging below.
            }

            ObjectLoadDiagnosticsFileSink.SafeWriteDiagnostic(message);
            Debug.WriteLine(message);

            return new InvalidOperationException(message, __exception);
        }
    }

    internal static class ObjectLoadDiagnosticsFileSink
    {
        internal static void SafeWriteDiagnostic(string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";

            foreach (string path in GetCandidatePaths())
            {
                try
                {
                    string? dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.AppendAllText(path, line);
                }
                catch
                {
                }
            }
        }

        private static string[] GetCandidatePaths()
        {
            string docs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord",
                "Configs",
                "ModLogs",
                "RF_ObjectLoadDiagnostics.log");

            string temp = Path.Combine(
                Path.GetTempPath(),
                "RF_ObjectLoadDiagnostics.log");

            string moduleDir;
            try
            {
                moduleDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            }
            catch
            {
                moduleDir = AppDomain.CurrentDomain.BaseDirectory;
            }

            string module = Path.Combine(moduleDir, "RF_ObjectLoadDiagnostics.log");
            return new[] { docs, temp, module };
        }
    }
}
