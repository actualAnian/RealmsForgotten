using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.ObjectExtensions;
using TaleWorlds.CampaignSystem;

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

            _field_marshall_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_1"));
            _field_marshall_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_2"));
            _field_marshall_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_3"));
            _field_marshall_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_4"));
            _field_marshall_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall1_5"));
            _field_marshall_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_1"));
            _field_marshall_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_2"));
            _field_marshall_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_3"));
            _field_marshall_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_4"));
            _field_marshall_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("FieldMarshall2_5"));

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
            _knight_errant_1_passive2.Initialize(CareerID, "{=rf_career_player_one_handed_10}Increases one handed weapon melee damage by 10%", "KnightErrant1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.OneHandedSword, WeaponClass.OneHandedAxe, WeaponClass.OneHandedPolearm, WeaponClass.Dagger, WeaponClass.Mace}, 10)));
            _knight_errant_1_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact", "KnightErrant1", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("poisoned_knife"), 1); }));
            _knight_errant_1_passive4.Initialize(CareerID, "{=rf_career_player_ranged_defence_10}Increases ranged damage resistance by 10%.", "KnightErrant1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 10)));
            _knight_errant_1_passive5.Initialize(CareerID, "{=rf_career_wages_10_reduction}cavalry troop wages are reduced by 10%", "KnightErrant1", new CareerChoiceObject.PassiveEffect(-0.1f, PassiveEffectType.Special));

            _knight_errant_2_passive1.Initialize(CareerID, "{=rf_career_60_mount_hitpoints}Increases mounts hitpoints by 60", "KnightErrant2", new CareerChoiceObject.PassiveEffect(60, PassiveEffectType.HorseHealth));
            _knight_errant_2_passive2.Initialize(CareerID, "{=rf_career_player_two_handed_10}Increases two handed weapon melee damage by 10%", "KnightErrant2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.TwoHandedSword, WeaponClass.TwoHandedAxe, WeaponClass.TwoHandedPolearm, WeaponClass.TwoHandedMace }, 10)));
            _knight_errant_2_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact.", "KnightErrant2", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("poisoned_knife"), 1); }));
            _knight_errant_2_passive4.Initialize(CareerID, "{=rf_career_player_magic_defence_20}Increase magic damage resistance by 20%", "KnightErrant2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 20)));
            _knight_errant_2_passive5.Initialize(CareerID, "{=rf_career_party_size_20}Increase max party size by 20", "KnightErrant2", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartySize));

            //tier 2
            _field_marshall_1_passive1.Initialize(CareerID, "{=rf_career_troop_melee_attack_20_mounted_player_captain}Increases melee damage of mounted troops, if you are their formation leader by 20%", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalMelee, 20), (Agent attacker, Agent victim) => { 
                return attacker.BelongsToMainParty() && attacker.Mount != null && attacker.Formation != null && attacker.Formation.Captain == Agent.Main; }));
            _field_marshall_1_passive2.Initialize(CareerID, "{=rf_career_no_stagger_15}Hits below 15 damage do not stagger the player", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special));
            _field_marshall_1_passive3.Initialize(CareerID, "{=rf_career_40_hitpoints}Increase hitpoints by 40", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _field_marshall_1_passive4.Initialize(CareerID, "{=rf_career_troop_heal_20}Your troops heal 20% faster", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.TroopRegeneration));
            _field_marshall_1_passive5.Initialize(CareerID, "{=rf_career_town_income_10}Increases town income by 10%", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(0.1f, PassiveEffectType.TownIncome));

            _field_marshall_2_passive1.Initialize(CareerID, "{=rf_career_troop_melee_attack_10_mounted}Increases melee damage of mounted troops by 10%", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalMelee, 10), (Agent attacker, Agent victim) => { return attacker.BelongsToMainParty() && attacker.Mount != null; }));
            _field_marshall_2_passive2.Initialize(CareerID, "{=rf_career_horse_speed_10}Increases horse speed by 10%", "FieldMarshall2", new CareerChoiceObject.AgentPropertiesPassiveEffect((Agent ag, AgentDrivenProperties pr) => { if (ag.Character != null && ag.Character.IsPlayerCharacter) pr.MountSpeed  += pr.MountSpeed * 1/10; }));
            _field_marshall_2_passive3.Initialize(CareerID, "{=rf_career_40_hitpoints_companions}Increase your companions hitpoints by 40", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.CompanionHealth));
            _field_marshall_2_passive4.Initialize(CareerID, "{=rf_career_troops_wounded_5}Your troops have 5% higher chance to be wounded instead of killed in battle", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(0.05f, PassiveEffectType.WoundedChance));
            _field_marshall_2_passive5.Initialize(CareerID, "{=rf_career_village_income_10}Increase village income by 10%", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(0.1f, PassiveEffectType.VillageIncome));

            //tier 3
            _paragon_of_virtue_1_passive1.Initialize(CareerID, "{=rf_career_mount_charge_40}Increases your mounts charge damage by 40%", "ParagonOfVirtue1", new CareerChoiceObject.AgentPropertiesPassiveEffect((Agent ag, AgentDrivenProperties pr) =>
            {
                if (ag.Character != CharacterObject.PlayerCharacter) return;
                pr.MountChargeDamage += pr.MountChargeDamage * 4 / 10;
            }));
            _paragon_of_virtue_1_passive2.Initialize(CareerID, "{=rf_career_player_two_handed_sword_axe_mace_both_polearm_20_mounted}While mounted, increases two handed sword, axe, mace, and one-two handed polearm weapon damage by 20%", "ParagonOfVirtue1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.TwoHandedAxe, WeaponClass.TwoHandedSword, WeaponClass.TwoHandedMace, WeaponClass.LowGripPolearm, WeaponClass.OneHandedPolearm, WeaponClass.TwoHandedPolearm }, 20), (Agent attacker, Agent victim) => { return attacker.Character != null && attacker.Character.IsPlayerCharacter && attacker.Mount != null; } ));
            _paragon_of_virtue_1_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact", "ParagonOfVirtue1", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("poisoned_knife"), 1); }));
            _paragon_of_virtue_1_passive4.Initialize(CareerID, "{=rf_career_troop_melee_15_infantry}Your infantry get 15% melee damage resistance.", "FieldMarshall1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.PhysicalMelee, 15), (Agent attacker, Agent victim) => { return victim.BelongsToMainParty() && victim.Character != null && victim.Character.IsInfantry; }));
            _paragon_of_virtue_1_passive5.Initialize(CareerID, "{=rf_career_cav_thrust_reload_10}Your cavalry thrust their weapons and reload ranged weapons 10% faster", "ParagonOfVirtue1", new CareerChoiceObject.AgentPropertiesPassiveEffect((Agent ag, AgentDrivenProperties pr) =>
            {
                if (ag.Character == null || !ag.BelongsToMainParty() || !ag.Character.IsMounted) return;
                pr.ThrustOrRangedReadySpeedMultiplier += pr.ThrustOrRangedReadySpeedMultiplier * 1 / 10;
                pr.ReloadSpeed += pr.ReloadSpeed * 1 / 10;
            }));

            _paragon_of_virtue_2_passive1.Initialize(CareerID, "{=rf_career_mount_dash_30}Increases mounts dash acceleration by 30%", "ParagonOfVirtue2", new CareerChoiceObject.AgentPropertiesPassiveEffect((Agent ag, AgentDrivenProperties pr) =>
            {
                if (ag.Character != CharacterObject.PlayerCharacter) return;
                pr.MountDashAccelerationMultiplier += pr.MountChargeDamage * 3 / 10;
            }));
            _paragon_of_virtue_2_passive2.Initialize(CareerID, "{=rf_career_player_one_handed_sword_axe_mace_20_mounted}While mounted, increases one handed sword, axe, mace weapon damage by 20%", "ParagonOfVirtue2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.OneHandedSword, WeaponClass.OneHandedAxe, WeaponClass.Mace}, 20), (Agent attacker, Agent victim) => { return attacker.Character != null && attacker.Character.IsPlayerCharacter && attacker.Mount != null; }));
            _paragon_of_virtue_2_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact", "ParagonOfVirtue2", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("poisoned_knife"), 1); }));
            _paragon_of_virtue_2_passive4.Initialize(CareerID, "{=rf_career_troop_melee_30_ranged}Your ranged units get 30% melee damage resistance,", "FieldMarshall2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.PhysicalMelee, 30), (Agent attacker, Agent victim) => { return victim.BelongsToMainParty() && victim.Character != null && victim.Character.IsRanged; }));
            _paragon_of_virtue_2_passive5.Initialize(CareerID, "{=rf_career_cav_swing_10}Your cavalry troops swing their weapons 10% faster", "ParagonOfVirtue2", new CareerChoiceObject.AgentPropertiesPassiveEffect((Agent ag, AgentDrivenProperties pr) =>
            {
                if (ag.Character == null || !ag.BelongsToMainParty() || !ag.Character.IsMounted) return;
                pr.SwingSpeedMultiplier += pr.SwingSpeedMultiplier * 1 / 10;
            }));
        }
    }
}