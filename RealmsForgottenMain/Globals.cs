using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten
{
    public static class Globals
    {
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
        internal static int GiantsCostMult {  get { return 2; } }
        public static bool IsGiant(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("half_giant"); }
        public static bool IsUndead(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("undead"); }
        public static bool IsHuman(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("human"); }
        public static bool IsMull(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("mull"); }
        public static bool IsElvean(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("elvean"); }
        public static bool IsXilantlacay(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("Xilantlacay"); }
        public static bool IsTlachiquiy(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("tlachiquiy"); }
        public static bool IsUrkrish(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("urkrish"); }
        public static bool IsThog(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("thog"); }
        public static bool IsShaitan(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("shaitan"); }
        public static bool IsKharach(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("kharach"); }
        public static bool IsBrute(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("brute"); }
        public static bool IsBark(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("bark"); }
        public static bool IsNurh(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("nurh"); }
        public static bool IsDaimo(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("daimo"); }
        public static bool IsSillok(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("sillok"); }
        public static bool IsDwarf(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("dwarf"); }
        public static bool IsUrkhai(this BasicCharacterObject character) { return character.Race == FaceGen.GetRaceOrDefault("urkhai"); }

        internal static List<string>  PlayerSelectableRaces { get { return _playerSelectableRaces; } }
        private static List<string> _playerSelectableRaces = new() { "human", "elvean", "undead", "mull", "half_giant", "Xilantlacay", "tlachiquiy", "dwarf", "urkhai" };

        public static bool IsMissionInitialized = false;

        public static List<string> GetOrderedRacesForSelection()
        {
            List<string> orderedRaces = new List<string>
            {
                "human",
                "elvean",
                "undead",
                "mull",
                "half_giant",
                "Xilantlacay",
                "tlachiquiy",
                "dwarf",
            };

            ValidateRaceOrder(orderedRaces);
            return orderedRaces;
        }

        private static void ValidateRaceOrder(List<string> orderedRaces)
        {
            foreach (string race in _playerSelectableRaces)
            {
                if (!orderedRaces.Contains(race))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Warning: Race {race} is not included in the selection order!"));
                }
            }
        }
    }
}
