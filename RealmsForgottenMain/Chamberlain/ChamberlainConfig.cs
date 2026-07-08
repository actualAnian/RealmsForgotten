using System.Collections.Generic;

namespace RealmsForgotten.Chamberlain
{
    public class ChamberlainConfig
    {
        public static List<string> PurchasableUnitList = new()
        {
            "rf_house_troop_recruit",
            "rf_house_troop_infantry",
            "rf_house_troop_archer",
            "rf_house_troop_trained_infantry",
            "rf_house_troop_spearman",
            "rf_house_troop_squire",
            "rf_house_troop_bowman",
        };
        public static List<string> PossibleTroopRaces = new()
        {
            "human",
            "elvean",
            "undead",
            "mull",
            "half_giant",
            "Xilantlacay",
            "dwarf",
            "urkhai"
        };
        public static string ChamberlainFolder = "RFChamberlain";
        public static string BackupXmlFile = "RFBaseChamberlainTroops.xml";
    }
}
