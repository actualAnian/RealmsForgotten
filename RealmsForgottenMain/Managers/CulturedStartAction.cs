using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using static RealmsForgotten.Globals;

namespace RealmsForgotten.Managers
{
    public class TroopSpawnInfo
    {
        public string TroopId { get; set; }
        public int Quantity { get; set; }

        public TroopSpawnInfo(string troopId, int quantity)
        {
            TroopId = troopId;
            Quantity = quantity;
        }
    }

    public static class CulturedStartAction
    {
        public static readonly Dictionary<StartType, Dictionary<string, string>> mainHeroStartingEquipment = new()
        {
            [StartType.Default] = new Dictionary<string, string>
            {
                ["aserai"] = "player_char_creation_default",
                ["empire"] = "player_char_creation_default",
                ["khuzait"] = "player_char_creation_default",
                ["sturgia"] = "player_char_creation_default",
                ["battania"] = "player_char_creation_default",
                ["vlandia"] = "player_char_creation_default",
                ["giant"] = "rf_xilan_default",
                ["aqarun"] = "rf_aqarun_default",
                ["south_realm"] = "player_char_creation_default",
            },
            [StartType.Merchant] = new Dictionary<string, string>
            {
                ["aserai"] = "merchant_start_aserai",
                ["empire"] = "merchant_start_empire",
                ["khuzait"] = "merchant_start_khuzait",
                ["sturgia"] = "merchant_start_sturgia",
                ["battania"] = "rf_elvean_merchant",
                ["vlandia"] = "merchant_start_vlandia",
                ["giant"] = "merchant_start_xilan",
                ["aqarun"] = "merchant_start_aqarun",
                ["south_realm"] = "merchant_start_empire",
            },
            [StartType.Exiled] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_exiled_equip",
                ["empire"] = "rf_exiled_equip",
                ["khuzait"] = "rf_exiled_equip",
                ["sturgia"] = "rf_exiled_equip",
                ["battania"] = "rf_exiled_equip",
                ["vlandia"] = "rf_exiled_equip",
                ["giant"] = "rf_exiled_equip",
                ["aqarun"] = "rf_exiled_equip",
                ["south_realm"] = "rf_exiled_equip"
            },
            [StartType.EscapedPrisoner] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_athas_mistic",
                ["empire"] = "rf_empire_mistic",
                ["khuzait"] = "rf_khuzait_mistic",
                ["sturgia"] = "rf_sturgia_mistic",
                ["battania"] = "rf_elvean_mistic",
                ["vlandia"] = "rf_nasoria_mistic",
                ["giant"] = "rf_giant_mistic",
                ["aqarun"] = "rf_aqarun_mistic",
                ["south_realm"] = "rf_empire_mistic"
            },
            [StartType.Looter] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_looter",
                ["empire"] = "rf_looter",
                ["khuzait"] = "rf_looter",
                ["sturgia"] = "rf_looter",
                ["battania"] = "rf_looter",
                ["vlandia"] = "rf_looter",
                ["giant"] = "rf_looter",
                ["aqarun"] = "rf_looter",
                ["south_realm"] = "rf_looter"
            },
            [StartType.Mercenary] = new Dictionary<string, string>
            {
                ["aserai"] = "merc_athas_start",
                ["empire"] = "merc_realms_start",
                ["khuzait"] = "merc_allkhuur_start",
                ["sturgia"] = "merc_vortiak_start",
                ["battania"] = "merc_elvean_start",
                ["vlandia"] = "merc_nasoria_start",
                ["giant"] = "merc_giant_start",
                ["aqarun"] = "merc_athas_start",
                ["south_realm"] = "merc_realms_start",
            },
            [StartType.VassalNoFief] = new Dictionary<string, string>
            {
                ["aserai"] = "athas_vassal_nofief_equip",
                ["empire"] = "realms_vassal_nofief",
                ["khuzait"] = "khuzait_vassal_nofief",
                ["sturgia"] = "dreadrealms_vassal_nofief",
                ["battania"] = "elvean_vassal_nofief",
                ["vlandia"] = "nasoria_vassal_nofief",
                ["giant"] = "giant_vassal_nofief",
                ["aqarun"] = "vassalnofief_aqarun_start",
                ["south_realm"] = "realms_vassal_nofief",
            },
            [StartType.KingdomRuler] = new Dictionary<string, string>
            {
                ["aserai"] = "king_athas_start",
                ["empire"] = "king_realms_start",
                ["khuzait"] = "king_allkhuur_start",
                ["sturgia"] = "king_vortiak_start",
                ["battania"] = "king_elvean_start",
                ["vlandia"] = "king_nasoria_start",
                ["giant"] = "king_giant_start",
                ["aqarun"] = "king_aqarun_start",
                ["south_realm"] = "king_realms_start",
            },
            [StartType.CastleRuler] = new Dictionary<string, string>
            {
                ["aserai"] = "vassal_athas_start",
                ["empire"] = "vassal_realms_start",
                ["khuzait"] = "vassal_allkhuur_start",
                ["sturgia"] = "vassal_vortiak_start",
                ["battania"] = "vassal_elvean_start",
                ["vlandia"] = "vassal_nasoria_start",
                ["giant"] = "vassal_giant_start",
                ["aqarun"] = "vassal_aqarun_start",
                ["south_realm"] = "vassal_realms_start",
            },
            [StartType.VassalFief] = new Dictionary<string, string>
            {
                ["aserai"] = "ruler_athas_start",
                ["empire"] = "ruler_realms_start",
                ["khuzait"] = "ruler_allkhuur_start",
                ["sturgia"] = "ruler_dreadrealms_start",
                ["battania"] = "lord_elvean_start",
                ["vlandia"] = "ruler_nasoria_start",
                ["giant"] = "ruler_giant_start",
                ["aqarun"] = "ruler_aqarun_start",
                ["south_realm"] = "ruler_realms_start",
            },
        };
        public static readonly Dictionary<(string Culture, StartType StartOption), List<TroopSpawnInfo>> CultureStartTypeToTroops = new()
        {
         { ("aserai", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_trader", 5),
                new TroopSpawnInfo("aserai_recruit", 10),
                new TroopSpawnInfo("aserai_archer", 5) }},

           { ("aserai", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_recruit", 5),
                new TroopSpawnInfo("aserai_archer", 3) }},

            { ("aserai", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_recruit", 2) }},

            { ("aserai", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_bandit", 8) }},

            { ("aserai", StartType.Mercenary), new List<TroopSpawnInfo> {
              new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("aserai", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_veteran_infantry", 10),
                new TroopSpawnInfo("aserai_master_archer", 5),
                new TroopSpawnInfo("aserai_mameluke", 3) }},

            { ("aserai", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_veteran_infantry", 20),
                new TroopSpawnInfo("aserai_master_archer", 10),
                new TroopSpawnInfo("aserai_mameluke", 10) }},

            { ("aserai", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_veteran_infantry", 15),
                new TroopSpawnInfo("aserai_master_archer", 10),
                new TroopSpawnInfo("aserai_mameluke", 5) }},

            { ("aserai", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_veteran_infantry", 10),
                new TroopSpawnInfo("aserai_master_archer", 5),
                new TroopSpawnInfo("aserai_mameluke", 5) }},

            { ("empire", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("empire_trader", 4),
                new TroopSpawnInfo("empire_recruit", 8),
                new TroopSpawnInfo("empire_archer", 4) }},

            { ("empire", StartType.Exiled), new List<TroopSpawnInfo> {
                 new TroopSpawnInfo("imperial_recruit", 4),
                new TroopSpawnInfo("imperial_archer", 2) }},

            { ("empire", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("empire", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("empire", StartType.Mercenary), new List<TroopSpawnInfo> {
               new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("empire", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 3) }},

            { ("empire", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 20),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 10) }},

            { ("empire", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 15),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 5) }},

            { ("empire", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 5) }},

             { ("battania", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battania_trader", 3),
                new TroopSpawnInfo("battania_volunteer", 10),
                new TroopSpawnInfo("battania_skirmisher", 5) }},

            { ("battania", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battania_volunteer", 5),
                new TroopSpawnInfo("battania_skirmisher", 3) }},

            { ("battania", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_volunteer", 2) }},

            { ("battania", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("battania", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("battania", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_veteran_falxman", 10),
                new TroopSpawnInfo("battanian_fian_champion", 5),
                new TroopSpawnInfo("battanian_hero", 3) }},

            { ("battania", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_veteran_falxman", 20),
                new TroopSpawnInfo("battanian_fian_champion", 10),
                new TroopSpawnInfo("battanian_hero", 10) }},

            { ("battania", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_veteran_falxman", 15),
                new TroopSpawnInfo("battanian_fian_champion", 10),
                new TroopSpawnInfo("battanian_hero", 5) }},

            { ("battania", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_veteran_falxman", 10),
                new TroopSpawnInfo("battanian_fian_champion", 5),
                new TroopSpawnInfo("battanian_heroic_lineage", 5) }},

            { ("sturgia", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("merchant_soldier", 4),
                new TroopSpawnInfo("merchant_archer", 10),
                new TroopSpawnInfo("merchant_trained_infantry", 4) }},

            { ("sturgia", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgian_recruit", 5),
                new TroopSpawnInfo("sturgian_archer", 2) }},

            { ("sturgia", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("sturgia", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("sturgia", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("sturgia", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgian_veteran_warrior", 10),
                new TroopSpawnInfo("sturgia_hardened_brigand", 5),
                new TroopSpawnInfo("sturgia_druzhinnik", 3) }},

            { ("sturgia", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgia_veteran_warrior", 20),
                new TroopSpawnInfo("sturgian_shock_troop", 10),
                new TroopSpawnInfo("sturgian_ulfhednar", 10),
                new TroopSpawnInfo("sturgian_druzhinnik", 10) }},

            { ("sturgia", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgian_veteran_warrior", 15),
                new TroopSpawnInfo("sturgian_shock_troop", 10),
                new TroopSpawnInfo("druzhinnik", 5) }},

            { ("sturgia", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgian_veteran_warrior", 10),
                new TroopSpawnInfo("sturgian_shock_troop", 5),
                new TroopSpawnInfo("druzhinnik", 5) }},

            { ("khuzait", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("merchant_trained_infantry", 3),
                new TroopSpawnInfo("merchant_soldier", 10),
                new TroopSpawnInfo("merchant_archer", 5) }},

            { ("khuzait", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_nomad", 5),
                new TroopSpawnInfo("khuzait_horse_archer", 2) }},

            { ("khuzait", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_nomad", 2) }},

            { ("khuzait", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("khuzait", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("khuzait", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_darkhan", 10),
                new TroopSpawnInfo("khuzait_marksman", 5),
                new TroopSpawnInfo("khuzait_heavy_lancer", 3) }},

            { ("khuzait", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_darkhan", 20),
                new TroopSpawnInfo("khuzait_marksman", 10),
                new TroopSpawnInfo("khuzait_heavy_lancer", 10) }},

            { ("khuzait", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_darkhan", 15),
                new TroopSpawnInfo("khuzait_marksman", 10),
                new TroopSpawnInfo("khuzait_heavy_lancer", 5) }},

            { ("khuzait", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_darkhan", 10),
                new TroopSpawnInfo("khuzait_marksman", 5),
                new TroopSpawnInfo("khuzait_heavy_lancer", 5) }},

            { ("vlandia", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("merchant_trained_infantry", 3),
                new TroopSpawnInfo("merchant_soldier", 10),
                new TroopSpawnInfo("merchant_archer", 5) }},

            { ("vlandia", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_recruit", 5),
                new TroopSpawnInfo("vlandian_crossbowman", 2) }},

            { ("vlandia", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("vlandia", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("vlandia", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_infantry", 5),
                new TroopSpawnInfo("vlandian_crossbowman", 5),
                new TroopSpawnInfo("vlandian_knight", 2) }},

            { ("vlandia", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_sergeant", 10),
                new TroopSpawnInfo("vlandian_sharpshooter", 5),
                new TroopSpawnInfo("vlandian_banner_knight", 3) }},

            { ("vlandia", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_sergeant", 20),
                new TroopSpawnInfo("vlandian_sharpshooter", 10),
                new TroopSpawnInfo("vlandian_banner_knight", 10) }},

            { ("vlandia", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_sergeant", 15),
                new TroopSpawnInfo("vlandian_sharpshooter", 10),
                new TroopSpawnInfo("vlandian_banner_knight", 5) }},

            { ("vlandia", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_sergeant", 10),
                new TroopSpawnInfo("vlandian_sharpshooter", 5),
                new TroopSpawnInfo("vlandian_banner_knight", 5) }},

            { ("giant", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("merchant_trained_infantry", 3),
                new TroopSpawnInfo("merchant_soldier", 10),
                new TroopSpawnInfo("merchant_archer", 5) }},

            { ("giant", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("xilan_troop", 3),
                new TroopSpawnInfo("giant_skirmisher", 2) }},

            { ("giant", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("giant", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 6) }},

            { ("giant", StartType.Mercenary), new List<TroopSpawnInfo> {
               new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("giant", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("giant_trained_infantry", 5),
                new TroopSpawnInfo("giant_experienced_infantry", 3),
                new TroopSpawnInfo("half_giant_archer", 2) }},

            { ("giant", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("giant_trained_infantry", 10),
                new TroopSpawnInfo("giant_experienced_infantry", 5),
                new TroopSpawnInfo("giant_berzerker_infantry", 5) }},

            { ("giant", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("giant_trained_infantry", 8),
                new TroopSpawnInfo("giant_experienced_infantry", 5),
                new TroopSpawnInfo("giant_leader_archer", 2) }},

            { ("giant", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("giant_experienced_infantry", 7),
                new TroopSpawnInfo("giant_berzerker_infantry", 5),
                new TroopSpawnInfo("giant_leader_archer", 3) }},

            { ("aqarun", StartType.Merchant), new List<TroopSpawnInfo> {
                 new TroopSpawnInfo("merchant_trained_infantry", 3),
                new TroopSpawnInfo("merchant_soldier", 10),
                new TroopSpawnInfo("merchant_archer", 5) }},

            { ("aqarun", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_troop", 6),
                new TroopSpawnInfo("Aqarun_cavalry", 2) }},

            { ("aqarun", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("aqarun", StartType.Looter), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 6) }},

            { ("aqarun", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("athas_arena_fighter_a", 5),
                new TroopSpawnInfo("athas_arena_ranged_a", 3) }},

            { ("aqarun", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_infantry", 5),
                new TroopSpawnInfo("Aqarun_warrior", 3),
                new TroopSpawnInfo("Aqarun_skirmisher_trained", 2) }},

            { ("aqarun", StartType.KingdomRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_warrior", 8),
                new TroopSpawnInfo("Aqarun_champion", 4),
                new TroopSpawnInfo("Aqarun_skirmisher_expert", 2) }},

            { ("aqarun", StartType.CastleRuler), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_infantry", 6),
                new TroopSpawnInfo("Aqarun_cavalry_veteran", 4),
                new TroopSpawnInfo("Aqarun_skirmisher_veteran", 2) }},

            { ("aqarun", StartType.VassalFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_fighter", 5),
                new TroopSpawnInfo("Aqarun_cavalry_master", 3),
                new TroopSpawnInfo("Aqarun_archer", 2) }},

            { ("south_realm", StartType.Merchant), new List<TroopSpawnInfo> {
    new TroopSpawnInfo("empire_trader", 4),
    new TroopSpawnInfo("empire_recruit", 8),
    new TroopSpawnInfo("empire_archer", 4) }},

{ ("south_realm", StartType.Exiled), new List<TroopSpawnInfo> {
     new TroopSpawnInfo("imperial_recruit", 4),
    new TroopSpawnInfo("imperial_archer", 2) }},

{ ("south_realm", StartType.EscapedPrisoner), new List<TroopSpawnInfo> {
    new TroopSpawnInfo("looter", 2) }},

{ ("south_realm", StartType.Looter), new List<TroopSpawnInfo> {
    new TroopSpawnInfo("looter", 8) }},

{ ("south_realm", StartType.Mercenary), new List<TroopSpawnInfo> {
   new TroopSpawnInfo("mercenary_volunteer", 10) }},

{ ("south_realm", StartType.VassalNoFief), new List<TroopSpawnInfo> {
    new TroopSpawnInfo("imperial_legionary", 10),
    new TroopSpawnInfo("imperial_palatine_guard", 5),
    new TroopSpawnInfo("imperial_cataphract", 3) }},

{ ("south_realm", StartType.KingdomRuler), new List<TroopSpawnInfo> {
    new TroopSpawnInfo("imperial_legionary", 20),
    new TroopSpawnInfo("imperial_palatine_guard", 10),
    new TroopSpawnInfo("imperial_cataphract", 10) }},

{ ("south_realm", StartType.CastleRuler), new List<TroopSpawnInfo> {
    new TroopSpawnInfo("imperial_legionary", 15),
    new TroopSpawnInfo("imperial_palatine_guard", 10),
    new TroopSpawnInfo("imperial_cataphract", 5) }},

{ ("south_realm", StartType.VassalFief), new List<TroopSpawnInfo> {
    new TroopSpawnInfo("imperial_legionary", 10),
    new TroopSpawnInfo("imperial_palatine_guard", 5),
    new TroopSpawnInfo("imperial_cataphract", 5) }},


    };

        public static List<TroopSpawnInfo> GetTroopsForStartOption(string culture, StartType startOption)
        {
            if (CultureStartTypeToTroops.TryGetValue((culture, startOption), out var troopList))
            {
                return troopList;
            }
            return new List<TroopSpawnInfo>(); // Return an empty list if no match is found
        }

        public static void Apply(int storyOption, int locationOption)
        {
            Console.WriteLine("Apply method started");
            StartType startOption = (StartType)storyOption;
            Hero mainHero = Hero.MainHero;
            Console.WriteLine($"Story option: {storyOption}, Location option: {locationOption}, Start option: {startOption}");
            Hero ruler = Hero.FindAll(hero => hero.Culture == mainHero.Culture && hero.IsAlive && hero.IsFactionLeader && !hero.MapFaction.IsMinorFaction).GetRandomElementInefficiently();
            Hero captor = Hero.FindAll(hero => hero.Culture == mainHero.Culture && hero.IsAlive && hero.MapFaction != null && !hero.MapFaction.IsMinorFaction && hero.IsPartyLeader && hero.PartyBelongedTo.DefaultBehavior != AiBehavior.Hold).GetRandomElementInefficiently();

            Settlement? startingSettlement = null;
            Settlement? ownedSettlement = null;
            GiveGoldAction.ApplyBetweenCharacters(mainHero, null, mainHero.Gold, true);
            mainHero.PartyBelongedTo.ItemRoster.Clear();
            switch (locationOption)
            {
                case 0:
                    startingSettlement = mainHero.HomeSettlement;
                    break;
                case 1:
                    startingSettlement = Settlement.FindAll(settlement => settlement.IsTown).GetRandomElementInefficiently();
                    break;
                case 2:
                    startingSettlement = Settlement.Find("town_A8");
                    break;
                case 3:
                    startingSettlement = Settlement.Find("town_B2");
                    break;
                case 4:
                    startingSettlement = Settlement.Find("town_EW2");
                    break;
                case 5:
                    startingSettlement = Settlement.Find("town_S2");
                    break;
                case 6:
                    startingSettlement = Settlement.Find("town_K4");
                    break;
                case 7:
                    startingSettlement = Settlement.Find("town_V3");
                    break;
                case 8: // only for castle start
                    startingSettlement = ownedSettlement = Settlement.All.Where(settlement => settlement.Culture == mainHero.Culture && settlement.IsCastle).GetRandomElementInefficiently();
                    break;
                case 10:
                    startingSettlement = Settlement.Find("town_G1");
                    break;
                case 11:
                    startingSettlement = Settlement.Find("town_A5");
                    break;
                case 12:
                    startingSettlement = Settlement.Find("town_ES1");
                    break;
                default:
                    break;
            }
            mainHero.PartyBelongedTo.Position2D = locationOption != 9 ? (startingSettlement != null ? startingSettlement.GatePosition : Settlement.Find("tutorial_training_field").Position2D) : captor.PartyBelongedTo.Position2D;
            if (GameStateManager.Current.ActiveState is MapState mapState)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
            Console.WriteLine($"Processing start option: {startOption}");
            switch (startOption)
            {

                case StartType.Default: // Default
                    Console.WriteLine("Processing Default start option");
                    ApplyInternal(mainHero, gold: 1000, grain: 2);
                    break;
                case StartType.Merchant: // Merchant
                                         // Dynamically retrieve troops for the merchant start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> merchantTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.Merchant);
                    ApplyInternal(mainHero, gold: 8000, grain: 250, mules: 25, troops: merchantTroops, startOption: StartType.Merchant);
                    break;
                case StartType.Exiled: // Exiled
                                       // Dynamically retrieve troops for the exiled start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> exiledTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.Exiled);
                    ApplyInternal(mainHero, gold: 3000, grain: 15, tier: 4, companions: 1, troops: exiledTroops, startOption: StartType.Exiled);
                    if (ruler != null)
                    {
                        ChangeCrimeRatingAction.Apply(ruler.MapFaction, 50, false);
                        CharacterRelationManager.SetHeroRelation(mainHero, ruler, -50);
                        foreach (Hero lord in Hero.FindAll(hero => hero.MapFaction == ruler.MapFaction && !hero.IsFactionLeader && hero.IsAlive))
                        {
                            CharacterRelationManager.SetHeroRelation(mainHero, lord, -10);
                        }
                    }
                    break;
                case StartType.Mercenary: // Mercenary
                                          // Dynamically retrieve troops for the mercenary start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> mercenaryTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.Mercenary);
                    ApplyInternal(mainHero, gold: 5000, grain: 25, tier: 2, troops: mercenaryTroops, startOption: StartType.Mercenary);
                    mainHero.PartyBelongedTo.RecentEventsMorale -= 40;
                    break;

                case StartType.Looter: // Looter
                                       // Dynamically retrieve troops for the looter start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> looterTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.Looter);
                    ApplyInternal(mainHero, gold: 500, grain: 10, troops: looterTroops, startOption: StartType.Looter);
                    foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
                    {
                        ChangeCrimeRatingAction.Apply(kingdom.MapFaction, 50, false);
                    }
                    break;

                case StartType.VassalNoFief: // VassalNoFief
                                             // Dynamically retrieve troops for the vassal no fief start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> vassalNoFiefTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.VassalNoFief);
                    ApplyInternal(mainHero, gold: 15000, grain: 40, tier: 3, troops: vassalNoFiefTroops, ruler: ruler, startOption: StartType.VassalNoFief);
                    break;

                case StartType.KingdomRuler: // KingdomRuler
                                             // Dynamically retrieve troops for the kingdom ruler start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> kingdomRulerTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.KingdomRuler);
                    ApplyInternal(mainHero, gold: 45000, grain: 150, tier: 5, troops: kingdomRulerTroops, companions: 3, companionParties: 2, startOption: StartType.KingdomRuler);
                    break;
                case StartType.CastleRuler: // Holding
                                            // Dynamically retrieve troops for the castle ruler start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> castleRulerTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.CastleRuler);
                    ApplyInternal(mainHero, gold: 60000, grain: 30, tier: 3, troops: castleRulerTroops, companions: 1, companionParties: 1, startingSettlement: startingSettlement, startOption: StartType.CastleRuler);
                    ownedSettlement ??= Settlement.All.Where(settlement => settlement.Culture == mainHero.Culture && settlement.IsCastle).GetRandomElementInefficiently();
                    break;

                case StartType.VassalFief: // Landed Vassal
                                           // Dynamically retrieve troops for the vassal fief start based on the hero's culture
                    Console.WriteLine("Processing Default start option");
                    List<TroopSpawnInfo> vassalFiefTroops = GetTroopsForStartOption(mainHero.Culture.StringId, StartType.VassalFief);
                    ApplyInternal(mainHero, gold: 35000, grain: 80, tier: 2, troops: vassalFiefTroops, companions: 1, companionParties: 1, ruler: ruler, startingSettlement: startingSettlement, startOption: StartType.VassalFief);
                    ownedSettlement ??= Settlement.All.Where(settlement => mainHero.Clan?.Kingdom == ruler.Clan?.Kingdom && settlement.IsCastle).GetRandomElementInefficiently();
                    break;

                case StartType.EscapedPrisoner: // Escaped Prisoner
                                                // No troops to assign for Escaped Prisoner, but applying other effects
                    Console.WriteLine("Processing Default start option");
                    ApplyInternal(mainHero, gold: 2000, grain: 10, startOption: StartType.EscapedPrisoner);
                    if (captor != null)
                    {
                        CharacterRelationManager.SetHeroRelation(mainHero, captor, -50);
                    }
                    break;

                default:
                    Console.WriteLine($"Unhandled start option: {startOption}");
                    break; // This line was missing a semicolon.
            }
            Console.WriteLine("Apply method completed");
            if (ownedSettlement != null)
                ChangeOwnerOfSettlementAction.ApplyByBarter(Hero.MainHero, ownedSettlement);
        }

        private static void ApplyInternal(Hero mainHero, int gold, int grain, int mules = 0, int tier = -1, List<TroopSpawnInfo>? troops = null, int companions = 0, int companionParties = 0, Hero? ruler = null, Settlement? startingSettlement = null, StartType startOption = StartType.Default)
        {
            GiveGoldAction.ApplyBetweenCharacters(null, mainHero, gold, true);
            mainHero.PartyBelongedTo.ItemRoster.AddToCounts(DefaultItems.Grain, grain);
            mainHero.PartyBelongedTo.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("mule"), mules);

            foreach (SkillObject skill in Skills.All)
            {
                mainHero.SetSkillValue(skill, (int)(mainHero.GetSkillValue(skill) * startingSkillMult[startOption]));
            }

            if (troops != null)
            {
                foreach (var troopInfo in troops)
                {
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopInfo.TroopId);
                    if (troop != null)
                    {
                        mainHero.PartyBelongedTo.AddElementToMemberRoster(troop, troopInfo.Quantity, false);
                    }
                }
            }

            for (int i = 0; i < companions; i++)
            {
                CharacterObject wanderer = (from character in CharacterObject.All
                                            where character.Occupation == Occupation.Wanderer && character.Culture == mainHero.Culture
                                            select character).GetRandomElementInefficiently();
                Settlement randomSettlement = (from settlement in Settlement.All
                                               where settlement.Culture == wanderer.Culture && settlement.IsTown
                                               select settlement).GetRandomElementInefficiently();
                Hero companion = HeroCreator.CreateSpecialHero(wanderer, randomSettlement, null, null, 33);

                companion.Clan = randomSettlement.OwnerClan;
                companion.ChangeState(Hero.CharacterStates.Active);
                if (startOption == StartType.KingdomRuler || startOption == StartType.CastleRuler || startOption == StartType.VassalFief) // gives companions noble equipment
                {
                    companion.BattleEquipment.FillFrom(Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge(companion, false)[0].AllEquipments.GetRandomElement());
                    companion.CivilianEquipment.FillFrom(Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge(companion, true)[0].AllEquipments.GetRandomElement());
                }
                AddCompanionAction.Apply(Clan.PlayerClan, companion);
                AddHeroToPartyAction.Apply(companion, mainHero.PartyBelongedTo, false);
                GiveGoldAction.ApplyBetweenCharacters(null, companion, 2000, true);
                if (i < companionParties)
                {
                    MobilePartyHelper.CreateNewClanMobileParty(companion, mainHero.Clan, out bool fromMainclan);
                }
            }
            if (ruler != null)
            {
                CharacterRelationManager.SetHeroRelation(mainHero, ruler, 10);
                ChangeKingdomAction.ApplyByJoinToKingdom(mainHero.Clan, ruler.Clan.Kingdom, false);
                mainHero.Clan.Influence = 10;
            }

            if (startOption == StartType.KingdomRuler || startOption == StartType.CastleRuler)
            {
                Campaign.Current.KingdomManager.CreateKingdom(mainHero.Clan.Name, mainHero.Clan.InformalName, mainHero.Clan.Culture, mainHero.Clan);
                mainHero.Clan.Influence = 100;
            }
        }

        public static bool MatchWildcardString(String pattern, String input)
        {
            if (String.Compare(pattern, input) == 0)
            {
                return true;
            }
            else if (String.IsNullOrEmpty(input))
            {
                if (String.IsNullOrEmpty(pattern.Trim(new Char[] { '*' }))) // Minor correction: Ensure the array syntax is correct.
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (pattern.Length == 0)
            {
                return false;
            }
            else if (pattern[0] == '*')
            {
                if (MatchWildcardString(pattern.Substring(1), input))
                {
                    return true;
                }
                else
                {
                    return MatchWildcardString(pattern, input.Substring(1));
                }
            }
            else if (pattern[pattern.Length - 1] == '*')
            {
                if (MatchWildcardString(pattern.Substring(0, pattern.Length - 1), input))
                {
                    return true;
                }
                else
                {
                    return MatchWildcardString(pattern, input.Substring(0, input.Length - 1));
                }
            }
            else if (pattern[0] == input[0])
            {
                return MatchWildcardString(pattern.Substring(1), input.Substring(1));
            }
            return false;
        }
    }
}