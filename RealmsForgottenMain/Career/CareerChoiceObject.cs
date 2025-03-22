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
        public CareerObject? OwnerCareer { get; private set; }
        public CareerChoiceGroupObject? BelongsToGroup { get; private set; }
        public CareerChoiceObject(string stringId) : base(stringId) { }
        public override string ToString() => Name.ToString();
        public PassiveEffect? Passive { get; private set; } = null;
        public List<WeaponClass>? weapons = null;
        public ActiveEffect? Active { get; private set; } = null;
        public void Initialize(CareerObject ownerCareer, string description, string belongsToGroup, ActiveEffect activeEffect)
        {
            Active = activeEffect;

            base.Initialize(new TextObject(StringId), new TextObject(description));
            OwnerCareer = ownerCareer;
            if (!string.IsNullOrEmpty(belongsToGroup))
            {
                BelongsToGroup = MBObjectManager.Instance.GetObject<CareerChoiceGroupObject>(x => x.StringId == belongsToGroup);
            }
            else BelongsToGroup = null;
            BelongsToGroup?.Choices.Add(this);
            AfterInitialized();
        }
        public void Initialize(CareerObject ownerCareer, string description, string belongsToGroup, PassiveEffect passiveEffect)
        {
            Passive = passiveEffect;
            base.Initialize(new TextObject(StringId), new TextObject(description));
            OwnerCareer = ownerCareer;
            if (!string.IsNullOrEmpty(belongsToGroup))
            {
                BelongsToGroup = MBObjectManager.Instance.GetObject<CareerChoiceGroupObject>(x => x.StringId == belongsToGroup);
            }
            else BelongsToGroup = null;
            BelongsToGroup?.Choices.Add(this);
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
            public bool WithFactorFlatSwitch;
            public DamageProportionTuple? DamageProportionTuple = null;

            public delegate bool SpecialCombatInteractionFunction(Agent attacker, Agent victim);
            private readonly SpecialCombatInteractionFunction? _specialCombatInteractionFunction;
            public delegate bool SpecialCharacterEvaluationFunction(CharacterObject characterObject);
            private readonly SpecialCharacterEvaluationFunction? _specialCharacterEvaluationFunction;
            private readonly Action? _perkActivate;

            public bool IsValidCombatInteraction(Agent attacker, Agent victim) => _specialCombatInteractionFunction == null || _specialCombatInteractionFunction.Invoke(attacker, victim);
            public bool IsValidCharacterObject(CharacterObject characterObject) => _specialCharacterEvaluationFunction == null || _specialCharacterEvaluationFunction.Invoke(characterObject);
            public PassiveEffect(PassiveEffectType type, DamageProportionTuple damageProportionTuple, SpecialCombatInteractionFunction? function = null)
            {
                EffectMagnitude = 0;
                Operation = OperationType.Add;
                PassiveEffectType = type;
                DamageProportionTuple = damageProportionTuple;
                _specialCombatInteractionFunction = function;
            }

            public PassiveEffect(float effectValue = 0, PassiveEffectType type = PassiveEffectType.Special, SpecialCharacterEvaluationFunction? function = null, bool withFactorFlatSwitch = false)
            {
                EffectMagnitude = effectValue;
                Operation = OperationType.Add;
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
            return Passive.EffectMagnitude;
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
        CompanionLimit, 
        ArmorPenetration,   //player ignores armor with attack mask - this cant be Spells, will be ignored


        //have to be enabled
        Special,           //For everything that requires special implementation
        //AccuracyPenalty,           //spray of ranged weapons
        //RangedMovementPenalty, // inaccuracy for ranged weapons penality due to movement
        //HorseHealth,        //only player, percentage based
        //HorseChargeDamage,  //Damage When Horse is raced into infantry.
        //PartySize,
        //TroopMorale,        //Morale
        //TroopUpgradeCost,
        //SwingSpeed,
        //EquipmentWeightReduction
    }
}
