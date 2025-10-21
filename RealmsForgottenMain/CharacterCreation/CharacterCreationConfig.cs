using RealmsForgotten.Managers;
using System.Collections;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade.Diamond;
using static RealmsForgotten.Globals;

namespace RealmsForgotten.CharacterCreation
{
    public class CharacterCreationConfig
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
                ["west_realm"] = "player_char_creation_default",
                ["mage"] = "player_char_creation_default",
                ["dwarf"] = "player_char_creation_default",
                ["urkhai"] = "player_char_creation_default",
                ["wulf"] = "player_char_creation_default",
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
                ["west_realm"] = "merchant_start_empire",
                ["mage"] = "merchant_start_empire",
                ["dwarf"] = "merchant_start_sturgia",
                ["urkhai"] = "merchant_start_sturgia",
                ["wulf"] = "merchant_start_empire",
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
                ["south_realm"] = "rf_exiled_equip",
                ["west_realm"] = "rf_exiled_equip",
                ["mage"] = "rf_exiled_equip",
                ["dwarf"] = "rf_exiled_equip",
                ["urkhai"] = "rf_exiled_equip",
                ["wulf"] = "rf_exiled_equip",
            },
            [StartType.Mistic] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_athas_mistic",
                ["empire"] = "rf_empire_mistic",
                ["khuzait"] = "rf_khuzait_mistic",
                ["sturgia"] = "rf_sturgia_mistic",
                ["battania"] = "rf_elvean_mistic",
                ["vlandia"] = "rf_nasoria_mistic",
                ["giant"] = "rf_giant_mistic",
                ["aqarun"] = "rf_aqarun_mistic",
                ["south_realm"] = "rf_empire_mistic",
                ["west_realm"] = "rf_empire_mistic",
                ["mage"] = "rf_empire_mistic",
                ["dwarf"] = "rf_sturgia_mistic",
                ["urkhai"] = "rf_sturgia_mistic",
                ["wulf"] = "rf_khuzait_mistic",
            },
            [StartType.Outlaw] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_looter",
                ["empire"] = "rf_looter",
                ["khuzait"] = "rf_looter",
                ["sturgia"] = "rf_looter",
                ["battania"] = "rf_looter",
                ["vlandia"] = "rf_looter",
                ["giant"] = "rf_looter",
                ["aqarun"] = "rf_looter",
                ["south_realm"] = "rf_looter",
                ["west_realm"] = "rf_looter",
                ["mage"] = "rf_looter",
                ["dwarf"] = "rf_looter",
                ["urkhai"] = "rf_looter",
                ["wulf"] = "rf_looter",
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
                ["west_realm"] = "merc_realms_start",
                ["mage"] = "merc_realms_start",
                ["dwarf"] = "merc_dwarf_start",
                ["urkhai"] = "merc_urkhai_start",
                ["wulf"] = "merc_allkhuur_start",
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
                ["west_realm"] = "realms_vassal_nofief",
                ["mage"] = "realms_vassal_nofief",
                ["dwarf"] = "dwarf_vassal_nofief",
                ["urkhai"] = "urkhai_vassal_nofief",
                ["wulf"] = "vassal_nofief_wulf_start",
            },
            [StartType.Knight] = new Dictionary<string, string>
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
                ["west_realm"] = "realms_vassal_nofief",
                ["mage"] = "realms_vassal_nofief",
                ["dwarf"] = "dwarf_vassal_ursurper",
                ["urkhai"] = "urkhai_vassal_ursurper",
                ["wulf"] = "knight_wulf_start",
            },
            [StartType.King] = new Dictionary<string, string>
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
                ["west_realm"] = "king_realms_start",
                ["mage"] = "king_realms_start",
                ["dwarf"] = "king_dwarf_start",
                ["urkhai"] = "king_urkhai_start",
                ["wulf"] = "king_wulf_start",
            },
            [StartType.Usurper] = new Dictionary<string, string>
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
                ["west_realm"] = "vassal_realms_start",
                ["mage"] = "vassal_realms_start",
                ["dwarf"] = "vassal_vortiak_start",
                ["urkhai"] = "vassal_urkhai_start",
                ["wulf"] = "vassal_wulf_start",
            }
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

            { ("aserai", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_recruit", 2) }},

            { ("aserai", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_bandit", 8) }},

            { ("aserai", StartType.Mercenary), new List<TroopSpawnInfo> {
              new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("aserai", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_veteran_infantry", 10),
                new TroopSpawnInfo("aserai_master_archer", 5),
                new TroopSpawnInfo("aserai_mameluke", 3) }},

            { ("aserai", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_veteran_infantry", 20),
                new TroopSpawnInfo("aserai_master_archer", 10),
                new TroopSpawnInfo("aserai_mameluke", 10) }},

            { ("aserai", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("aserai_veteran_infantry", 15),
                new TroopSpawnInfo("aserai_master_archer", 10),
                new TroopSpawnInfo("aserai_mameluke", 5) }},

            { ("aserai", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("empire", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("empire", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("empire", StartType.Mercenary), new List<TroopSpawnInfo> {
               new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("empire", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 3) }},

            { ("empire", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 20),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 10) }},

            { ("empire", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 15),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 5) }},

            { ("empire", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("battania", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_volunteer", 2) }},

            { ("battania", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("battania", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("battania", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_veteran_falxman", 10),
                new TroopSpawnInfo("battanian_fian_champion", 5),
                new TroopSpawnInfo("battanian_hero", 3) }},

            { ("battania", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_veteran_falxman", 20),
                new TroopSpawnInfo("battanian_fian_champion", 10),
                new TroopSpawnInfo("battanian_hero", 10) }},

            { ("battania", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("battanian_veteran_falxman", 15),
                new TroopSpawnInfo("battanian_fian_champion", 10),
                new TroopSpawnInfo("battanian_hero", 5) }},

            { ("battania", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("sturgia", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("sturgia", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("sturgia", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("sturgia", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgian_veteran_warrior", 10),
                new TroopSpawnInfo("sturgia_hardened_brigand", 5),
                new TroopSpawnInfo("sturgia_druzhinnik", 3) }},

            { ("sturgia", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgia_veteran_warrior", 20),
                new TroopSpawnInfo("sturgian_shock_troop", 10),
                new TroopSpawnInfo("sturgian_ulfhednar", 10),
                new TroopSpawnInfo("sturgian_druzhinnik", 10) }},

            { ("sturgia", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("sturgian_veteran_warrior", 15),
                new TroopSpawnInfo("sturgian_shock_troop", 10),
                new TroopSpawnInfo("druzhinnik", 5) }},

            { ("sturgia", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("khuzait", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_nomad", 2) }},

            { ("khuzait", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("khuzait", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("khuzait", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_darkhan", 10),
                new TroopSpawnInfo("khuzait_marksman", 5),
                new TroopSpawnInfo("khuzait_heavy_lancer", 3) }},

            { ("khuzait", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_darkhan", 20),
                new TroopSpawnInfo("khuzait_marksman", 10),
                new TroopSpawnInfo("khuzait_heavy_lancer", 10) }},

            { ("khuzait", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("khuzait_darkhan", 15),
                new TroopSpawnInfo("khuzait_marksman", 10),
                new TroopSpawnInfo("khuzait_heavy_lancer", 5) }},

            { ("khuzait", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("vlandia", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("vlandia", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("vlandia", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_infantry", 5),
                new TroopSpawnInfo("vlandian_crossbowman", 5),
                new TroopSpawnInfo("vlandian_knight", 2) }},

            { ("vlandia", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_sergeant", 10),
                new TroopSpawnInfo("vlandian_sharpshooter", 5),
                new TroopSpawnInfo("vlandian_banner_knight", 3) }},

            { ("vlandia", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_sergeant", 20),
                new TroopSpawnInfo("vlandian_sharpshooter", 10),
                new TroopSpawnInfo("vlandian_banner_knight", 10) }},

            { ("vlandia", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("vlandian_sergeant", 15),
                new TroopSpawnInfo("vlandian_sharpshooter", 10),
                new TroopSpawnInfo("vlandian_banner_knight", 5) }},

            { ("vlandia", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("giant", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("giant", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 6) }},

            { ("giant", StartType.Mercenary), new List<TroopSpawnInfo> {
               new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("giant", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("giant_trained_infantry", 5),
                new TroopSpawnInfo("giant_experienced_infantry", 3),
                new TroopSpawnInfo("half_giant_archer", 2) }},

            { ("giant", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("giant_trained_infantry", 10),
                new TroopSpawnInfo("giant_experienced_infantry", 5),
                new TroopSpawnInfo("giant_berzerker_infantry", 5) }},

            { ("giant", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("giant_trained_infantry", 8),
                new TroopSpawnInfo("giant_experienced_infantry", 5),
                new TroopSpawnInfo("giant_leader_archer", 2) }},

            { ("giant", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("aqarun", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("aqarun", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 6) }},

            { ("aqarun", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("athas_arena_fighter_a", 5),
                new TroopSpawnInfo("athas_arena_ranged_a", 3) }},

            { ("aqarun", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_infantry", 5),
                new TroopSpawnInfo("Aqarun_warrior", 3),
                new TroopSpawnInfo("Aqarun_skirmisher_trained", 2) }},

            { ("aqarun", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_warrior", 8),
                new TroopSpawnInfo("Aqarun_champion", 4),
                new TroopSpawnInfo("Aqarun_skirmisher_expert", 2) }},

            { ("aqarun", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("Aqarun_infantry", 6),
                new TroopSpawnInfo("Aqarun_cavalry_veteran", 4),
                new TroopSpawnInfo("Aqarun_skirmisher_veteran", 2) }},

            { ("aqarun", StartType.Knight), new List<TroopSpawnInfo> {
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

            { ("south_realm", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("south_realm", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("south_realm", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("south_realm", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                 new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 3) }},

            { ("south_realm", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 20),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 10) }},

            { ("south_realm", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 15),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 5) }},

            { ("south_realm", StartType.Knight), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 5) }},
            { ("west_realm", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 15),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 5) }},
            { ("west_realm", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},
            { ("west_realm", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_recruit", 4),
                new TroopSpawnInfo("imperial_archer", 2) }},
            { ("west_realm", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 20),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 10) }},
            { ("west_realm", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},
            { ("west_realm", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},
            { ("west_realm", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("empire_trader", 4),
                new TroopSpawnInfo("empire_recruit", 8),
                new TroopSpawnInfo("empire_archer", 4) }},
            { ("west_realm", StartType.Knight), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 5) }},
            { ("west_realm", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 3) }},
            { ("mage", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 15),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 5) }},
            { ("mage", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},
            { ("mage", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_recruit", 4),
                new TroopSpawnInfo("imperial_archer", 2) }},
            { ("mage", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 20),
                new TroopSpawnInfo("imperial_palatine_guard", 10),
                new TroopSpawnInfo("imperial_cataphract", 10) }},
            { ("mage", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},
            { ("mage", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},
            { ("mage", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("empire_trader", 4),
                new TroopSpawnInfo("empire_recruit", 8),
                new TroopSpawnInfo("empire_archer", 4) }},
            { ("mage", StartType.Knight), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 5) }},
            { ("mage", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("imperial_legionary", 10),
                new TroopSpawnInfo("imperial_palatine_guard", 5),
                new TroopSpawnInfo("imperial_cataphract", 3) }},

             { ("dwarf", StartType.Merchant), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("merchant_dwarf", 4),
                new TroopSpawnInfo("dwarf_troop", 8),
                new TroopSpawnInfo("dwarf_militia_veteran_archer", 4) }},

            { ("dwarf", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_troop", 4),
                new TroopSpawnInfo("dwarf_levy_crossbowman", 2) }},

            { ("dwarf", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_troop", 5) }},

            { ("dwarf", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_looter", 8) }},

            { ("dwarf", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_mercenary_volunteer", 10) }},

            { ("dwarf", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_heavy_spearman", 10),
                new TroopSpawnInfo("dwarf_hardened_crossbowman", 5),
                new TroopSpawnInfo("dwarf_exp_cavalry", 3) }},

            { ("dwarf", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_veteran_warrior", 20),
                new TroopSpawnInfo("dwarf_sharpshooter", 10),
                new TroopSpawnInfo("dugrast_druzhinnik", 10) }},

            { ("dwarf", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_heavy_spearman", 15),
                new TroopSpawnInfo("dwarf_hardened_crossbowman", 10),
                new TroopSpawnInfo("dwarf_exp_cavalry", 5) }},

            { ("dwarf", StartType.Knight), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dwarf_heavy_spearman", 10),
                new TroopSpawnInfo("dwarf_hardened_crossbowman", 5),
                new TroopSpawnInfo("dugrast_druzhinnik", 5) }},
            { ("urkhai", StartType.Merchant), new List<TroopSpawnInfo> {
                 new TroopSpawnInfo("merchant_soldier", 4),
                 new TroopSpawnInfo("merchant_archer", 10),
                new TroopSpawnInfo("merchant_trained_infantry", 4) }},

            { ("urkhai", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("urkhai_troop", 5),
                new TroopSpawnInfo("urkhai_archer", 2) }},

            { ("urkhai", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("urkhai", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("urkhai", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("urkhai_mercenary_volunteer", 10) }},

            { ("urkhai", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("urkhai_veteran_infantry", 10),
                new TroopSpawnInfo("urkhai_trained_archer", 5),
                new TroopSpawnInfo("urkhai_cavalry", 3) }},

            { ("urkhai", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("urkhai_veteran_infantry", 20),
                new TroopSpawnInfo("urkhai_veteran_archer", 10),
                new TroopSpawnInfo("uruk_hai_veteran_infantry", 10),
                new TroopSpawnInfo("urkhai_veteran_cavalry", 10) }},

            { ("urkhai", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("urkhai_veteran_infantry", 15),
                new TroopSpawnInfo("urkhai_veteran_archer", 10),
                new TroopSpawnInfo("urkhai_cavalry", 5) }},

            { ("urkhai", StartType.Knight), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("urkhai_veteran_infantry", 10),
                new TroopSpawnInfo("urkhai_veteran_archer", 5),
                new TroopSpawnInfo("urkhai_cavalry", 5) }},

             { ("wulf", StartType.Exiled), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("wulf_troop", 5),
                new TroopSpawnInfo("wulf_archer", 2) }},

            { ("wulf", StartType.Mistic), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 2) }},

            { ("wulf", StartType.Outlaw), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("looter", 8) }},

            { ("wulf", StartType.Mercenary), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("mercenary_volunteer", 10) }},

            { ("wulf", StartType.VassalNoFief), new List<TroopSpawnInfo> {
                 new TroopSpawnInfo("wulf_infantry", 10),
                new TroopSpawnInfo("wulf_archer", 5),
                new TroopSpawnInfo("dunland_skirmisher", 3) }},

            { ("wulf", StartType.King), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("wulf_militia_veteran_archer", 15),
                new TroopSpawnInfo("dunland_medium_axemen", 20),
                new TroopSpawnInfo("dunland_medium_spearmen", 10),
                new TroopSpawnInfo("dunland_heavy_axemen", 10),
                new TroopSpawnInfo("dunland_skirmisher", 10),
                new TroopSpawnInfo("wulf_raider", 10) }},

            { ("wulf", StartType.Usurper), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("wulf_militia_veteran_archer", 15),
                new TroopSpawnInfo("dunland_medium_spearmen", 10),
                new TroopSpawnInfo("dunland_skirmisher", 5) }},

            { ("wulf", StartType.Knight), new List<TroopSpawnInfo> {
                new TroopSpawnInfo("dunland_light_axemen", 10),
                new TroopSpawnInfo("wulf_militia_veteran_archer", 5),
                new TroopSpawnInfo("dunland_skirmisher", 5) }},
            };

        public static List<TroopSpawnInfo> GetTroopsForStartOption(string culture, StartType startOption)
        {
            if (CultureStartTypeToTroops.TryGetValue((culture, startOption), out var troopList))
            {
                return troopList;
            }
            return new List<TroopSpawnInfo>(); // Return an empty list if no match is found
        }
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
        public class CustomStartData
        {
            public int Gold { get; set; }
            public int Grain { get; set; }
            public int Mules { get; set; }
            public int ClanTier { get; set; }
            public int NumberOfCompanions { get; set; }
            public int CompanionParties { get; set; }
            public CustomStartData(int gold, int grain, int mules = 0, int clanTier = 0, int numberOfCompanions = 0, int companionParties = 0)
            {
                Gold = gold;
                Grain = grain;
                Mules = mules;
                ClanTier = clanTier;
                NumberOfCompanions = numberOfCompanions;
                CompanionParties = companionParties;
            }
            public static readonly Dictionary<StartType, CustomStartData> All = new()
            {
                [StartType.Default] = new CustomStartData(1000, 2),
                [StartType.Merchant] = new CustomStartData(8000, 250, 25),
                [StartType.Exiled] = new CustomStartData(3000, 15, 5, 4, 1),
                [StartType.Mercenary] = new CustomStartData(5000, 25, 5, 2),
                [StartType.Outlaw] = new CustomStartData(500, 10),
                [StartType.VassalNoFief] = new CustomStartData(15000, 40, 5, 3),
                [StartType.King] = new CustomStartData(45000, 150, 20, 5, 3, 2),
                [StartType.Usurper] = new CustomStartData(60000, 30, 10, 3, 2, 1),
                [StartType.Knight] = new CustomStartData(35000, 80, 5, 2, 1, 1),
                [StartType.Mistic] = new CustomStartData(3000, 10),
            };
        }
        public static string GetBodyPropertiesFromCulture(string culture)
        {
            if (cultureToBodyProperties.TryGetValue(culture, out string value))
                return value;
            return HumanBodyPropString;
        }
        static readonly Dictionary<string, string> cultureToBodyProperties = new()
        {
            ["aserai"] = AthasBodyPropString,
            ["battania"] = ElveanBodyPropString,
            ["empire"] = HumanBodyPropString,
            ["khuzait"] = AllKhuurBodyPropString,
            ["sturgia"] = UndeadBodyPropString,
            ["vlandia"] = NasoriaBodyPropString,
            ["giant"] = XilantlacayBodyPropString,
            ["aqarun"] = AqarunBodyPropString,
            ["mage"] = HumanBodyPropString,
            ["dwarf"] = DwarfBodyPropString,
            ["urkhai"] = HumanBodyPropString,
            ["wulf"] = WulfBodyPropString
        };
        public static int GetRaceIdFromCulture(string culture)
        {
            if (cultureToRace.TryGetValue(culture, out string raceName))
            {
                return RaceManager.Instance.GetRaceIdFromName(raceName) ;
            }
            return 0;
        }

        static readonly Dictionary<string, string> cultureToRace = new()
        {
            ["battania"] = "elvean",
            ["sturgia"] = "undead",
            ["giant"] = "Xilantlacay",
            ["dwarf"] = "dwarf",
            ["urkhai"] = "urkhai"
        };
        const string AthasBodyPropString = "<BodyProperties version=\"4\" age=\"22.23\" weight=\"0.0448\" build=\"0.6065\"  key=\"003FB40FCE001016AF9E6DFC6B0756871FF2FD9D8031BB1327CCC0244CAB9C060069160306EC96D8000000000000000000000000000000000000000010CC1004\"  />";
        const string NasoriaBodyPropString = "<BodyProperties version=\"4\" age=\"40\" weight=\"0.8288\" build=\"0.4213\"  key=\"001EAC0B80000004FFC53FE76E83CCEA36A3EC6D8174DF4070129ADF3E13E54B0366C6350684B8A7000000000000000000000000000000000000000026CC7002\"  />";
        const string AllKhuurBodyPropString = "<BodyProperties version=\"4\" age=\"22.49\" weight=\"0.9599\" build=\"0.3611\"  key=\"001EF80D8000200AB8708BB6CDC85229D3698B3ABDFE344CD22D3DD5388988680355E6350596723B0000000000000000000000000000000000000000609C1005\"  />";
        const string ElveanBodyPropString = "<BodyProperties version=\"4\" age=\"22.49\" weight=\"0.0262\" build=\"0.5108\"  key=\"00000400000000038788080F07757777F0F887F8F88008888E068A89808D80060078060307883F10000000000000000000000000000000000000000052F47145\"  />";
        const string HumanBodyPropString = "<BodyProperties version=\"4\" age=\"22.35\" weight=\"0.5417\" build=\"0.5231\"  key=\"000DF00FC00033CD8771188F38770F8801F188778888888888888888546AF0F90088860308888888000000000000000000000000000000000000000043044144\"  />";
        const string UndeadBodyPropString = "<BodyProperties version=\"4\" age=\"40\" weight=\"0.2978\" build=\"0.9522\"  key=\"000004001900178D18E0788057F760886F8707E84EA8E18174414A490D1100E803BE46350BA7B7A50000000000000000000000000000000000000000016430C6\"  />";
        const string AqarunBodyPropString = "<BodyProperties version=\"4\" age=\"22.2\" weight=\"0.3272\" build=\"0.6343\"  key=\"003FF00997001019BFEBEF53ADA8CB8B1FFDFD063C34C704EEFCE0BD50AF939F009A560309FCF9B80000000000000000000000000000000000000000112C9002\"  />";
        const string XilantlacayBodyPropString = "<BodyProperties version=\"4\" age=\"22.2\" weight=\"0.3272\" build=\"0.6343\"  key=\"003458078000200AFDAECE6F0BB44F0EF5F1DEFEDAA6B1818E66E1EE818DF07A007A560307E84F31000000000000000000000000000000000000000052F43142\"  />";
        const string DwarfBodyPropString = "<BodyProperties version=\"4\" age=\"22.2\" weight=\"1\" build=\"0.9954\"  key=\"002BB00780003A50FFEFFFFEEEFEF00FFEF5EFA74756E898FFFCF80E516FFEFF003FF60303EFBE9E0000000000000000000000000000000000000000105C9142\"  />";
        const string WulfBodyPropString = "<BodyProperties version=\"4\" age=\"22.03\" weight=\"0.4738\" build=\"1\"  key=\"0029B00FC000140AE6DC6DFD98ECBF8901516F289FAC23B49FD822F795D3F08900CF76030CFEEDAA0000000000000000000000000000000000000000439C3142\"  />";

        public static List<string> PlayerSelectableCultures = new()
        {
            "aserai", "battania", "empire", "khuzait", "sturgia", "vlandia", // DO NOT REMOVE, in Bannelord 1.3 beta removing one of the default cultures causes a crash
            "urkhai", "wulf"
        };
    }
}
