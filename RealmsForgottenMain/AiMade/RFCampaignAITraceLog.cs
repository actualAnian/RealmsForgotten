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
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
                foreach (string logPath in LogPaths.Distinct())
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(logPath);
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.AppendAllText(logPath, line);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }
}
