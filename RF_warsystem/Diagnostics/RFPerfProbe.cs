using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RF_warsystem.Diagnostics
{
    /// <summary>
    /// Lightweight per-tick stopwatch. Wrap any suspect method in
    /// <c>using (RFPerfProbe.Measure("Name")) { ... }</c>; when enabled it
    /// accumulates the time spent under each name and, once per in-game day,
    /// dumps a sorted "who ate the frame" line to RF_Perf.log. Off unless the
    /// RF Diagnostics MCM page turns it on (RuntimeEnabled pushed like the trace
    /// log), so a normal session pays nothing.
    ///
    /// Lives in RF_warsystem because both the war behaviors here AND
    /// RealmsForgotten (which references this project) need it — the 10 war
    /// daily-ticks and the strategic-intrigue tick are the freeze suspects.
    /// </summary>
    public static class RFPerfProbe
    {
        public static bool RuntimeEnabled;

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_Perf.log");

        private static readonly Dictionary<string, double> TotalMs = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, double> WorstMs = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> Calls = new(StringComparer.Ordinal);

        private readonly struct Scope : IDisposable
        {
            private readonly string _name;
            private readonly long _startTicks;

            public Scope(string name)
            {
                _name = name;
                _startTicks = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                double ms = (Stopwatch.GetTimestamp() - _startTicks) * 1000.0 / Stopwatch.Frequency;
                TotalMs.TryGetValue(_name, out double total);
                TotalMs[_name] = total + ms;
                WorstMs.TryGetValue(_name, out double worst);
                if (ms > worst)
                {
                    WorstMs[_name] = ms;
                }
                Calls.TryGetValue(_name, out int calls);
                Calls[_name] = calls + 1;
            }
        }

        /// <summary>Returns a disposable timing scope, or a no-op when disabled.</summary>
        public static IDisposable Measure(string name)
        {
            return RuntimeEnabled ? new Scope(name) : NullScope.Instance;
        }

        /// <summary>Call once per in-game day (from ONE behavior) to flush the
        /// day's accumulated timings, worst-to-best, then reset.</summary>
        public static void FlushDaily(int day)
        {
            if (!RuntimeEnabled || TotalMs.Count == 0)
            {
                return;
            }

            try
            {
                var names = new List<string>(TotalMs.Keys);
                names.Sort((a, b) => TotalMs[b].CompareTo(TotalMs[a]));

                double grandTotal = 0.0;
                var lines = new System.Text.StringBuilder();
                lines.Append("day=").Append(day).Append(" | ");
                foreach (string name in names)
                {
                    double total = TotalMs[name];
                    grandTotal += total;
                    lines.Append(name).Append('=')
                        .Append(total.ToString("F1")).Append("ms")
                        .Append("(worst ").Append(WorstMs[name].ToString("F1"))
                        .Append(", n=").Append(Calls[name]).Append(") ");
                }
                lines.Append("| TOTAL=").Append(grandTotal.ToString("F1")).Append("ms");
                lines.Append(Environment.NewLine);

                string directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.AppendAllText(LogPath, lines.ToString());
            }
            catch
            {
                // profiling must never break a tick
            }
            finally
            {
                TotalMs.Clear();
                WorstMs.Clear();
                Calls.Clear();
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }
}
