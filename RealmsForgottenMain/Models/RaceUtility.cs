using System.Collections.Generic;
using TaleWorlds.Library;

namespace RealmsForgotten.Models
{
    public static class RaceUtility
    {
        public static readonly Dictionary<string, int> RaceMappings;

        static RaceUtility()
        {
            RaceMappings = new Dictionary<string, int>();

            List<string> targetRaceNames = new List<string> { "half_giant", "bark", "nurh", "daimo", "sillok", "balrog" };

            foreach (var raceName in targetRaceNames)
            {
                try
                {
                    int raceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault(raceName);
                    if (raceId != -1)
                    {
                        RaceMappings[raceName] = raceId;
                    }
                    else
                    {
                        LogMessage($"RaceUtility: Race '{raceName}' not found.");
                    }
                }
                catch (KeyNotFoundException)
                {
                    LogMessage($"RaceUtility: Race '{raceName}' not found.");
                }
            }
        }

        public static int GetRaceId(string raceName)
        {
            return RaceMappings.TryGetValue(raceName, out int raceId) ? raceId : -1;
        }

        private static void LogMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message));
        }
    }
}