using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Career
{
    public class CareerChoiceObject : PropertyObject
    {
        public CareerObject OwnerCareer { get; private set; }
        public CareerChoiceGroupObject BelongsToGroup { get; private set; }

        public CareerChoiceObject(string stringId) : base(stringId) { }
        public override string ToString() => Name.ToString();
        public PassiveEffect Passive { get; private set; }
        public ActiveEffect Active{ get; private set; }
        public void Initialize(CareerObject ownerCareer, string description, string belongsToGroup, ActiveEffect activeEffect)
        {
            TextObject text;
            text = new TextObject(description);
            Active = activeEffect;

            base.Initialize(new TextObject(StringId), text);
            OwnerCareer = ownerCareer;
            if (!string.IsNullOrEmpty(belongsToGroup))
            {
                BelongsToGroup = MBObjectManager.Instance.GetObject<CareerChoiceGroupObject>(x => x.StringId == belongsToGroup);
            }
            else BelongsToGroup = null;
            if (BelongsToGroup != null) BelongsToGroup.Choices.Add(this);
            AfterInitialized();

        }
        public void Initialize(CareerObject ownerCareer, string description, string belongsToGroup, bool isRootNode, PassiveEffect passiveEffect = null)
        {
            TextObject text;
            text = new TextObject(description);
            Passive = passiveEffect;
            if (GameTexts.TryGetText("careerchoice_description", out var descriptionOverride, StringId))
            {
                if (Passive != null)
                {
                    if (Passive.DamageProportionTuple != null)
                    {
                        var damageType = Passive.DamageProportionTuple.DamageType;
                        GameTexts.TryGetText("tor_damagetype", out var damageTypeText, damageType.ToString());
                        GameTexts.SetVariable("EFFECT_DAMAGE_TYPE", damageTypeText);

                        GameTexts.SetVariable("EFFECT_ATTACK_TYPE", damageTypeText);
                        GameTexts.SetVariable("EFFECT_VALUE", (Passive.DamageProportionTuple.Percent).ToString("R"));
                    }
                    else
                    {
                        if (Passive.InterpretAsPercentage)
                        {
                            if (Passive.PassiveEffectType == PassiveEffectType.ArmorPenetration)
                            {
                                GameTexts.SetVariable("EFFECT_VALUE", (-Passive.EffectMagnitude).ToString("R"));
                            }
                            else
                            {
                                GameTexts.SetVariable("EFFECT_VALUE", Passive.EffectMagnitude.ToString("R"));
                            }
                        }
                        else
                        {
                            GameTexts.SetVariable("EFFECT_VALUE", Passive.EffectMagnitude.ToString());
                        }
                    }
                }

                if (descriptionOverride != null)
                {
                    text = new TextObject(descriptionOverride.ToString());
                }
            }
            base.Initialize(new TextObject(StringId), text);
            OwnerCareer = ownerCareer;
            if (!string.IsNullOrEmpty(belongsToGroup))
            {
                BelongsToGroup = MBObjectManager.Instance.GetObject<CareerChoiceGroupObject>(x => x.StringId == belongsToGroup);
            }
            else BelongsToGroup = null;
            if (BelongsToGroup != null) BelongsToGroup.Choices.Add(this);
            AfterInitialized();
        }
        public class ActiveEffect
        {
            private readonly Action onExecute;
            public ActiveEffect(Action _onExecute)
            {
                onExecute = _onExecute;
            }

            public void TryExecute()
            {
                try
                {
                    onExecute.Invoke();
                }
                catch (Exception ex)
                {
                    InformationManager.DisplayMessage(new($"ERROR giving player active effect, message: {ex.Message}"));
                }
            }
        }
        public class PassiveEffect
        {
            public float EffectMagnitude = 0f;
            public OperationType Operation = OperationType.None;
            public PassiveEffectType PassiveEffectType = PassiveEffectType.Special;
            public bool InterpretAsPercentage = true;
            public bool WithFactorFlatSwitch;
            public DamageProportionTuple? DamageProportionTuple;

            public delegate bool SpecialCombatInteractionFunction(Agent attacker, Agent victim);
            private readonly SpecialCombatInteractionFunction? _specialCombatInteractionFunction;
            public delegate bool SpecialCharacterEvaluationFunction(CharacterObject characterObject);
            private readonly SpecialCharacterEvaluationFunction? _specialCharacterEvaluationFunction;
            private readonly Action? _perkActivate;

            public bool IsValidCombatInteraction(Agent attacker, Agent victim) => _specialCombatInteractionFunction == null || _specialCombatInteractionFunction.Invoke(attacker, victim);

            public bool IsValidCharacterObject(CharacterObject characterObject) => _specialCharacterEvaluationFunction == null || _specialCharacterEvaluationFunction.Invoke(characterObject);
            public PassiveEffect(PassiveEffectType type, DamageProportionTuple damageProportionTuple, SpecialCombatInteractionFunction? function = null, bool interpretAsPercentage = true)
            {
                InterpretAsPercentage = true;
                EffectMagnitude = 0;
                Operation = OperationType.Add;
                InterpretAsPercentage = interpretAsPercentage;
                PassiveEffectType = type;
                DamageProportionTuple = damageProportionTuple;
                _specialCombatInteractionFunction = function;
            }

            public PassiveEffect(float effectValue = 0, PassiveEffectType type = PassiveEffectType.Special, bool asPercent = false, SpecialCharacterEvaluationFunction? function = null, bool withFactorFlatSwitch = false)
            {
                EffectMagnitude = effectValue;
                Operation = OperationType.Add;
                InterpretAsPercentage = asPercent;
                PassiveEffectType = type;
                _specialCharacterEvaluationFunction = function;
                WithFactorFlatSwitch = withFactorFlatSwitch;
            }
            public PassiveEffect(PassiveEffectType type, Action perkActivate)
            {
                _perkActivate = perkActivate;
                PassiveEffectType = type;
            }
            public void Activate()
            {
                _perkActivate?.Invoke();
            }
        }

        public float GetPassiveValue()
        {
            if (Passive == null) return 0;
            return Passive.InterpretAsPercentage ? Passive.EffectMagnitude / 100 : Passive.EffectMagnitude;
        }
    }
    public enum OperationType
    {
        Add,
        Multiply,
        Replace,
        None
    }

    public enum PassiveEffectType
    {
        //edited to work
        Ammo,               //arrows, crossbows , flat number
        SpellAmmo,          // + alchemical stones
        Health,             //Player health points, flat number
        Damage,             //player damage, requires damage tuple
        Resistance,         //player resistance requires damage tuple
        TroopDamage,
        TroopResistance,
        MercContractIncome,
        WorkshopIncome,
        TownIncome,
        VillageIncome,
        CaravanIncome,
        TroopRegeneration,
        HealthRegeneration,
        SpottingRange,
        TroopWages,
        GetItem,
        PartyMovementSpeed,
        OnKill,

        //have to be enabled
        Special,            //For everything that requires special implementation

        CustomResourceUpkeepModifier, //scales custom resource upkeep
        CustomResourceUpgradeCostModifier, //scales custom upgrade costs
        CustomResourceGain, //daily gain for custom resource , flat number
        AccuracyPenalty,           //spray of ranged weapons
        RangedMovementPenalty, // inaccuracy for ranged weapons penality due to movement
        ArmorPenetration,   //player ignores armor with attack mask - this cant be Spells, will be ignored
        HorseHealth,        //only player, percentage based
        HorseChargeDamage,  //Damage When Horse is raced into infantry.
        BuffDuration,       //Increases duration for friendly augments    
        DebuffDuration,     //Increases duration for hex
        PartySize,
        CompanionLimit,
        TroopMorale,        //Morale
        TroopUpgradeCost,
        SwingSpeed,
        EquipmentWeightReduction
    }
}
