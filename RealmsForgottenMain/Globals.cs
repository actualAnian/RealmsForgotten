using RealmsForgotten.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

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
            Outlaw,
            VassalNoFief,
            King,
            Usurper,
            Knight,
            Mistic
        }

        public static Dictionary<StartType, double> startingSkillMult = new()
        {
            [StartType.Default] = 1,
            [StartType.Merchant] = 1,
            [StartType.Exiled] = 2,
            [StartType.Mercenary] = 1.5,
            [StartType.Outlaw] = 1,
            [StartType.VassalNoFief] = 2,
            [StartType.King] = 3.5,
            [StartType.Usurper] = 3,
            [StartType.Knight] = 2.5,
            [StartType.Mistic] = 1,
        };
        internal static int GiantCountsAs => 2;
        internal static int GiantsCostMult => 2;
        public static bool IsGiant(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "half_giant";
        public static bool IsUndead(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "undead";
        public static bool IsHuman(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "human";
        public static bool IsMull(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "mull";
        public static bool IsElvean(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "elvean";
        public static bool IsXilantlacay(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "Xilantlacay";
        public static bool IsTlachiquiy(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "tlachiquiy";
        public static bool IsUrkrish(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "urkrish";
        public static bool IsThog(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "thog";
        public static bool IsShaitan(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "shaitan";
        public static bool IsKharach(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "kharach";
        public static bool IsBrute(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "brute";
        public static bool IsBark(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "barh";
        public static bool IsNurh(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "nurh";
        public static bool IsDaimo(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "daimo";
        public static bool IsSillok(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "sillok";
        public static bool IsDwarf(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "dwarf";
        public static bool IsUrkhai(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "urkhai";
        public static bool IsOrcbase(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "orc_base";
        public static bool IsEvilWitch(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "evil_witch";
        public static bool IsBalrog(this BasicCharacterObject c) => c != null && RaceManager.Instance.GetRaceNameFromId(c.Race) == "balrog";
        public static bool IsZombie(this BasicCharacterObject c) => RaceManager.Instance.GetRaceNameFromId(c.Race) == "zombie";

        internal static List<string> PlayerSelectableRaces { get { return _playerSelectableRaces; } }
        private static readonly List<string> _playerSelectableRaces = new() { "human", "elvean", "undead", "mull", "half_giant", "Xilantlacay", "dwarf", "urkhai" };

        public static bool IsMissionInitialized = false;

        public static List<string> GetOrderedRacesForSelection()
        {
            List<string> orderedRaces = new()
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
        public static bool IsWarSailsLoaded => ModuleHelper.IsModuleActive("NavalDLC");
        public static bool IsUsingRFWarsailsModule => Directory.GetFiles(ModuleHelper.GetModuleFullPath("RF_Map") + "/ModuleData/DistanceCaches").Count() >= 3;
        public static List<string> SkillsOrderInCharacterDeveloper = new()
        {
            "OneHanded",
            "TwoHanded",
            "Polearm",
            "Bow",
            "Crossbow",
            "Throwing",
            "Riding",
            "Athletics",
            "Crafting",
            "Scouting",
            "Tactics",
            "Roguery",
            "Charm",
            "Leadership",
            "Trade",
            "Steward",
            "Medicine",
            "Engineering",
            "Faith",
            "Arcane",
            "Alchemy",
            "Mariner",
            "Boatswain",
            "Shipmaster"
        };
    }
}