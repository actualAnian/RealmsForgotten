using System;
using System.IO;

namespace RF_AIDialog
{
    /// <summary>
    /// Minimal file logger for diagnosing save/load crashes.
    /// Writes to %USERPROFILE%\Documents\rfai_debug.log.
    /// Remove (or comment out all Log calls) once the crash is resolved.
    /// </summary>
    internal static class RFAIDebug
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "rfai_debug.log");

        public static void Log(string message)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, line);
            }
            catch { }
        }

        public static void Clear()
        {
            try { File.Delete(LogPath); } catch { }
        }
    }
}
