using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using System.Collections.Generic;

namespace RealmsForgotten.Career.CareerTypes
{
    public class KnightCareerChoices : RFCareerChoicesBase
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public KnightCareerChoices(CareerObject id) : base(id) { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        private CareerChoiceObject _knight_errant_1_passive1;
        private CareerChoiceObject _knight_errant_1_passive2;
        private CareerChoiceObject _knight_errant_1_passive3;
        private CareerChoiceObject _knight_errant_1_passive4;
        private CareerChoiceObject _knight_errant_1_passive5;

        private CareerChoiceObject _knight_errant_2_passive1;
        private CareerChoiceObject _knight_errant_2_passive2;
        private CareerChoiceObject _knight_errant_2_passive3;
        private CareerChoiceObject _knight_errant_2_passive4;
        private CareerChoiceObject _knight_errant_2_passive5;

        private CareerChoiceObject _field_marshall_1_passive1;
        private CareerChoiceObject _field_marshall_1_passive2;
        private CareerChoiceObject _field_marshall_1_passive3;
        private CareerChoiceObject _field_marshall_1_passive4;
        private CareerChoiceObject _field_marshall_1_passive5;

        private CareerChoiceObject _field_marshall_2_passive1;
        private CareerChoiceObject _field_marshall_2_passive2;
        private CareerChoiceObject _field_marshall_2_passive3;
        private CareerChoiceObject _field_marshall_2_passive4;
        private CareerChoiceObject _field_marshall_2_passive5;

        private CareerChoiceObject _paragon_of_virtue_1_passive1;
        private CareerChoiceObject _paragon_of_virtue_1_passive2;
        private CareerChoiceObject _paragon_of_virtue_1_passive3;
        private CareerChoiceObject _paragon_of_virtue_1_passive4;
        private CareerChoiceObject _paragon_of_virtue_1_passive5;

        private CareerChoiceObject _paragon_of_virtue_2_passive1;
        private CareerChoiceObject _paragon_of_virtue_2_passive2;
        private CareerChoiceObject _paragon_of_virtue_2_passive3;
        private CareerChoiceObject _paragon_of_virtue_2_passive4;
        private CareerChoiceObject _paragon_of_virtue_2_passive5;
        protected override void RegisterAll()
        {
            _knight_errant_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant1_1"));
            _knight_errant_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant1_2"));
            _knight_errant_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant1_3"));
            _knight_errant_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant1_4"));
            _knight_errant_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant1_5"));
            _knight_errant_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant2_1"));
            _knight_errant_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant2_2"));
            _knight_errant_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant2_3"));
            _knight_errant_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant2_4"));
            _knight_errant_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("KnightErrant2_5"));

            _field_marshall_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_1"));
            _field_marshall_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_2"));
            _field_marshall_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_3"));
            _field_marshall_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_4"));
            _field_marshall_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_5"));
            _field_marshall_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_1"));
            _field_marshall_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_2"));
            _field_marshall_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_3"));
            _field_marshall_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_4"));
            _field_marshall_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_5"));

            _paragon_of_virtue_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue1_1"));
            _paragon_of_virtue_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue1_2"));
            _paragon_of_virtue_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue1_3"));
            _paragon_of_virtue_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue1_4"));
            _paragon_of_virtue_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue1_5"));
            _paragon_of_virtue_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue2_1"));
            _paragon_of_virtue_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue2_2"));
            _paragon_of_virtue_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue2_3"));
            _paragon_of_virtue_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue2_4"));
            _paragon_of_virtue_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ParagonOfVirtue2_5"));
        }

