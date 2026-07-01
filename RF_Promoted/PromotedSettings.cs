using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace RF_Promoted;

public sealed class PromotedSettings
{
    public bool DebugMessages { get; set; }
    public bool AlwaysPromote { get; set; }
    public float RatioThreshold { get; set; } = 1.1f;
    public int MeritPerKill { get; set; } = 1;
    public int MeritThreshold { get; set; } = 8;
    public bool AllowMultiplePromotions { get; set; }
    public bool PromotedCompanionsIncreaseLimit { get; set; }
    public bool EnableBonusCompanionLimit { get; set; }
    public int BonusCompanionLimitValue { get; set; } = 1;

    public static PromotedSettings Current { get; private set; } = new();

    public static void Load()
    {
        PromotedSettings settings = new();

        foreach (string path in GetCandidatePaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                ParseInto(settings, File.ReadAllLines(path));
                Current = settings;
                return;
            }
            catch
            {
                // Keep defaults if config is malformed.
            }
        }

        Current = settings;
    }

    private static void ParseInto(PromotedSettings settings, string[] lines)
    {
        foreach (string rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            string line = rawLine.Trim();
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = line.Substring(0, separatorIndex).Trim();
            string value = line.Substring(separatorIndex + 1).Trim();

            switch (key)
            {
                case "DebugMessages":
                    settings.DebugMessages = ParseBool(value);
                    break;
                case "AlwaysPromote":
                    settings.AlwaysPromote = ParseBool(value);
                    break;
                case "RatioThreshold":
                    settings.RatioThreshold = ParseFloat(value, settings.RatioThreshold);
                    break;
                case "MeritPerKill":
                    settings.MeritPerKill = ParseInt(value, settings.MeritPerKill);
                    break;
                case "MeritThreshold":
                    settings.MeritThreshold = ParseInt(value, settings.MeritThreshold);
                    break;
                case "AllowMultiplePromotions":
                    settings.AllowMultiplePromotions = ParseBool(value);
                    break;
                case "PromotedCompanionsIncreaseLimit":
                    settings.PromotedCompanionsIncreaseLimit = ParseBool(value);
                    break;
                case "EnableBonusCompanionLimit":
                    settings.EnableBonusCompanionLimit = ParseBool(value);
                    break;
                case "BonusCompanionLimitValue":
                    settings.BonusCompanionLimitValue = ParseInt(value, settings.BonusCompanionLimitValue);
                    break;
            }
        }
    }

    private static bool ParseBool(string value)
    {
        return bool.TryParse(value, out bool parsed) && parsed;
    }

    private static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
    }

    private static string[] GetCandidatePaths()
    {
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        string moduleRoot = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", ".."));

        return new[]
        {
            Path.Combine(moduleRoot, "promoted_config.txt"),
            Path.Combine(assemblyDirectory, "promoted_config.txt"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "promoted_config.txt")
        };
    }
}
