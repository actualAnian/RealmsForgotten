using System;
using System.IO;

namespace RealmsForgotten.Patches
{
    internal static class RFHardDebugLog
    {
        private const bool Enabled = false;

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Configs",
            "ModLogs",
            "RF_HardDebug.log");

        internal static void Write(string message)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                string? dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
