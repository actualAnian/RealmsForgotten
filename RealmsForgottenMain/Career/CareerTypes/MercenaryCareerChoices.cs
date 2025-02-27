using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.ObjectExtensions;

namespace RealmsForgotten.Career.CareerTypes
{
    public class MercenaryCareerChoices : RFCareerChoicesBase
    {
        public MercenaryCareerChoices(CareerObject id) : base(id) { }


        private CareerChoiceObject _mercenaryRootNode;

        private CareerChoiceObject _wandering_blade_1_passive1;
        private CareerChoiceObject _wandering_blade_1_passive2;
        private CareerChoiceObject _wandering_blade_1_passive3;
        private CareerChoiceObject _wandering_blade_1_passive4;
        private CareerChoiceObject _wandering_blade_1_keystone;

        private CareerChoiceObject _wandering_blade_2_passive1;
        private CareerChoiceObject _wandering_blade_2_passive2;
        private CareerChoiceObject _wandering_blade_2_passive3;
        private CareerChoiceObject _wandering_blade_2_passive4;
        private CareerChoiceObject _wandering_blade_2_keystone;

        private CareerChoiceObject _warlord_of_coin_1_passive1;
        private CareerChoiceObject _warlord_of_coin_1_passive2;
        private CareerChoiceObject _warlord_of_coin_1_passive3;
        private CareerChoiceObject _warlord_of_coin_1_passive4;
        private CareerChoiceObject _warlord_of_coin_1_passive5;

        private CareerChoiceObject _warlord_of_coin_2_passive1;
        private CareerChoiceObject _warlord_of_coin_2_passive2;
        private CareerChoiceObject _warlord_of_coin_2_passive3;
        private CareerChoiceObject _warlord_of_coin_2_passive4;
        private CareerChoiceObject _warlord_of_coin_2_passive5;

        private CareerChoiceObject _mercenaryLordpassive1;
        private CareerChoiceObject _mercenaryLordpassive2;
        private CareerChoiceObject _mercenaryLordpassive3;
        private CareerChoiceObject _mercenaryLordpassive4;
        private CareerChoiceObject _mercenaryLordkeystone;

        private CareerChoiceObject _merc_equipment_II_passive1;
        private CareerChoiceObject _merc_equipment_II_passive2;
        private CareerChoiceObject _merc_equipment_II_passive3;
        private CareerChoiceObject _merc_equipment_II_passive4;
        private CareerChoiceObject _merc_equipment_II_keystone;
        protected override void RegisterAll()
        {
            _mercenaryRootNode = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryRoot"));

            _wandering_blade_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_1"));
            _wandering_blade_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_2"));
            _wandering_blade_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_3"));
            _wandering_blade_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_4"));
            _wandering_blade_1_keystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_5"));

            _wandering_blade_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_1"));
            _wandering_blade_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_2"));
            _wandering_blade_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_3"));
            _wandering_blade_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_4"));
            _wandering_blade_2_keystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_5"));

            _warlord_of_coin_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin1_1"));
            _warlord_of_coin_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin1_2"));
            _warlord_of_coin_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin1_3"));
            _warlord_of_coin_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin1_4"));
            _warlord_of_coin_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin1_5"));

            _warlord_of_coin_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin2_1"));
            _warlord_of_coin_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin2_2"));
            _warlord_of_coin_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin2_3"));
            _warlord_of_coin_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin2_4"));
            _warlord_of_coin_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WarlordOfCoin2_5"));

            _mercenaryLordpassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1"));
            _mercenaryLordpassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2"));
            _mercenaryLordpassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive3"));
            _mercenaryLordpassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive4"));
            _mercenaryLordkeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordKeystone"));

            _merc_equipment_II_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryEquipmentIPassive1"));
            _merc_equipment_II_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryEquipmentIPassive2"));
            _merc_equipment_II_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryEquipmentIPassive3"));
            _merc_equipment_II_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryEquipmentIPassive4"));
            _merc_equipment_II_keystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryEquipmentIKeystone"));
        }

        protected override void InitializeKeyStones()
        {
            _mercenaryRootNode.Initialize(CareerID, "The Mercenary prepares the men around him for the next attack. Makes all troops unbreakable for a short amount of time. The duration is prolonged by the leadership skills", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "righteous_fury_effect",
                        PropertyName = "TemporaryAttributes",
                        PropertyValue = (choice, originalValue, agent) => new List<string> { "Unstoppable", "Unbreakable" },
                        MutationType = OperationType.Replace
                    },
                });

            _wandering_blade_1_keystone.Initialize(CareerID, "Headshots return the arrow/bolt used.", "WanderingBlade1", false, ChoiceType.Keystone); // CareerPerkMissionBehavior, WanderingBladeKeystone

            _wandering_blade_2_keystone.Initialize(CareerID, "Increases mercenary contract gains by 20%.", "WanderingBlade2", false, ChoiceType.Keystone);

