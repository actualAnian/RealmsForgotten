using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace RF_IsoCam
{
    /// <summary>
    /// MCM settings page for RF_IsoCam. Follows the same AttributeGlobalSettings&lt;T&gt;
    /// pattern used by RFSmithing (Id / DisplayName / FolderName / FormatType override).
    /// Key bindings are stored as plain strings (InputKey enum member names) exactly like
    /// the main RealmsForgotten mod does, then parsed with Enum.Parse at runtime.
    /// </summary>
    public sealed class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "RF_IsoCam";
        public override string DisplayName => "RF IsoCam";
        public override string FolderName => "RF_IsoCam";
        public override string FormatType => "json";

        // ---------------- General ----------------

        private bool _enableIsoCamera = true;

        [SettingPropertyBool("{=rf_isocam_enable}Enable isometric camera", RequireRestart = false,
            HintText = "{=rf_isocam_enable_desc}Master switch. When off, the toggle key does nothing and the game keeps its normal camera.")]
        [SettingPropertyGroup("RF IsoCam")]
        public bool EnableIsoCamera
        {
            get => _enableIsoCamera;
            set { if (value != _enableIsoCamera) { _enableIsoCamera = value; OnPropertyChanged(); } }
        }

        // ---------------- Keys ----------------

        private string _toggleKey = "H";

        [SettingPropertyText("{=rf_isocam_toggle_key}Toggle key", RequireRestart = false,
            HintText = "{=rf_isocam_toggle_key_desc}InputKey name that toggles the isometric camera on/off (e.g. H, BackSpace, L). Avoid keys already used in combat like Tab.")]
        [SettingPropertyGroup("RF IsoCam/Keys")]
        public string ToggleKey
        {
            get => _toggleKey;
            set { if (value != _toggleKey) { _toggleKey = value; OnPropertyChanged(); } }
        }

        private string _rotateLeftKey = "Delete";

        [SettingPropertyText("{=rf_isocam_rotate_left_key}Rotate left key", RequireRestart = false,
            HintText = "{=rf_isocam_rotate_left_key_desc}InputKey name that rotates the isometric camera counter-clockwise while held.")]
        [SettingPropertyGroup("RF IsoCam/Keys")]
        public string RotateLeftKey
        {
            get => _rotateLeftKey;
            set { if (value != _rotateLeftKey) { _rotateLeftKey = value; OnPropertyChanged(); } }
        }

        private string _rotateRightKey = "PageDown";

        [SettingPropertyText("{=rf_isocam_rotate_right_key}Rotate right key", RequireRestart = false,
            HintText = "{=rf_isocam_rotate_right_key_desc}InputKey name that rotates the isometric camera clockwise while held.")]
        [SettingPropertyGroup("RF IsoCam/Keys")]
        public string RotateRightKey
        {
            get => _rotateRightKey;
            set { if (value != _rotateRightKey) { _rotateRightKey = value; OnPropertyChanged(); } }
        }

        // ---------------- Camera geometry ----------------

        private float _cameraDistance = 14f;

        [SettingPropertyFloatingInteger("{=rf_isocam_distance}Camera distance", 3f, 45f, "0.0", RequireRestart = false,
            HintText = "{=rf_isocam_distance_desc}Straight-line distance from the player to the camera. Higher = further away / more zoomed out. Default 14.")]
        [SettingPropertyGroup("RF IsoCam/Camera")]
        public float CameraDistance
        {
            get => _cameraDistance;
            set { if (value != _cameraDistance) { _cameraDistance = value; OnPropertyChanged(); } }
        }

        private float _cameraHeight = 6f;

        [SettingPropertyFloatingInteger("{=rf_isocam_height}Extra camera height", 0f, 30f, "0.0", RequireRestart = false,
            HintText = "{=rf_isocam_height_desc}Additional vertical raise applied to both the camera and the look target. Raises the whole framing without changing the tilt. Default 6.")]
        [SettingPropertyGroup("RF IsoCam/Camera")]
        public float CameraHeight
        {
            get => _cameraHeight;
            set { if (value != _cameraHeight) { _cameraHeight = value; OnPropertyChanged(); } }
        }

        private float _cameraAngle = 55f;

        [SettingPropertyFloatingInteger("{=rf_isocam_angle}Camera pitch angle", 15f, 85f, "0.0", RequireRestart = false,
            HintText = "{=rf_isocam_angle_desc}Down-tilt of the camera in degrees. 90 = straight top-down, low values = flatter/behind. Default 55.")]
        [SettingPropertyGroup("RF IsoCam/Camera")]
        public float CameraAngle
        {
            get => _cameraAngle;
            set { if (value != _cameraAngle) { _cameraAngle = value; OnPropertyChanged(); } }
        }

        private float _rotationSpeed = 70f;

        [SettingPropertyFloatingInteger("{=rf_isocam_rotation_speed}Rotation speed", 10f, 220f, "0.0", RequireRestart = false,
            HintText = "{=rf_isocam_rotation_speed_desc}How fast the rotate keys spin the camera around the player, in degrees per second. Default 70.")]
        [SettingPropertyGroup("RF IsoCam/Camera")]
        public float RotationSpeed
        {
            get => _rotationSpeed;
            set { if (value != _rotationSpeed) { _rotationSpeed = value; OnPropertyChanged(); } }
        }
    }
}
