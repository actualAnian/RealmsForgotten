using System;
using System.IO;
using System.Linq;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade
{
    internal static class RFCampaignAITraceLog
    {
        private static readonly string[] LogPaths =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_CampaignAITrace.log"),
            Path.Combine(BasePath.Name, "Configs", "ModLogs", "RF_CampaignAITrace.log"),
            Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", "RF_CampaignAITrace.log")
        };

        internal static void Write(string message)
        {
            // Gated by the RF Diagnostics MCM page (default OFF): this is the
            // most verbose log in the mod — it reached 10 MB in testing.
            if (!RealmsForgotten.Diagnostics.RFLogSwitchboard.IsEnabled(
                    RealmsForgotten.Diagnostics.RFLogSwitchboard.CampaignAiTrace))
            {
                return;
            }

            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
                foreach (string logPath in LogPaths.Distinct())
                {
                    RealmsForgotten.Diagnostics.RFLogSwitchboard.Append(
                        RealmsForgotten.Diagnostics.RFLogSwitchboard.CampaignAiTrace, logPath, line);
                }
            }
            catch
            {
            }
        }
    }
}
