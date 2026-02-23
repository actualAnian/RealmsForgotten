using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade
{
    public static class RFMissionBehaviorTickProbe
    {
        private static bool _patched;
        private static readonly HashSet<Type> _patchedTypes = new();

        // Throttle
        private static readonly Dictionary<string, int> _enterCount = new();
        private static readonly Dictionary<string, long> _lastLogMs = new();

        public static void PatchMissionBehaviorsFor(Mission mission)
        {
            if (mission == null) return;

            // Patch only once globally
            if (!_patched)
            {
                _patched = true;
                RFLogger.Log("[TickProbe] Initialized");
            }

            var h = new Harmony("realmsforgotten.tickprobe");

            // Snapshot of behaviors currently on mission
            var behaviors = mission.MissionBehaviors?.ToList();
            if (behaviors == null || behaviors.Count == 0)
            {
                RFLogger.Log("[TickProbe] No mission behaviors found.");
                return;
            }

            foreach (var b in behaviors)
            {
                if (b == null) continue;
                var t = b.GetType();
                if (_patchedTypes.Contains(t)) continue;

                // Patch OnMissionTick(float)
                var onTick = t.GetMethod("OnMissionTick",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(float) },
                    null);

                if (onTick != null)
                {
                    h.Patch(onTick,
                        prefix: new HarmonyMethod(typeof(RFMissionBehaviorTickProbe).GetMethod(nameof(OnTickPrefix), BindingFlags.Static | BindingFlags.NonPublic)),
                        postfix: new HarmonyMethod(typeof(RFMissionBehaviorTickProbe).GetMethod(nameof(OnTickPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                }

                // Patch AfterStart()
                var afterStart = t.GetMethod("AfterStart",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);

                if (afterStart != null)
                {
                    h.Patch(afterStart,
                        prefix: new HarmonyMethod(typeof(RFMissionBehaviorTickProbe).GetMethod(nameof(AfterStartPrefix), BindingFlags.Static | BindingFlags.NonPublic)),
                        postfix: new HarmonyMethod(typeof(RFMissionBehaviorTickProbe).GetMethod(nameof(AfterStartPostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                }

                _patchedTypes.Add(t);
                RFLogger.Log("[TickProbe] Patched: " + t.FullName);
            }
        }

        private static bool ShouldLog(string key, int everyN = 120, long minIntervalMs = 1000)
        {
            int c = _enterCount.TryGetValue(key, out var v) ? v : 0;
            c++;
            _enterCount[key] = c;

            long now = Stopwatch.GetTimestamp();
            long freq = Stopwatch.Frequency;
            long nowMs = (now * 1000) / freq;

            long last = _lastLogMs.TryGetValue(key, out var lm) ? lm : 0;
            bool okInterval = (nowMs - last) >= minIntervalMs;

            // log every N calls OR if enough time passed (to catch hangs)
            if ((c % everyN) == 1 || okInterval)
            {
                _lastLogMs[key] = nowMs;
                return true;
            }
            return false;
        }

        // ---- AfterStart ----
        private static void AfterStartPrefix(object __instance)
        {
            string key = __instance.GetType().FullName + ".AfterStart";
            if (ShouldLog(key, everyN: 1, minIntervalMs: 0))
                RFLogger.Log("[TickProbe] ENTER " + key);
        }

        private static void AfterStartPostfix(object __instance)
        {
            string key = __instance.GetType().FullName + ".AfterStart";
            RFLogger.Log("[TickProbe] EXIT  " + key);
        }

        // ---- OnMissionTick ----
        private static void OnTickPrefix(object __instance, float dt, out long __state)
        {
            __state = Stopwatch.GetTimestamp();
            string key = __instance.GetType().FullName + ".OnMissionTick";

            // Não spammar: mas mantém sinal suficiente pra achar o último antes do hang
            if (ShouldLog(key, everyN: 120, minIntervalMs: 1500))
                RFLogger.Log("[TickProbe] ENTER " + key + " dt=" + dt.ToString("0.000"));
        }

        private static void OnTickPostfix(object __instance, float dt, long __state)
        {
            long end = Stopwatch.GetTimestamp();
            double ms = (end - __state) * 1000.0 / Stopwatch.Frequency;

            // Só loga EXIT quando ficou "caro"
            if (ms >= 25.0)
            {
                string key = __instance.GetType().FullName + ".OnMissionTick";
                RFLogger.Log("[TickProbe] EXIT  " + key + " ms=" + ms.ToString("0.0"));
            }
        }
    }
}
