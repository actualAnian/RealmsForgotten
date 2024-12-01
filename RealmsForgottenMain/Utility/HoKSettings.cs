using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.Utility
{
    internal sealed class HoKSettings : AttributeGlobalSettings<HoKSettings>
    {
        private bool _useStandardOptionScreen = false;

        public override string Id => "HealOnKill_v1";

        public override string DisplayName => "Heal on Kill";

        public override string FolderName => "HoK";

        public override string FormatType => "json2";

        [SettingPropertyInteger("{=HOKQHudaXBC2}Player Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOK2u0NsZmvY}Amount to heal the player upon killing an enemy.", Order = 1, RequireRestart = false)]
        public int playerHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOKcdYICCJ1c}Friendly AI Hero Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKv3ISUPLCw}Amount to heal friendly AI heroes (companions/family/friendly lords) upon their killing an enemy.", Order = 2, RequireRestart = false)]
        public int friendlyAIHeroHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOK9riNbUGxH}Enemy AI Hero Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKbpOuFvNWY}Amount to heal enemy AI heroes (enemy lords) upon their killing an enemy.", Order = 3, RequireRestart = false)]
        public int enemyAIHeroHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOKcSryc2957}Friendly AI Troop Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKdkXBUtCXU}Amount to heal friendly troops upon their killing an enemy.", Order = 4, RequireRestart = false)]
        public int friendlyAITroopHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOKAMcW5G1nb}Enemy AI Troop Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKh1va9W1Qn}Amount to heal enemy troops upon their killing an enemy.", Order = 5, RequireRestart = false)]
        public int enemyAITroopHealing { get; set; } = 0;

        [SettingPropertyFloatingInteger("{=HOKqrGwnJy3M}Player Life Leech", 0.0f, 1f, "#0%", HintText = "{=HOKt1UZbPrEb}Heals the player by percentage of damage of done.", Order = 6, RequireRestart = false)]
        public float playerLifeLeechPercent { get; set; } = 0.15f;

        [SettingPropertyFloatingInteger("{=HOKzqH9tLiTH}Friendly AI Hero Life Leech", 0.0f, 1f, "#0%", HintText = "{=HOKWbg0KQXmU}Heals friendly AI heroes (companions/family/friendly lords) by percentage of damage done.", Order = 7, RequireRestart = false)]
        public float friendlyAIHeroLifeLeechPercent { get; set; } = 0.0f;

        [SettingPropertyFloatingInteger("{=HOKlkkghJF1z}Enemy AI Hero Life Leech", 0.0f, 1f, "#0%", HintText = "{=HOKjmEYyVf0s}Heals enemy AI heroes (enemy lords) by percentage of damage done.", Order = 8, RequireRestart = false)]
        public float enemyAIHeroLifeLeechPercent { get; set; } = 0.0f;

        [SettingPropertyFloatingInteger("{=HOKtxiCNy3zI}Friendly AI Troop Life Leech", 0.0f, 1f, "#0%", HintText = "{=HOKfqSNv6jN4}Heals friendly troops by percentage of damage done.", Order = 9, RequireRestart = false)]
        public float friendlyAITroopLifeLeechPercent { get; set; } = 0.0f;

        [SettingPropertyFloatingInteger("{=HOKoBDhrhWb1}Enemy AI Troop Life Leech", 0.0f, 1f, "#0%", HintText = "{=HOKxDKdnJiEZ}Heals enemy troops by percentage of damage done.", Order = 10, RequireRestart = false)]
        public float enemyAITroopLifeLeechPercent { get; set; } = 0.0f;

        [SettingPropertyBool("{=HOKxaE9F7AUy}Heal Horses Too", HintText = "{=HOKgABdvxkl0}For mounted troops (and you) all healing done is also applied to the mount.", Order = 11, RequireRestart = false)]
        public bool healHorsesToo { get; set; } = true;

        [SettingPropertyBool("{=HOKNjCLqQi5A}Allow Ranged Healing", HintText = "{=HOKWw2fllAec}Allow healing on ranged damage", Order = 12, RequireRestart = false)]
        public bool allowRangedHealing { get; set; } = true;

        [SettingPropertyBool("{=HOKMuUyjaika}Log Player Healing to Chat", HintText = "", Order = 13, RequireRestart = false)]
        public bool logPlayerHealingToChat { get; set; } = true;

        [SettingPropertyBool("{=HOKBtXbn11mL}Log AI Hero Healing to Chat", HintText = "", Order = 14, RequireRestart = false)]
        public bool logHeroHealingToChat { get; set; } = false;

        [SettingPropertyBool("{=HOKxe6PdaTxL}Log Troop Healing to Chat", HintText = "{=HOKr4xGiDQeC}(Warning: this is just for debugging, don't enable this unless you like spam.)", Order = 15, RequireRestart = false)]
        public bool logTroopHealingToChat { get; set; } = false;

        [SettingPropertyBool("{=HOKHvyPZeDlG}Enable Medicine Skill Gain", HintText = "{=HOKJnU6UxTaU}Give medicine skill for healing.", Order = 16, RequireRestart = false)]
        public bool enableMedicineSkillGain { get; set; } = false;

        [SettingPropertyBool("{=HOKOhh5bNU0y}Enable Debug Mode", HintText = "{=HOKT35YqcIkF}Logs caught exceptions and such.", Order = 17, RequireRestart = false)]
        public bool debugMessages { get; set; } = false;

        public bool UseStandardOptionScreen
        {
            get => this._useStandardOptionScreen;
            set
            {
                if (this._useStandardOptionScreen == value)
                    return;
                this._useStandardOptionScreen = value;
                this.OnPropertyChanged(nameof(UseStandardOptionScreen));
            }
        }
    }
}
