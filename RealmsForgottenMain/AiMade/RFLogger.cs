using System;
using System.IO;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade
{
    internal static class RFLogger
    {
        private static readonly string LogPath =
            Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", "RF_FreezeProbe.log");

        internal static void Log(string msg)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n";
                File.AppendAllText(LogPath, line);
                Debug.Print("[RF] " + msg);
            }
            catch
            {
                // don’t crash if file IO fails
            }
        }
    }
}
