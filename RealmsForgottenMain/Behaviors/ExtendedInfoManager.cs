using RealmsForgotten.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.Behaviors
{
    public static class ExtendedInfoManager
    {
        private static Dictionary<string, List<ResistanceTuple>> raceResistances = new Dictionary<string, List<ResistanceTuple>>();

        static ExtendedInfoManager()
        {
            // Initialize with specific resistances for given races
            raceResistances["bark"] = new List<ResistanceTuple>
        {
            new ResistanceTuple("Physical", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Pierce", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Blunt", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Fire", 0.0f) // No reduction
        };
            raceResistances["daimo"] = new List<ResistanceTuple>
        {
            new ResistanceTuple("Physical", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Pierce", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Blunt", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Fire", 0.0f) // No reduction
        };
            raceResistances["sillok"] = new List<ResistanceTuple>
        {
            new ResistanceTuple("Physical", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Pierce", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Blunt", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Fire", 0.0f) // No reduction
        };
            raceResistances["nurh"] = new List<ResistanceTuple>
        {
            new ResistanceTuple("Physical", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Pierce", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Blunt", 1.0f), // 100% reduction (immune)
            new ResistanceTuple("Fire", 0.0f) // No reduction
        };
        }
        public static List<ResistanceTuple> GetRaceResistances(string raceId)
        {
            return raceResistances.ContainsKey(raceId) ? raceResistances[raceId] : new List<ResistanceTuple>();
        }
    }

}
