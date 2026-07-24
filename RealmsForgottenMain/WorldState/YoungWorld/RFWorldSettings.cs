using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace RealmsForgotten.WorldState.YoungWorld
{
    /// <summary>
    /// MCM settings for the "World Evolution" systems (Young World, wall-stage
    /// troop tier cap, and the Faction Economy Index). Brand-new settings group
    /// (<c>Id = "RF_World"</c>); it does not touch any existing RF settings.
    ///
    /// EVERY toggle defaults to <c>false</c> — with all of them off the game must
    /// be bit-for-bit identical to vanilla RF. Consumers must never read the raw
    /// instance directly: use the static <see cref="YoungWorld"/> / <see cref="TierCap"/>
    /// / <see cref="IefMilitary"/> / <see cref="IefTelemetry"/> accessors, which are
    /// null-safe and return <c>false</c> when the MCM assembly is missing or
    /// incompatible (same defensive pattern as Homesteads' TraceLogger).
    /// </summary>
    public class RFWorldSettings : AttributeGlobalSettings<RFWorldSettings>
    {
        public override string Id => "RF_World";
        public override string DisplayName => new TextObject("{=RF_World_Name}RF World Evolution").ToString();
        public override string FolderName => "RF_World";
        public override string FormatType => "xml";

        // ── Young World ──────────────────────────────────────────────────────
        [SettingPropertyBool("{=RF_World_YoungWorld_Name}Young World (new campaigns only)", Order = 0, RequireRestart = false,
            HintText = "{=RF_World_YoungWorld_Hint}Only affects NEW campaigns, applied once at creation. Fortifications start at wall level 1 (one capital per kingdom at level 2) and prosperity/hearth begin low, so the realms grow up from a rough age. Has no effect on saved games.")]
        [SettingPropertyGroup("{=RF_World_Group_YoungWorld}Young World", GroupOrder = 0)]
        public bool YoungWorldEnabled { get; set; } = false;

        // ── Wall-stage troop tier cap ────────────────────────────────────────
        [SettingPropertyBool("{=RF_World_TierCap_Name}Wall-Stage Troop Tier Cap", Order = 0, RequireRestart = false,
            HintText = "{=RF_World_TierCap_Hint}Volunteers offered by notables (to the player AND to AI lords) cannot grow past a tier tied to their kingdom's wall stage: mostly level-1 walls allow tiers 1-2 only, mostly level-2 walls up to tier 4, two or more level-3 towns lift the cap entirely.")]
        [SettingPropertyGroup("{=RF_World_Group_TierCap}Troop Tier Cap", GroupOrder = 1)]
        public bool TierCapEnabled { get; set; } = false;

        [SettingPropertyBool("{=RF_World_EquipCap_Name}Equipment Tier Cap (shops & smithy)", Order = 2, RequireRestart = false,
            HintText = "{=RF_World_EquipCap_Hint}Extends the wall-stage cap to gear: town markets of a below-stage kingdom stop stocking weapons/armor/shields above the stage tier (1-2 / up to 4 / all), and the smithy refuses crafting pieces above it. Magic staffs, horses and banners are untouched.")]
        [SettingPropertyGroup("{=RF_World_Group_TierCap}Troop Tier Cap", GroupOrder = 1)]
        public bool EquipmentCapEnabled { get; set; } = false;

        [SettingPropertyBool("{=RF_World_Ief_Name}IEF Military Effects (±1 tier)", Order = 1, RequireRestart = false,
            HintText = "{=RF_World_Ief_Hint}When the tier cap is on, a strong economy (Faction Economy Index ≥ 70) raises the kingdom's cap by one tier and a weak one (≤ 40) lowers it by one. Requires the tier cap to be enabled.")]
        [SettingPropertyGroup("{=RF_World_Group_TierCap}Troop Tier Cap", GroupOrder = 1)]
        public bool IefMilitaryEffectsEnabled { get; set; } = false;

        // ── Faction Economy Index (phase 1: measurement only) ────────────────
        [SettingPropertyBool("{=RF_World_IefTelemetry_Name}IEF Telemetry Log", Order = 0, RequireRestart = false,
            HintText = "{=RF_World_IefTelemetry_Hint}Writes one line per kingdom per day to Configs/ModLogs/RF_IEF.log. Measurement only — no gameplay effect on its own.")]
        [SettingPropertyGroup("{=RF_World_Group_Ief}Faction Economy Index", GroupOrder = 2)]
        public bool IefTelemetryEnabled { get; set; } = false;

        // ── Null-safe static accessors ───────────────────────────────────────
        // MCM may be absent/incompatible; touching GlobalSettings<> can then throw
        // at JIT time. Isolate the type reference in a NoInlining method and wrap
        // the call so a missing assembly degrades to "off" instead of crashing.

        private static bool _mcmUnavailable;

        public static bool YoungWorld => ReadBool(static s => s.YoungWorldEnabled);
        public static bool TierCap => ReadBool(static s => s.TierCapEnabled);
        public static bool EquipmentCap => ReadBool(static s => s.EquipmentCapEnabled);
        public static bool IefMilitary => ReadBool(static s => s.IefMilitaryEffectsEnabled);
        public static bool IefTelemetry => ReadBool(static s => s.IefTelemetryEnabled);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadFromInstance(Func<RFWorldSettings, bool> selector)
        {
            RFWorldSettings? instance = GlobalSettings<RFWorldSettings>.Instance;
            return instance != null && selector(instance);
        }

        private static bool ReadBool(Func<RFWorldSettings, bool> selector)
        {
            if (_mcmUnavailable)
            {
                return false;
            }

            try
            {
                return ReadFromInstance(selector);
            }
            catch
            {
                _mcmUnavailable = true;
                return false;
            }
        }
    }
}
