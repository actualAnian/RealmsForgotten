using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten
{
    public class RFSettings
    {
        static ICustomSettingsProvider _provider;
        public static ICustomSettingsProvider Instance
        {
            get
            {
                if (_provider != null) return _provider;
                try
                {
                    if (GlobalSettings<CustomSettings>.Instance != null)
                    {
                        _provider = GlobalSettings<CustomSettings>.Instance;
                        return _provider;
                    }
                }
                catch
                {
                    string text = "no MCM module found, using default settings";
                    InformationManager.DisplayMessage(new InformationMessage(text, new Color(0, 0, 0)));
                }
                _provider = new HardcodedCustomSettings();
                return _provider;
            }
        }
    }
    public interface ICustomSettingsProvider
    {
        bool PunishingArenaDefeats { get; set; }
        public bool SmartAthasEnslavers { get; set; }
    }

    public class HardcodedCustomSettings : ICustomSettingsProvider
    {
        public bool PunishingArenaDefeats { get; set; } = false;
        public bool SmartAthasEnslavers { get; set; } = false;
    }

    public class CustomSettings : AttributeGlobalSettings<CustomSettings>, ICustomSettingsProvider
    {
        public override string Id { get; } = "Realms Forgotten Setings";
        public override string DisplayName => new TextObject("{=CustomSettings_Name}Realms Forgotten {VERSION}", new Dictionary<string, object>
    {
        { "VERSION", typeof(CustomSettings).Assembly.GetName().Version?.ToString(3) ?? "ERROR" }
    }).ToString();
        public override string FolderName { get; } = "Custom";
        public override string FormatType { get; } = "json";

        [SettingPropertyBool("Punishing arena defeats", RequireRestart = false, HintText = "Life in an arena is not an easy one, defeat means death!")]
        [SettingPropertyGroup("{=CustomSettings_General}General")]
        public bool PunishingArenaDefeats { get; set; } = false;

        [SettingPropertyBool("Stronger athas enslavers", RequireRestart = false, HintText = "You are not the only one who realised infantry moves faster on campaign map when there are spare horses, now enslavers will make use of this")]
        [SettingPropertyGroup("{=CustomSettings_General}General")]
        public bool SmartAthasEnslavers { get; set; } = false;

        [SettingPropertyBool("Enable influence cost for recruiting from different cultures.", RequireRestart = false, HintText = "Lords will spent influence for recruiting from different cultures.")]
        [SettingPropertyGroup("{=CustomSettings_General}General")]
        public bool InfluenceCostForDifferentCultures { get; set; } = true;
    }
}
