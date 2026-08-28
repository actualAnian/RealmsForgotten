using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace RealmsForgotten.Raiding
{
    /// <summary>
    /// Pagina MCM do sistema de saque. Todo numero que o More Raiding fixava no
    /// codigo vira opcao aqui (pedido do autor 2026-08-27), inclusive o bloco de
    /// consequencias pesadas, que fica DESLIGADO por padrao.
    /// Leitura em runtime sempre pelo <see cref="RFRaidConfig"/>, que aguenta o
    /// MCM ausente.
    /// </summary>
    public sealed class RFRaidSettings : AttributeGlobalSettings<RFRaidSettings>
    {
        public override string Id => "RF_Raiding";
        public override string DisplayName => "RF Raiding";
        public override string FolderName => "RF_Raiding";
        public override string FormatType => "json";

        private const string GroupGeneral = "{=rf_raid_g_general}Raiding on Foot";
        private const string GroupResponse = "{=rf_raid_g_response}Defenders";
        private const string GroupArson = "{=rf_raid_g_arson}Arson";
        private const string GroupNight = "{=rf_raid_g_night}Torches at Night";
        private const string GroupBanner = "{=rf_raid_g_banner}Black Banner";
        private const string GroupGrim = "{=rf_raid_g_grim}Grim Consequences";

        [SettingPropertyBool("{=rf_raid_enable}Enable raiding on foot", RequireRestart = false,
            HintText = "{=rf_raid_enable_d}Lets you sack a village or town from inside the scene: strike a civilian and the raid begins. Turn off to restore vanilla behaviour completely.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool EnableSceneRaids { get; set; } = true;

        [SettingPropertyInteger("{=rf_raid_troops}Troops you bring into the scene", 0, 60, "0", RequireRestart = false,
            HintText = "{=rf_raid_troops_d}Upper limit of soldiers spawned at your side when the raid starts. Roguery and indoor/outdoor still reduce it. Default 30.")]
        [SettingPropertyGroup(GroupGeneral)]
        public int MaxTroopsInScene { get; set; } = 30;

        [SettingPropertyBool("{=rf_raid_orders}Battle orders in settlement scenes", RequireRestart = true,
            HintText = "{=rf_raid_orders_d}Adds the formation order UI to village and town scenes so you can command the troops you brought. Applies on the next game start.")]
        [SettingPropertyGroup(GroupGeneral)]
        public bool EnableOrderUI { get; set; } = true;

        [SettingPropertyInteger("{=rf_raid_militia_v}Village militia response (s)", 5, 180, "0", RequireRestart = false,
            HintText = "{=rf_raid_militia_v_d}Seconds before the village militia reaches you. Default 35.")]
        [SettingPropertyGroup(GroupResponse)]
        public int MilitiaResponseVillage { get; set; } = 35;

        [SettingPropertyInteger("{=rf_raid_militia_t}Town garrison response (s)", 5, 180, "0", RequireRestart = false,
            HintText = "{=rf_raid_militia_t_d}Seconds before the town garrison hits the streets. Default 30.")]
        [SettingPropertyGroup(GroupResponse)]
        public int MilitiaResponseTown { get; set; } = 30;

        [SettingPropertyBool("{=rf_raid_hostage}Captured when you fall", RequireRestart = false,
            HintText = "{=rf_raid_hostage_d}Being knocked out during a raid puts you in the settlement's prison instead of ending the mission. You escape over time, sooner with high Roguery.")]
        [SettingPropertyGroup(GroupResponse)]
        public bool EnableHostageCapture { get; set; } = true;

        [SettingPropertyBool("{=rf_night_enable}Torches at night", RequireRestart = false,
            HintText = "{=rf_night_enable_d}After 20:00 and before 04:00, soldiers in battle and townsfolk in the streets carry lit torches. Applies from the next scene you enter.")]
        [SettingPropertyGroup(GroupNight)]
        public bool EnableNightTorches { get; set; } = true;

        [SettingPropertyInteger("{=rf_night_foot}One torch per N foot soldiers", 0, 60, "0", RequireRestart = false,
            HintText = "{=rf_night_foot_d}Infantry and archers. 0 turns them off. Default 15.")]
        [SettingPropertyGroup(GroupNight)]
        public int NightTorchRatioFoot { get; set; } = 15;

        [SettingPropertyInteger("{=rf_night_mounted}One torch per N riders", 0, 60, "0", RequireRestart = false,
            HintText = "{=rf_night_mounted_d}Cavalry and horse archers ride more spread out, so they carry more light. 0 turns them off. Default 9.")]
        [SettingPropertyGroup(GroupNight)]
        public int NightTorchRatioMounted { get; set; } = 9;

        [SettingPropertyInteger("{=rf_night_civ}One torch per N townsfolk", 0, 30, "0", RequireRestart = false,
            HintText = "{=rf_night_civ_d}People walking the streets of a village or town at night. 0 leaves the streets dark. Default 4.")]
        [SettingPropertyGroup(GroupNight)]
        public int NightTorchRatioCivilian { get; set; } = 4;

        [SettingPropertyBool("{=rf_night_player}Give yourself a torch", RequireRestart = false,
            HintText = "{=rf_night_player_d}You receive a torch when entering a village or town at night.")]
        [SettingPropertyGroup(GroupNight)]
        public bool NightTorchForPlayer { get; set; } = true;

        [SettingPropertyBool("{=rf_raid_arson}Enable arson", RequireRestart = false,
            HintText = "{=rf_raid_arson_d}Gives you throwing torches when a raid begins. Where a torch lands, fire takes hold, grows, spreads and burns whoever walks through it.")]
        [SettingPropertyGroup(GroupArson)]
        public bool EnableArson { get; set; } = true;

        [SettingPropertyBool("{=rf_raid_arson_troops}Your soldiers set fires too", RequireRestart = false,
            HintText = "{=rf_raid_arson_troops_d}While the raid runs, your troops torch what is around them, so the settlement burns without you having to aim at every roof.")]
        [SettingPropertyGroup(GroupArson)]
        public bool EnableTroopArson { get; set; } = true;

        [SettingPropertyBool("{=rf_raid_bb}Enable black banner attacks", RequireRestart = false,
            HintText = "{=rf_raid_bb_d}Lets you attack caravans with your identity hidden: no hostility, no crime, and your troops fight under a black banner. Success depends on Roguery.")]
        [SettingPropertyGroup(GroupBanner)]
        public bool EnableBlackBanner { get; set; } = true;

        [SettingPropertyInteger("{=rf_raid_bb_max}Maximum party size", 5, 200, "0", RequireRestart = false,
            HintText = "{=rf_raid_bb_max_d}A host this large cannot pass unnoticed. Above this many healthy troops the option is refused. Default 45.")]
        [SettingPropertyGroup(GroupBanner)]
        public int BlackBannerMaxTroops { get; set; } = 45;

        [SettingPropertyInteger("{=rf_raid_bb_skill}Roguery for a certain success", 20, 300, "0", RequireRestart = false,
            HintText = "{=rf_raid_bb_skill_d}Roguery level at which hiding your identity never fails. The chance is your Roguery divided by this. Default 111.")]
        [SettingPropertyGroup(GroupBanner)]
        public int BlackBannerCertaintySkill { get; set; } = 111;

        [SettingPropertyBool("{=rf_raid_grim}Grim consequences", RequireRestart = false,
            HintText = "{=rf_raid_grim_d}OFF by default. When on, the game counts the defenceless killed during a raid and holds it against your Mercy trait, as the original mod did. Leave off for a lighter tone.")]
        [SettingPropertyGroup(GroupGrim)]
        public bool EnableGrimConsequences { get; set; } = false;
    }

    /// <summary>Leitura tolerante: se o MCM nao estiver presente, valem os padroes.</summary>
    internal static class RFRaidConfig
    {
        private static RFRaidSettings S => RFRaidSettings.Instance;

        public static bool SceneRaidsEnabled => S?.EnableSceneRaids ?? true;
        public static int MaxTroopsInScene => S?.MaxTroopsInScene ?? 30;
        public static bool OrderUIEnabled => S?.EnableOrderUI ?? true;
        public static int MilitiaResponseVillage => S?.MilitiaResponseVillage ?? 35;
        public static int MilitiaResponseTown => S?.MilitiaResponseTown ?? 30;
        public static bool HostageCaptureEnabled => S?.EnableHostageCapture ?? true;
        public static bool NightTorchesEnabled => S?.EnableNightTorches ?? true;
        public static int NightTorchRatioFoot => S?.NightTorchRatioFoot ?? 15;
        public static int NightTorchRatioMounted => S?.NightTorchRatioMounted ?? 9;
        public static int NightTorchRatioCivilian => S?.NightTorchRatioCivilian ?? 4;
        public static bool NightTorchForPlayer => S?.NightTorchForPlayer ?? true;
        public static bool ArsonEnabled => S?.EnableArson ?? true;
        public static bool TroopArsonEnabled => S?.EnableTroopArson ?? true;
        public static bool BlackBannerEnabled => S?.EnableBlackBanner ?? true;
        public static int BlackBannerMaxTroops => S?.BlackBannerMaxTroops ?? 45;
        public static int BlackBannerCertaintySkill => S?.BlackBannerCertaintySkill ?? 111;
        public static bool GrimConsequencesEnabled => S?.EnableGrimConsequences ?? false;
    }
}
