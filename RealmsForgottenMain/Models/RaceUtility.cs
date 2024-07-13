using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace RealmsForgotten.Models
{
    public static class RaceUtility
    {
        public static readonly Dictionary<string, int> RaceMappings;

        static RaceUtility()
        {
            RaceMappings = new Dictionary<string, int>();

            // List of target race names
            List<string> targetRaceNames = new List<string> { "half_giant", "bark", "nurh", "daimo", "sillok", "unknown" };

            // Attempt to get the race ID for each target race name and add to the HashSet
            foreach (var raceName in targetRaceNames)
            {
                try
                {
                    int raceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault(raceName);
                    if (raceId != -1) // Assuming -1 is returned if the race is not found
                    {
                        RaceMappings[raceName] = raceId;
                        LogMessage($"RaceUtility: Added race '{raceName}' with ID {raceId}.");
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