        protected override void InitializePassives()
        {
            //tier 1
            _knight_errant_1_passive1.Initialize(CareerID, "{=rf_career_20_hitpoints}Increases Hitpoints by 20", "KnightErrant1", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _knight_errant_1_passive2.Initialize(CareerID, "{=}Increases one handed weapon melee damage by 10%", "KnightErrant1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.OneHandedSword, WeaponClass.OneHandedAxe, WeaponClass.OneHandedPolearm, WeaponClass.Dagger, WeaponClass.Mace}, 10)));
            _knight_errant_1_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact.", "WanderiKnightErrant1ngBlade1", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.Add(new ItemRosterElement(MBObjectManager.Instance.GetObject<ItemObject>("a"))); }));
            _knight_errant_1_passive4.Initialize(CareerID, "{=}Increases ranged damage resistance by 10%.", "KnightErrant1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 10)));
            _knight_errant_1_passive5.Initialize(CareerID, "{=rf_career_wages_10_reduction}cavalry troop wages are reduced by 10%", "KnightErrant1", new CareerChoiceObject.PassiveEffect(-0.1f, PassiveEffectType.Special));

            _knight_errant_2_passive1.Initialize(CareerID, "{=}Increases mounts hitpoints by 60", "KnightErrant2", new CareerChoiceObject.PassiveEffect(60, PassiveEffectType.HorseHealth));
            _knight_errant_2_passive2.Initialize(CareerID, "{=}Increases two handed weapon melee damage by 10%", "KnightErrant2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.TwoHandedSword, WeaponClass.TwoHandedAxe, WeaponClass.TwoHandedPolearm, WeaponClass.TwoHandedMace }, 10)));
            _knight_errant_2_passive3.Initialize(CareerID, "{=}Gain a magical artifact.", "KnightErrant2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));
            _knight_errant_2_passive4.Initialize(CareerID, "{=}Increases damage from daggers, Increase magic damage resistance by 20%.", "KnightErrant2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 20)));
            _knight_errant_2_passive5.Initialize(CareerID, "{=}Increase max party size by 10", "KnightErrant2", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));

            //tier 2
            _field_marshall_1_passive1.Initialize(CareerID, "{=rf_career_20_hitpoints}Increases Hitpoints by 20", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _field_marshall_1_passive2.Initialize(CareerID, "{=}Increases one handed weapon melee damage by 10%", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.OneHandedSword, WeaponClass.OneHandedAxe, WeaponClass.OneHandedPolearm, WeaponClass.Dagger, WeaponClass.Mace }, 10)));
            _field_marshall_1_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact.", "FieldMarshall1", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.Add(new ItemRosterElement(MBObjectManager.Instance.GetObject<ItemObject>("a"))); }));
            _field_marshall_1_passive4.Initialize(CareerID, "{=}Increases ranged damage resistance by 10%.", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 10)));
            _field_marshall_1_passive5.Initialize(CareerID, "{=rf_career_wages_10_reduction}cavalry troop wages are reduced by 10%", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(-0.1f, PassiveEffectType.Special));

            _field_marshall_2_passive1.Initialize(CareerID, "{=}Increases mounts hitpoints by 60", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(60, PassiveEffectType.HorseHealth));
            _field_marshall_2_passive2.Initialize(CareerID, "{=}Increases two handed weapon melee damage by 10%", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.TwoHandedSword, WeaponClass.TwoHandedAxe, WeaponClass.TwoHandedPolearm, WeaponClass.TwoHandedMace }, 10)));
            _field_marshall_2_passive3.Initialize(CareerID, "{=}Gain a magical artifact.", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));
            _field_marshall_2_passive4.Initialize(CareerID, "{=}Increases damage from daggers, Increase magic damage resistance by 20%.", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 20)));
            _field_marshall_2_passive5.Initialize(CareerID, "{=}Increase max party size by 10", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));

            //tier 3
            _paragon_of_virtue_1_passive1.Initialize(CareerID, "{=rf_career_20_hitpoints}Increases Hitpoints by 20", "ParagonOfVirtue1", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _paragon_of_virtue_1_passive2.Initialize(CareerID, "{=}Increases one handed weapon melee damage by 10%", "ParagonOfVirtue1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.OneHandedSword, WeaponClass.OneHandedAxe, WeaponClass.OneHandedPolearm, WeaponClass.Dagger, WeaponClass.Mace }, 10)));
            _paragon_of_virtue_1_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact.", "ParagonOfVirtue1", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.Add(new ItemRosterElement(MBObjectManager.Instance.GetObject<ItemObject>("a"))); }));
            _paragon_of_virtue_1_passive4.Initialize(CareerID, "{=}Increases ranged damage resistance by 10%.", "ParagonOfVirtue1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 10)));
            _paragon_of_virtue_1_passive5.Initialize(CareerID, "{=rf_career_wages_10_reduction}cavalry troop wages are reduced by 10%", "ParagonOfVirtue1", new CareerChoiceObject.PassiveEffect(-0.1f, PassiveEffectType.Special));

            _paragon_of_virtue_2_passive1.Initialize(CareerID, "{=}Increases mounts hitpoints by 60", "ParagonOfVirtue2", new CareerChoiceObject.PassiveEffect(60, PassiveEffectType.HorseHealth));
            _paragon_of_virtue_2_passive2.Initialize(CareerID, "{=}Increases two handed weapon melee damage by 10%", "ParagonOfVirtue2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.TwoHandedSword, WeaponClass.TwoHandedAxe, WeaponClass.TwoHandedPolearm, WeaponClass.TwoHandedMace }, 10)));
            _paragon_of_virtue_2_passive3.Initialize(CareerID, "{=}Gain a magical artifact.", "ParagonOfVirtue2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));
            _paragon_of_virtue_2_passive4.Initialize(CareerID, "{=}Increases damage from daggers, Increase magic damage resistance by 20%.", "ParagonOfVirtue2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 20)));
            _paragon_of_virtue_2_passive5.Initialize(CareerID, "{=}Increase max party size by 10", "ParagonOfVirtue2", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
        }
    }
}