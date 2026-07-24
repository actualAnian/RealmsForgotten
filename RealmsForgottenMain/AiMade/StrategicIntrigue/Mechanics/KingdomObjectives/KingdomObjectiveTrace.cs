using System;
using System.IO;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.KingdomObjectives
{
    /// <summary>
    /// Diagnostic trail for the grand-design → war bridge. One line per realm
    /// per day: the design's progress/pressure, the target it picked, and — the
    /// point of the whole thing — WHICH gate stopped it from pressing for war.
    ///
    /// Reported symptom this exists for: "the Wulfhart never go to war" even
    /// though MartialGlory is the most aggressive design in the game on paper.
    /// Rather than guess which of the five gates starves it, log them.
    ///
    /// Off unless the player enables it (RF World Evolution → Kingdom Objective
    /// telemetry). Writes to Configs/ModLogs/RF_KingdomObjectives.log,
    /// best-effort: any IO failure is swallowed.
    /// </summary>
    public static class KingdomObjectiveTrace
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_KingdomObjectives.log");

        public static bool Enabled => RealmsForgotten.Diagnostics.RFLogSwitchboard.IsEnabled(
            RealmsForgotten.Diagnostics.RFLogSwitchboard.KingdomObjectives);

        public static void Write(
            Kingdom kingdom,
            KingdomIntrigueState state,
            Kingdom target,
            float conviction,
            bool pressForWar,
            string blockedBy)
        {
            if (!Enabled || kingdom == null || state == null)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string line = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "day={0} kingdom={1} design={2} progress={3:0.0} pressure={4:0.0} momentum={5:0.0} warScore={6:0.0} exhaustion={7:0.0} target={8} conviction={9:0.00} press={10} blocked={11}{12}",
                    (int)CampaignTime.Now.ToDays,
                    kingdom.StringId,
                    state.ObjectiveType,
                    state.ObjectiveProgress,
                    state.ObjectivePressure,
                    state.ObjectiveMomentum,
                    state.ObjectiveWarScore,
                    state.WarExhaustion,
                    target?.StringId ?? "none",
                    conviction,
                    pressForWar,
                    blockedBy ?? "-",
                    Environment.NewLine);

                RealmsForgotten.Diagnostics.RFLogSwitchboard.Append(
                    RealmsForgotten.Diagnostics.RFLogSwitchboard.KingdomObjectives, LogPath, line);
            }
            catch
            {
                // telemetry is never allowed to break a campaign tick
            }
        }
    }
}
