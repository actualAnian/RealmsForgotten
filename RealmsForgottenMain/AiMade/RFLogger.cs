using System;
using System.IO;
using System.Linq;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade
{
    internal static class RFLogger
    {
        private static readonly string[] LogPaths =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_ColdLoadProbe.log"),
            Path.Combine(BasePath.Name, "Configs", "ModLogs", "RF_ColdLoadProbe.log"),
            Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", "RF_ColdLoadProbe.log")
        };

        internal static void Log(string msg)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n";
                foreach (string logPath in LogPaths.Distinct())
                {
                    try
                    {
                        string? directory = Path.GetDirectoryName(logPath);
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.AppendAllText(logPath, line);
                    }
                    catch
                    {
                        // Try the next path.
                    }
                }
                Debug.Print("[RF] " + msg);
            }
            catch
            {
                // don’t crash if file IO fails
            }
        }
    }
}
