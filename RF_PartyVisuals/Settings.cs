using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace RF_PartyVisuals
{
    /// <summary>
    /// MCM settings for RF_PartyVisuals. Same AttributeGlobalSettings pattern as RF_IsoCam.
    /// </summary>
    public sealed class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "RF_PartyVisuals";
        public override string DisplayName => "RF Party Visuals";
        public override string FolderName => "RF_PartyVisuals";
        public override string FormatType => "json";

        private bool _enabled = true;

        [SettingPropertyBool("{=rf_pv_enable}Enable party visuals", RequireRestart = false,
            HintText = "{=rf_pv_enable_desc}Master switch. When off, party icons keep their vanilla appearance.")]
        [SettingPropertyGroup("RF Party Visuals")]
        public bool Enabled
        {
            get => _enabled;
            set { if (value != _enabled) { _enabled = value; OnPropertyChanged(); } }
        }

        private int _additionalUnitCount = 6;

        [SettingPropertyInteger("{=rf_pv_extra}Extra troop figures", 0, 20, "0", RequireRestart = false,
            HintText = "{=rf_pv_extra_desc}Maximum extra 3D troop figures shown around a party icon, on top of the leader. Higher = fuller icons but heavier. Actual count also scales with party size. Default 6.")]
        [SettingPropertyGroup("RF Party Visuals")]
        public int AdditionalUnitCount
        {
            get => _additionalUnitCount;
            set { if (value != _additionalUnitCount) { _additionalUnitCount = value; OnPropertyChanged(); } }
        }

        private float _viewDistance = 45f;

        [SettingPropertyFloatingInteger("{=rf_pv_dist}View distance", 10f, 120f, "0", RequireRestart = false,
            HintText = "{=rf_pv_dist_desc}Only parties within this map distance of your party get the extra figures (performance). Beyond it, the vanilla icon is used. Default 45.")]
        [SettingPropertyGroup("RF Party Visuals")]
        public float ViewDistance
        {
            get => _viewDistance;
            set { if (value != _viewDistance) { _viewDistance = value; OnPropertyChanged(); } }
        }

        private float _figureSpacing = 0.55f;

        [SettingPropertyFloatingInteger("{=rf_pv_space}Figure spacing", 0.30f, 1.20f, "0.00", RequireRestart = false,
            HintText = "{=rf_pv_space_desc}Gap between the troop figures clustered behind a party's leader. Higher = more breathing room / looser crowd. Applies as parties are refreshed. Default 0.55.")]
        [SettingPropertyGroup("RF Party Visuals")]
        public float FigureSpacing
        {
            get => _figureSpacing;
            set { if (value != _figureSpacing) { _figureSpacing = value; OnPropertyChanged(); } }
        }

        private bool _restyleLivingWorld = true;

        [SettingPropertyBool("{=rf_pv_lw}Restyle living-world parties", RequireRestart = false,
            HintText = "{=rf_pv_lw_desc}Give RF living-world parties (herders, merchants, pilgrims, refugees, settlers) themed figures — herd animals, carts, processions.")]
        [SettingPropertyGroup("RF Party Visuals")]
        public bool RestyleLivingWorld
        {
            get => _restyleLivingWorld;
            set { if (value != _restyleLivingWorld) { _restyleLivingWorld = value; OnPropertyChanged(); } }
        }
    }
}
