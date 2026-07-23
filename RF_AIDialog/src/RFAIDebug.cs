using System;
using System.IO;

namespace RF_AIDialog
{
    /// <summary>
    /// Minimal file logger for diagnosing save/load crashes.
    /// Writes to %USERPROFILE%\Documents\rfai_debug.log.
    ///
    /// Gated behind AIConfig.DebugMenuEnabled: this used to be always-on with
    /// no size cap — lip-sync logged per frame of speech and every LLM reply
    /// was dumped, growing the file to hundreds of MB over weeks while adding
    /// disk I/O to the game thread.
    /// </summary>
    internal static class RFAIDebug
    {
        private const long MaxLogBytes = 10L * 1024 * 1024;

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "rfai_debug.log");

        private static bool _sizeChecked;

        public static void Log(string message)
        {
            try
            {
                if (!AIConfig.DebugMenuEnabled)
                    return;

                // One rotation check per session: if a previous session left the
                // log oversized, start fresh instead of growing forever.
                if (!_sizeChecked)
                {
                    _sizeChecked = true;
                    var info = new FileInfo(LogPath);
                    if (info.Exists && info.Length > MaxLogBytes)
                        info.Delete();
                }

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
