using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten
{
    public static class Globals
    {
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
        internal static int GiantCountsAs => 2;
        internal static int GiantsCostMult => 2;

        // ---------- CACHED RACE IDS (int) ----------
        private static readonly int _raceHalfGiantId = FaceGen.GetRaceOrDefault("half_giant");
        private static readonly int _raceUndeadId = FaceGen.GetRaceOrDefault("undead");
        private static readonly int _raceHumanId = FaceGen.GetRaceOrDefault("human");
        private static readonly int _raceMullId = FaceGen.GetRaceOrDefault("mull");
        private static readonly int _raceElveanId = FaceGen.GetRaceOrDefault("elvean");
        private static readonly int _raceXilantId = FaceGen.GetRaceOrDefault("Xilantlacay");
        private static readonly int _raceTlachId = FaceGen.GetRaceOrDefault("tlachiquiy");
        private static readonly int _raceUrkrishId = FaceGen.GetRaceOrDefault("urkrish");
        private static readonly int _raceThogId = FaceGen.GetRaceOrDefault("thog");
        private static readonly int _raceShaitanId = FaceGen.GetRaceOrDefault("shaitan");
        private static readonly int _raceKharachId = FaceGen.GetRaceOrDefault("kharach");
        private static readonly int _raceBruteId = FaceGen.GetRaceOrDefault("brute");
        private static readonly int _raceBarkId = FaceGen.GetRaceOrDefault("bark");
        private static readonly int _raceNurhId = FaceGen.GetRaceOrDefault("nurh");
        private static readonly int _raceDaimoId = FaceGen.GetRaceOrDefault("daimo");
        private static readonly int _raceSillokId = FaceGen.GetRaceOrDefault("sillok");
        private static readonly int _raceDwarfId = FaceGen.GetRaceOrDefault("dwarf");
        private static readonly int _raceUrkhaiId = FaceGen.GetRaceOrDefault("urkhai");
        private static readonly int _raceOrcbaseId = FaceGen.GetRaceOrDefault("orc_base");
        private static readonly int _raceEvilWitchId = FaceGen.GetRaceOrDefault("evil_witch");
        private static readonly int _raceBalrogId = FaceGen.GetRaceOrDefault("balrog");
        private static readonly int _raceZombieId = FaceGen.GetRaceOrDefault("zombie"); 

        // Small helper
        private static bool HasRace(int race, int target) => race == target;

        // ---------- EXTENSIONS FOR BasicCharacterObject ----------
        public static bool IsGiant(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceHalfGiantId);
        public static bool IsUndead(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceUndeadId);
        public static bool IsHuman(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceHumanId);
        public static bool IsMull(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceMullId);
        public static bool IsElvean(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceElveanId);
        public static bool IsXilantlacay(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceXilantId);
        public static bool IsTlachiquiy(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceTlachId);
        public static bool IsUrkrish(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceUrkrishId);
        public static bool IsThog(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceThogId);
        public static bool IsShaitan(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceShaitanId);
        public static bool IsKharach(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceKharachId);
        public static bool IsBrute(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceBruteId);
        public static bool IsBark(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceBarkId);
        public static bool IsNurh(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceNurhId);
        public static bool IsDaimo(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceDaimoId);
        public static bool IsSillok(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceSillokId);
        public static bool IsDwarf(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceDwarfId);
        public static bool IsUrkhai(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceUrkhaiId);
        public static bool IsOrcbase(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceOrcbaseId);
        public static bool IsEvilWitch(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceEvilWitchId);
        public static bool IsBalrog(this BasicCharacterObject c) => c != null && HasRace(c.Race, _raceBalrogId);
        public static bool IsZombie(this BasicCharacterObject c) => HasRace(c.Race, _raceZombieId);

        // ---------- EXTENSIONS FOR CharacterObject (forwarders) ----------
        public static bool IsGiant(this CharacterObject c) => c != null && HasRace(c.Race, _raceHalfGiantId);
        public static bool IsUndead(this CharacterObject c) => c != null && HasRace(c.Race, _raceUndeadId);
        public static bool IsHuman(this CharacterObject c) => c != null && HasRace(c.Race, _raceHumanId);
        public static bool IsMull(this CharacterObject c) => c != null && HasRace(c.Race, _raceMullId);
        public static bool IsElvean(this CharacterObject c) => c != null && HasRace(c.Race, _raceElveanId);
        public static bool IsXilantlacay(this CharacterObject c) => c != null && HasRace(c.Race, _raceXilantId);
        public static bool IsTlachiquiy(this CharacterObject c) => c != null && HasRace(c.Race, _raceTlachId);
        public static bool IsUrkrish(this CharacterObject c) => c != null && HasRace(c.Race, _raceUrkrishId);
        public static bool IsThog(this CharacterObject c) => c != null && HasRace(c.Race, _raceThogId);
        public static bool IsShaitan(this CharacterObject c) => c != null && HasRace(c.Race, _raceShaitanId);
        public static bool IsKharach(this CharacterObject c) => c != null && HasRace(c.Race, _raceKharachId);
        public static bool IsBrute(this CharacterObject c) => c != null && HasRace(c.Race, _raceBruteId);
        public static bool IsBark(this CharacterObject c) => c != null && HasRace(c.Race, _raceBarkId);
        public static bool IsNurh(this CharacterObject c) => c != null && HasRace(c.Race, _raceNurhId);
        public static bool IsDaimo(this CharacterObject c) => c != null && HasRace(c.Race, _raceDaimoId);
        public static bool IsSillok(this CharacterObject c) => c != null && HasRace(c.Race, _raceSillokId);
        public static bool IsDwarf(this CharacterObject c) => c != null && HasRace(c.Race, _raceDwarfId);
        public static bool IsUrkhai(this CharacterObject c) => c != null && HasRace(c.Race, _raceUrkhaiId);
        public static bool IsOrcbase(this CharacterObject c) => c != null && HasRace(c.Race, _raceOrcbaseId);
        public static bool IsEvilWitch(this CharacterObject c) => c != null && HasRace(c.Race, _raceEvilWitchId);
        public static bool IsBalrog(this CharacterObject c) => c != null && HasRace(c.Race, _raceBalrogId);

        internal static List<string>  PlayerSelectableRaces { get { return _playerSelectableRaces; } }
        private static List<string> _playerSelectableRaces = new() { "human", "elvean", "undead", "mull", "half_giant", "Xilantlacay", "dwarf", "urkhai" };

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
                "dwarf",
                "urkhai"
            };

            ValidateRaceOrder(orderedRaces);
            return orderedRaces;
        }
        public static bool IsBanditParty(PartyBase party)
        {
            return party?.MobileParty?.PartyComponent?.GetType()?.Name == "BanditPartyComponent";
        }

        public static bool IsCaravanParty(PartyBase party)
        {
            return party.MobileParty.PartyComponent.GetType().Name == "VillagerPartyComponent";
        }
        public static bool IsVillager(PartyBase party)
        {
            return party.MobileParty.PartyComponent.GetType().Name == "VillagerPartyComponent";
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
        // Career.PlayerCareerExtension.DamageType
        public readonly static List<(Func<BasicCharacterObject, bool> Check, float[] Resistances)> RaceResistances = new()
        {
            (character => character.IsDwarf(), new float[] { 0f, 0f, 0f, 0.4f, 0f, 0f }),
            (character => character.IsGiant(), new float[] { 0f, 0.5f, 0.5f, -0.2f, -0.2f, 0f }),
            //(character => character.IsTroll(), new float[] { 0f, 0.5f, 0.5f, 0.3f, -0.2f, 0f }),
            (character => character.IsUndead(), new float[] { 0f, 0f, 0f, 0.1f, -0.2f, 0f }),
            (character => character.IsHuman(), new float[] { 0f, 0f, 0f, 0f, 0f, 0f }),
            (character => character.IsTlachiquiy(), new float[] { 0f, 0f, 0f, 0.3f, -0.1f, 0f }),
            //(character => character.IsDemon(), new float[] { 0f, 0f, 0f, -0.2f, 0.5f, 0f }),
            (character => character.IsMull(), new float[] { 0f, 0.2f, 0.2f, -0.1f, 0f, 0f }),
            (character => character.IsUrkhai(), new float[] { 0f, 0f, 0f, 0.2f, -0.1f, 0f }),
            (character => character.IsElvean(), new float[] { 0f, 0f, 0f, 0.15f, -0.15f, 0f }),
            (character => character.IsXilantlacay(), new float[] { 0f, 0f, 0f, 0.15f, 0f, 0f }),
            (character => character.IsOrcbase(), new float[] { 0f, 0.15f, 0f, 0.1f, 0f, 0f }),
            (character => character.IsEvilWitch(), new float[] { 0f, 0f, 0f, 0.15f, -0.2f, 0f })
         };
        public static bool IsCurrentMainAgentPlayerHero()
        {
            return TaleWorlds.MountAndBlade.Agent.Main != null && TaleWorlds.MountAndBlade.Agent.Main.Character == CharacterObject.PlayerCharacter;
        }
    }
}