            _warlord_of_coin_1_passive5.Initialize(CareerID, "Increases range damage during the career ability by 15%.", "Headhunter", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "let_them_have_it_range_dmg" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _warlord_of_coin_2_passive5.Initialize(CareerID, "Killing an enemy replenishes 10 Health.", "DuelChampion", false, ChoiceType.Keystone);

            _mercenaryLordkeystone.Initialize(CareerID, "Values for career ability effects are doubled.", "Paymaster", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_range_dmg",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_range_res",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_ats",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "let_them_have_it_melee_rls",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => (float)originalValue * 2,
                        MutationType = OperationType.Multiply
                    }
                });

            _merc_equipment_II_keystone.Initialize(CareerID, "The Career ability reduces reload time by 15%.", "MercenaryLord", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_let_them_have_it",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ( (List<string>)originalValue ).Concat(new[] { "let_them_have_it_melee_rls" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });
        }

        protected override void InitializePassives()
        {
            _wandering_blade_1_passive1.Initialize(CareerID, "5 extra arrows, bolts ammo", "WanderingBlade1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _wandering_blade_1_passive2.Initialize(CareerID, "Increases Hitpoints by 20", "WanderingBlade1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _wandering_blade_1_passive3.Initialize(CareerID, "Extra 20% armor penetration of melee attacks", "WanderingBlade1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration));
            _wandering_blade_1_passive4.Initialize(CareerID, "Increases ranged damage by 10%.", "WanderingBlade1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalRanged, 10)));

            _wandering_blade_2_passive1.Initialize(CareerID, "The Spotting range of the party is increased by 20%", "WanderingBlade2", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _wandering_blade_2_passive2.Initialize(CareerID, "Companion limit of party is increased by 2", "WanderingBlade2", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.PhysicalMelee, 10),
                (attacker, victim) => !victim.BelongsToMainParty() && !(victim.IsMainAgent || victim.IsHero)));
            _wandering_blade_2_passive3.Initialize(CareerID, "Mercenary troops cost no extra wage.", "WanderingBlade2", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));
            _wandering_blade_2_passive4.Initialize(CareerID, "Increases melee damage resistance of melee troops by 10%.", "WanderingBlade2", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.HealthRegeneration));

            _warlord_of_coin_1_passive1.Initialize(CareerID, "10 extra ammo.", "WarlordOfCoin1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo));
            _warlord_of_coin_1_passive2.Initialize(CareerID, "Increases influence gain by 20%.", "WarlordOfCoin1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.DailyInfluence, true));
            _warlord_of_coin_1_passive3.Initialize(CareerID, "Companion limit of party is increased by 5.", "WarlordOfCoin1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _warlord_of_coin_1_passive4.Initialize(CareerID, "Increases ranged damage resistance by 15%.", "WarlordOfCoin1", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalMelee, 15)));

            _warlord_of_coin_2_passive1.Initialize(CareerID, "Increases health regeneration after battles by 10", "WarlordOfCoin2", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.HealthRegeneration));
            _warlord_of_coin_2_passive2.Initialize(CareerID, "Attacks deal bonus damage against shields.", "WarlordOfCoin2", false, ChoiceType.Passive, null);
            _warlord_of_coin_2_passive3.Initialize(CareerID, "Increases Hitpoints by 40.", "WarlordOfCoin2", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _warlord_of_coin_2_passive4.Initialize(CareerID, "Increases melee damage by 15%.", "WarlordOfCoin2", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalMelee, 15)));

            _mercenaryLordpassive1.Initialize(CareerID, "4 extra special ammo like grenades or buckshot.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(4, PassiveEffectType.Special, false)); //TORAgentStatCalculateModel 97
            _mercenaryLordpassive2.Initialize(CareerID, "Increases the damage of all ranged troops by 15%.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalMelee, 15),
                (attacker, victim) => !attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero)));
            _mercenaryLordpassive3.Initialize(CareerID, "Higher mercenary contract payment, lower Influence loss. Scales with the Trade skill.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special, true)); // TOR_Core.Models.TORClanFinanceModel. 53
            _mercenaryLordpassive4.Initialize(CareerID, "Ranged shots can penetrate multiple targets.", "MercenaryLord", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); //TORAgentApplyDamage 29

            _merc_equipment_II_passive1.Initialize(CareerID, "Companion limit of party is increased by 5.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
            _merc_equipment_II_passive2.Initialize(CareerID, "Increases the damage of all melee troops by 15%.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalMelee, 15),
                (attacker, victim) => !attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero)));

            _merc_equipment_II_passive3.Initialize(CareerID, "Hits below 15 damage do not stagger the player.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special)); // Agent extension 83
            _merc_equipment_II_passive4.Initialize(CareerID, "Companion health of party is increased by 25.", "Commander", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special));
        }
    }
}