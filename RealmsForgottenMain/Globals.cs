using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten
{
    public static class Globals
    {
        //public static Dictionary<string, string> TroopsIdToRaces = new()
        //{
        //    ["human"] = "imperial_recruit",
        //    ["elvean"] = "battanian_volunteer",
        //    ["mull"] = "aserai_youth",
        //    ["undead"] = "sturgian_warrior_son",
        //    ["half_giant"] = "cs_troll_raiders_raider",
        //    ["Xilantlacay"] = "giant_archer",
        //    ["tlachiquiy"] = "minotaur"
        //};
        public static Dictionary<string, int> RacesIds = new();

        public static Assembly realmsForgottenAssembly = Assembly.GetExecutingAssembly();

        public static ICustomSettingsProvider Settings { get { return RFSettings.Instance; } }
        public enum StartType
        {
            Other = -1,
            Default,
            Merchant,
            Exiled,
            Mercenary,
            Looter,
            VassalNoFief,
            KingdomRuler,
            CastleRuler,
            VassalFief,
            EscapedPrisoner
        }

        public static Dictionary<StartType, double> startingSkillMult = new()
        {
            [StartType.Default] = 1,
            [StartType.Merchant] = 1,
            [StartType.Exiled] = 2,
            [StartType.Mercenary] = 1.5,
            [StartType.Looter] = 1,
            [StartType.VassalNoFief] = 2,
            [StartType.KingdomRuler] = 3.5,
            [StartType.CastleRuler] = 3,
            [StartType.VassalFief] = 2.5,
            [StartType.EscapedPrisoner] = 1,
        };
        internal static int GiantCountsAs { get { return 2; } }

        //internal static void SetRacesIds()
        //{
        //    foreach(KeyValuePair<string, string> TroopPair in TroopsIdToRaces)
        //    {
        //        try
        //        {
        //            CharacterObject troop = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>(TroopPair.Value);
        //            FaceGen.GetRaceOrDefault(TroopPair.Key] = troop.Race;
        //        }
        //        catch
        //        {
        //            FaceGen.GetRaceOrDefault(TroopPair.Key] = -1;
        //            string text = $"error trying to set the race id of {TroopPair.Key} in RealmsForgotten.Globals.SetGiantRaceId";
        //            InformationManager.DisplayMessage(new InformationMessage(text, new Color(1, 0, 0)));
        //        }
        //    }
        //}
        public static bool IsGiant(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("half_giant"); }
        public static bool IsUndead(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("undead"); }
        public static bool IsHuman(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("human"); }
        public static bool IsMull(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("mull"); }
        public static bool IsElvean(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("elvean"); }
        public static bool IsXilantlacay(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("Xilantlacay"); }
        public static bool IsTlachiquiy(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("tlachiquiy"); }

        internal static List<string>  PlayerSelectableRaces { get { return _playerSelectableRaces; } }
        private static List<string> _playerSelectableRaces = new() { "human", "elvean", "undead", "mull", "half_giant", "Xilantlacay" };
    }
}
