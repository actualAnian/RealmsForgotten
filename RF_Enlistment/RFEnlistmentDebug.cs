using System;
using System.IO;

namespace RF_Enlistment;

internal static class RFEnlistmentDebug
{
    public static bool Enabled => false;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord",
        "Configs",
        "ModLogs",
        "RF_EnlistmentTrace.log");

    public static void Log(string message)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            string? directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line);
        }
        catch
        {
        }
    }

    public static void Clear()
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            if (File.Exists(LogPath))
            {
                File.Delete(LogPath);
            }
        }
        catch
        {
        }
    }
}
