using System;
using System.IO;
using TaleWorlds.Library;

namespace RF_Settlers
{
    /// <summary>
    /// File diagnostics for the settlers system, same location as the Battle AI
    /// telemetry: Documents\Mount and Blade II Bannerlord\Configs\ModLogs\RF_Settlers.log
    /// </summary>
    public static class SettlersLog
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_Settlers.log");

        public static void Write(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Debug.Print("[RF_Settlers] " + message);
            try
            {
                string directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never take the game down.
            }
        }
    }
}
