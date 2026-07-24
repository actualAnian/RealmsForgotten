using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace RealmsForgotten.Diagnostics
{
    /// <summary>
    /// "RF Diagnostics" MCM page: one switch per diagnostic file, all OFF by
    /// default, plus a master switch. Nothing here changes gameplay — it only
    /// decides what gets written to Documents\...\Configs\ModLogs, so a player
    /// never pays disk for diagnostics they did not ask for. Each file is also
    /// capped and rotated once (see <see cref="RFLogSwitchboard"/>).
    ///
    /// Reading is null-safe: with MCM missing or incompatible everything reads
    /// as OFF instead of throwing.
    /// </summary>
    public class RFLogSettings : AttributeGlobalSettings<RFLogSettings>
    {
        public override string Id => "RF_Logs";
        public override string DisplayName => new TextObject("{=RF_Logs_Name}RF Diagnostics").ToString();
        public override string FolderName => "RF_Logs";
        public override string FormatType => "xml";

        private const string GroupGeneral = "{=RF_Logs_Group_General}General";
        private const string GroupWorld = "{=RF_Logs_Group_World}World & Kingdoms";
        private const string GroupSystems = "{=RF_Logs_Group_Systems}Mod Systems";
        private const string GroupBattle = "{=RF_Logs_Group_Battle}Battle AI";

        [SettingPropertyBool("{=RF_Logs_Master_Name}Enable diagnostic logs", Order = 0, RequireRestart = false,
            HintText = "{=RF_Logs_Master_Hint}Master switch. While this is off NOTHING is written, whatever the individual switches say. Turn it on only while investigating an issue — logs are written to Documents/Mount and Blade II Bannerlord/Configs/ModLogs and each file is capped at 8 MB (one rotation kept).")]
        [SettingPropertyGroup(GroupGeneral, GroupOrder = 0)]
        public bool MasterEnabled { get; set; } = false;

        // ── World & kingdoms ─────────────────────────────────────────────────
        [SettingPropertyBool("{=RF_Logs_KingdomObjectives}Kingdom grand designs", Order = 0, RequireRestart = false,
            HintText = "{=RF_Logs_KingdomObjectives_Hint}RF_KingdomObjectives.log — per realm per day: design progress, pressure, chosen target and which gate is blocking a war declaration.")]
        [SettingPropertyGroup(GroupWorld, GroupOrder = 1)]
        public bool KingdomObjectives { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_WarSystem}War director", Order = 1, RequireRestart = false,
            HintText = "{=RF_Logs_WarSystem_Hint}RF_WarSystemTrace.log — target focus, fronts, phases and special war/peace requests. Verbose.")]
        [SettingPropertyGroup(GroupWorld, GroupOrder = 1)]
        public bool WarSystemTrace { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_CampaignAi}Campaign AI trace", Order = 2, RequireRestart = false,
            HintText = "{=RF_Logs_CampaignAi_Hint}RF_CampaignAITrace.log — party-level AI decisions. VERY verbose (this one reached 10 MB in testing).")]
        [SettingPropertyGroup(GroupWorld, GroupOrder = 1)]
        public bool CampaignAiTrace { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_FactionEconomy}Faction economy index", Order = 3, RequireRestart = false,
            HintText = "{=RF_Logs_FactionEconomy_Hint}RF_IEF.log — daily economic index per kingdom.")]
        [SettingPropertyGroup(GroupWorld, GroupOrder = 1)]
        public bool FactionEconomy { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_YoungWorld}Young World", Order = 4, RequireRestart = false,
            HintText = "{=RF_Logs_YoungWorld_Hint}RF_YoungWorld.log — what the Young World pass did to walls and prosperity at campaign creation.")]
        [SettingPropertyGroup(GroupWorld, GroupOrder = 1)]
        public bool YoungWorld { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_Capitulation}Capitulation audit", Order = 5, RequireRestart = false,
            HintText = "{=RF_Logs_Capitulation_Hint}RF_CapitulationAudit.log — why realms did or did not capitulate.")]
        [SettingPropertyGroup(GroupWorld, GroupOrder = 1)]
        public bool Capitulation { get; set; } = false;

        // ── Mod systems ──────────────────────────────────────────────────────
        [SettingPropertyBool("{=RF_Logs_ResourceZones}Resource zones", Order = 0, RequireRestart = false,
            HintText = "{=RF_Logs_ResourceZones_Hint}RF_ResourceZones.log — zone ownership, caravans, depletion.")]
        [SettingPropertyGroup(GroupSystems, GroupOrder = 2)]
        public bool ResourceZones { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_Settlers}Settlers", Order = 1, RequireRestart = false,
            HintText = "{=RF_Logs_Settlers_Hint}RF_Settlers.log — settler caravans, camps and village founding.")]
        [SettingPropertyGroup(GroupSystems, GroupOrder = 2)]
        public bool Settlers { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_Enlistment}Enlistment", Order = 2, RequireRestart = false,
            HintText = "{=RF_Logs_Enlistment_Hint}RF_Enlistment*.log — service under a lord and that lord's decisions.")]
        [SettingPropertyGroup(GroupSystems, GroupOrder = 2)]
        public bool Enlistment { get; set; } = false;

        // ── Battle AI ────────────────────────────────────────────────────────
        [SettingPropertyBool("{=RF_Logs_BattleRuntime}Battle AI runtime trace", Order = 0, RequireRestart = false,
            HintText = "{=RF_Logs_BattleRuntime_Hint}RF_BattleAI_RuntimeTrace.log — per-formation snapshots during battles. Verbose, but the single most useful log for tactics feedback.")]
        [SettingPropertyGroup(GroupBattle, GroupOrder = 3)]
        public bool BattleAiRuntime { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_BattleMemory}Battle AI adaptive memory", Order = 1, RequireRestart = false,
            HintText = "{=RF_Logs_BattleMemory_Hint}RF_BattleAI_AdaptiveMemory.log — one line per battle: doctrine used and how it went. Tiny; this is also the file the AI learns from, so leaving it on is cheap.")]
        [SettingPropertyGroup(GroupBattle, GroupOrder = 3)]
        public bool BattleAiMemory { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_BattleTactics}Battle AI tactics telemetry", Order = 2, RequireRestart = false,
            HintText = "{=RF_Logs_BattleTactics_Hint}RF_BattleAI_tactics.log — active tactic and behavior weights per formation.")]
        [SettingPropertyGroup(GroupBattle, GroupOrder = 3)]
        public bool BattleAiTactics { get; set; } = false;

        [SettingPropertyBool("{=RF_Logs_BattleTrap}Bandit trap telemetry", Order = 3, RequireRestart = false,
            HintText = "{=RF_Logs_BattleTrap_Hint}RF_BattleAI_trap.log — bandit bait/pincer decisions.")]
        [SettingPropertyGroup(GroupBattle, GroupOrder = 3)]
        public bool BattleAiTrap { get; set; } = false;

        // ── Reading (null-safe) ──────────────────────────────────────────────
        private static bool _mcmUnavailable;

        internal static bool IsLogEnabled(string key)
        {
            if (_mcmUnavailable)
            {
                return false;
            }

            try
            {
                RFLogSettings? settings = ReadInstance();
                if (settings == null || !settings.MasterEnabled)
                {
                    return false;
                }

                return key switch
                {
                    RFLogSwitchboard.KingdomObjectives => settings.KingdomObjectives,
                    RFLogSwitchboard.WarSystemTrace => settings.WarSystemTrace,
                    RFLogSwitchboard.CampaignAiTrace => settings.CampaignAiTrace,
                    RFLogSwitchboard.FactionEconomy => settings.FactionEconomy,
                    RFLogSwitchboard.YoungWorld => settings.YoungWorld,
                    RFLogSwitchboard.Capitulation => settings.Capitulation,
                    RFLogSwitchboard.ResourceZones => settings.ResourceZones,
                    RFLogSwitchboard.Settlers => settings.Settlers,
                    RFLogSwitchboard.Enlistment => settings.Enlistment,
                    RFLogSwitchboard.BattleAiRuntime => settings.BattleAiRuntime,
                    RFLogSwitchboard.BattleAiMemory => settings.BattleAiMemory,
                    RFLogSwitchboard.BattleAiTactics => settings.BattleAiTactics,
                    RFLogSwitchboard.BattleAiTrap => settings.BattleAiTrap,
                    _ => false
                };
            }
            catch
            {
                _mcmUnavailable = true;
                return false;
            }
        }

        // Isolated so a missing MCM assembly fails here (caught) instead of at
        // the JIT of the caller.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RFLogSettings? ReadInstance()
        {
            return AttributeGlobalSettings<RFLogSettings>.Instance;
        }
    }
}
