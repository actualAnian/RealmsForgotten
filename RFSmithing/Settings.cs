﻿using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace RealmsForgotten.Smithing
{
    public sealed class Settings : AttributeGlobalSettings<Settings>
    {
        private float _maximumBotchChance = 0.95f;

        private bool _defaultSmeltingModel = false;

        private bool _allowSmeltingOtherItems = true;

        private int _skillOverDifficultyBeforeNoPenalty = 25;

        private bool _allowCraftingNormalWeapons = false;

        private bool _noMaterialsRequired = false;

        private bool _noStaminaRequired = false;

        private bool _noSkillRequired = false;

        private float _craftingCostModifier = 1f;

        private float _craftingCostAdditionalModifier = 1f;

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_material_cost}Material cost modifier", 0f, 5f, "x0.00", HintText = "{=rfsmithing_mcm_crafting_material_cost_description}How much the material cost is multiplied by when crafting. Default is x1.00", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public float CraftingCostModifier
        {
            get => _craftingCostModifier;
            set
            {
                if (value != _craftingCostModifier)
                {
                    _craftingCostModifier = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_additional_material_cost}Additional material cost modifier", 0f, 5f, "x0.00", HintText = "{=rfsmithing_mcm_crafting_additional_material_cost_description}How much the additional materials cost is multiplied by when crafting. Default is x1.00", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public float CraftingCostAdditionalModifier
        {
            get => _craftingCostAdditionalModifier;
            set
            {
                if (value != _craftingCostAdditionalModifier)
                {
                    _craftingCostAdditionalModifier = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyBool("{=rfsmithing_mcm_no_materials_required}No materials required", HintText = "{=rfsmithing_mcm_no_materials_required_description}If enabled, crafting will not require any materials.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats")]
        public bool NoMaterialsRequired
        {
            get => _noMaterialsRequired;
            set
            {
                if (value != _noMaterialsRequired)
                {
                    _noMaterialsRequired = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyBool("{=rfsmithing_mcm_no_stamina_required}No stamina required", HintText = "{=rfsmithing_mcm_no_stamina_required_description}If enabled, crafting will not require any stamina.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats")]
        public bool NoStaminaRequired
        {
            get => _noStaminaRequired;
            set
            {
                if (value != _noStaminaRequired)
                {
                    _noStaminaRequired = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyBool("{=rfsmithing_mcm_difficulty_one}Set difficulty to 1", HintText = "{=rfsmithing_mcm_difficulty_one_description}If enabled, crafting difficulty will be set to 1.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats")]
        public bool NoSkillRequired
        {
            get => _noSkillRequired;
            set
            {
                if (value != _noSkillRequired)
                {
                    _noSkillRequired = value;
                    OnPropertyChanged();
                }
            }
        }

        /**
         *
         * For testing porpuses
         */
        private bool _useOldModifierBehaviour = false;

        [SettingPropertyBool("Use old modifier behaviour", HintText = "Use old modifier behaviour", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public bool UseOldModifierBehaviour
        {
            get => _useOldModifierBehaviour;
            set
            {
                if (value != _useOldModifierBehaviour)
                {
                    _useOldModifierBehaviour = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _legendaryChanceIncrease = 0f;

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_legendary_added_chance}Legendary added chance", 0f, 1f, "#0%", Order = 5, HintText = "{=rfsmithing_mcm_crafting_legendary_added_chance_description}Added chance to get a Legendary crafting result.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats/Chances")]
        public float LegendaryChanceIncrease
        {
            get => _legendaryChanceIncrease;
            set
            {
                if (value != _legendaryChanceIncrease)
                {
                    _legendaryChanceIncrease = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _masterworkChanceIncrease = 0f;

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_masterwork_added_chance}Masterwork added chance", 0f, 1f, "#0%", Order = 4, HintText = "{=rfsmithing_mcm_crafting_masterwork_added_chance_description}Added chance to get a Masterwork crafting result.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats/Chances")]
        public float MasterworkChanceIncrease
        {
            get => _masterworkChanceIncrease;
            set
            {
                if (value != _masterworkChanceIncrease)
                {
                    _masterworkChanceIncrease = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _fineChanceIncrease = 0f;

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_fine_added_chance}Fine added chance", 0f, 1f, "#0%", Order = 3, HintText = "{=rfsmithing_mcm_crafting_fine_added_chance_description}Added chance to get a Fine crafting result.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats/Chances")]
        public float FineChanceIncrease
        {
            get => _fineChanceIncrease;
            set
            {
                if (value != _fineChanceIncrease)
                {
                    _fineChanceIncrease = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _commonChanceIncrease = 0f;

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_common_added_chance}Common added chance", 0f, 1f, "#0%", Order = 2, HintText = "{=rfsmithing_mcm_crafting_common_added_chance_description}Added chance to get a Common crafting result.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats/Chances")]
        public float CommonChanceIncrease
        {
            get => _commonChanceIncrease;
            set
            {
                if (value != _commonChanceIncrease)
                {
                    _commonChanceIncrease = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _inferiorChanceIncrease = 0f;

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_inferior_added_chance}Inferior added chance", 0f, 1f, "#0%", Order = 1, HintText = "{=rfsmithing_mcm_crafting_inferior_added_chance_description}Added chance to get a Inferior crafting result.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats/Chances")]
        public float InferiorChanceIncrease
        {
            get => _inferiorChanceIncrease;
            set
            {
                if (value != _inferiorChanceIncrease)
                {
                    _inferiorChanceIncrease = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _poorChanceIncrease = 0f;

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_crafting_poor_added_chance}Poor added chance", 0f, 1f, "#0%", Order = 0, HintText = "{=rfsmithing_mcm_crafting_poor_added_chance_description}Added chance to get a Poor crafting result.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Cheats/Chances")]
        public float PoorChanceIncrease
        {
            get => _poorChanceIncrease;
            set
            {
                if (value != _poorChanceIncrease)
                {
                    _poorChanceIncrease = value;
                    OnPropertyChanged();
                }
            }
        }

        /**
         *
         * For testing porpuses
         */
        public override string Id => "RFSmithing";
        public override string DisplayName => "RFSmithing";
        public override string FolderName => "RFSmithing";
        public override string FormatType => "json";

        [SettingPropertyFloatingInteger("{=rfsmithing_mcm_maximum_botch_chance}Maximum botch chance", 0f, 1f, "#0%", Order = 2, HintText = "{=rfsmithing_mcm_maximum_botch_chance_description}Maximum chance to botch crafting when Crafting skill level is lower than difficulty.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public float MaximumBotchChance
        {
            get => _maximumBotchChance;
            set
            {
                if (value != _maximumBotchChance)
                {
                    _maximumBotchChance = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyBool("{=rfsmithing_mcm_use_vanilla_smelting_calculations}Use Vanilla smelting calculations", HintText = "{=rfsmithing_mcm_use_vanilla_smelting_calculations_description}Use vanilla smelting calculations that turn 0.8 weight pugios into 2.5 weight of materials.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public bool DefaultSmeltingModel
        {
            get => _defaultSmeltingModel;
            set
            {
                if (value != _defaultSmeltingModel)
                {
                    _defaultSmeltingModel = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyBool("{=rfsmithing_mcm_allow_smelting_other_items}Allow smelting other items", HintText = "{=rfsmithing_mcm_allow_smelting_other_items_description}Allow smelting other items such as armor, shields, etc as long as they return at least one material", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public bool AllowSmeltingOtherItems
        {
            get => _allowSmeltingOtherItems;
            set
            {
                if (value != _allowSmeltingOtherItems)
                {
                    _allowSmeltingOtherItems = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyInteger("{=rfsmithing_mcm_crafting_penalty_threshold}Crafting penalty threshold", 0, 100, HintText = "{=rfsmithing_mcm_crafting_penalty_threshold_description}How much higher your skill has to be than the item difficulty before you have no chance of making a bad item.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public int SkillOverDifficultyBeforeNoPenalty
        {
            get => _skillOverDifficultyBeforeNoPenalty;
            set
            {
                if (value != _skillOverDifficultyBeforeNoPenalty)
                {
                    _skillOverDifficultyBeforeNoPenalty = value;
                    OnPropertyChanged();
                }
            }
        }

        [SettingPropertyBool("{=rfsmithing_mcm_allow_crafting_normal_weapons}Allow crafting normal weapons", HintText = "{rfsmithing_mcm_allow_crafting_normal_weapons_description}Allow crafting normal weapons in Craft mode.", RequireRestart = false)]
        [SettingPropertyGroup("RFSmithing/Behaviours")]
        public bool AllowCraftingNormalWeapons
        {
            get => _allowCraftingNormalWeapons;
            set
            {
                if (value != _allowCraftingNormalWeapons)
                {
                    _allowCraftingNormalWeapons = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}