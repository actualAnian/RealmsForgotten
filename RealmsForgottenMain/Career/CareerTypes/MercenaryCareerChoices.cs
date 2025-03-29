using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.ObjectExtensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using System.Collections.Generic;

namespace RealmsForgotten.Career.CareerTypes
{
    public class MercenaryCareerChoices : RFCareerChoicesBase
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public MercenaryCareerChoices(CareerObject id) : base(id) { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        private CareerChoiceObject _wandering_blade_1_passive1;
        private CareerChoiceObject _wandering_blade_1_passive2;
        private CareerChoiceObject _wandering_blade_1_passive3;
        private CareerChoiceObject _wandering_blade_1_passive4;
        private CareerChoiceObject _wandering_blade_1_passive5;

        private CareerChoiceObject _wandering_blade_2_passive1;
        private CareerChoiceObject _wandering_blade_2_passive2;
        private CareerChoiceObject _wandering_blade_2_passive3;
        private CareerChoiceObject _wandering_blade_2_passive4;
        private CareerChoiceObject _wandering_blade_2_passive5;

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

        private CareerChoiceObject _mercenary_lord_1_passive1;
        private CareerChoiceObject _mercenary_lord_1_passive2;
        private CareerChoiceObject _mercenary_lord_1_passive3;
        private CareerChoiceObject _mercenary_lord_1_passive4;
        private CareerChoiceObject _mercenary_lord_1_passive5;

        private CareerChoiceObject _mercenary_lord_2_passive1;
        private CareerChoiceObject _mercenary_lord_2_passive2;
        private CareerChoiceObject _mercenary_lord_2_passive3;
        private CareerChoiceObject _mercenary_lord_2_passive4;
        private CareerChoiceObject _mercenary_lord_2_passive5;
        protected override void RegisterAll()
        {
            _wandering_blade_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_1"));
            _wandering_blade_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_2"));
            _wandering_blade_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_3"));
            _wandering_blade_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_4"));
            _wandering_blade_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade1_5"));

            _wandering_blade_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_1"));
            _wandering_blade_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_2"));
            _wandering_blade_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_3"));
            _wandering_blade_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_4"));
            _wandering_blade_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WanderingBlade2_5"));

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

            _mercenary_lord_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1_1"));
            _mercenary_lord_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1_2"));
            _mercenary_lord_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1_3"));
            _mercenary_lord_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1_4"));
            _mercenary_lord_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive1_5"));

            _mercenary_lord_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2_1"));
            _mercenary_lord_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2_2"));
            _mercenary_lord_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2_3"));
            _mercenary_lord_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2_4"));
            _mercenary_lord_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("MercenaryLordPassive2_5"));
        }

        protected override void InitializePassives()
        {
            //tier 1
            _wandering_blade_1_passive1.Initialize(CareerID, "{=rf_career_5_arrows}5 extra arrows, bolts ammo", "WanderingBlade1", new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _wandering_blade_1_passive2.Initialize(CareerID, "{=rf_career_20_hitpoints}Increases Hitpoints by 20", "WanderingBlade1", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _wandering_blade_1_passive3.Initialize(CareerID, "{=rf_career_magical_artifact}Gain a magical artifact", "WanderingBlade1", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("poisoned_knife"), 1); }));
            _wandering_blade_1_passive4.Initialize(CareerID, "{=rf_career_player_sword_axe_10}Increases one handed sword, axe damage by 10%", "WanderingBlade1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.OneHandedSword, WeaponClass.OneHandedAxe }, 10)));
            _wandering_blade_1_passive5.Initialize(CareerID, "{=rf_career_wages_10_reduction}Party wages are reduced by 10%", "WanderingBlade1", new CareerChoiceObject.PassiveEffect(-0.1f, PassiveEffectType.TroopWages));

            _wandering_blade_2_passive1.Initialize(CareerID, "{=rf_career_companion_two}Companion limit of party is increased by 2", "WanderingBlade2", new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.CompanionLimit));
            _wandering_blade_2_passive2.Initialize(CareerID, "{=rf_career_20_health_post_battle}Increases health regeneration after battles by 20", "WanderingBlade2", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.HealthRegeneration));
            _wandering_blade_2_passive3.Initialize(CareerID, "{=}Gain a magical artifact", "WanderingBlade2", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("poisoned_knife"), 1); }));
            _wandering_blade_2_passive4.Initialize(CareerID, "{=rf_career_player_dagger_mace_10}Increases damage from daggers, one-two handed maces by 10%", "WanderingBlade2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.Mace, WeaponClass.TwoHandedMace, WeaponClass.Dagger}, 10)));
            _wandering_blade_2_passive5.Initialize(CareerID, "{=rf_career_merc_no_extra_wage}Mercenary troops cost no extra wage", "WanderingBlade2", new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.Special));

            //tier 2
            _warlord_of_coin_1_passive1.Initialize(CareerID, "{=rf_career_troop_ranged_attack_15}Increases ranged damage of your troops by 15%", "WarlordOfCoin1", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Ammo));
            _warlord_of_coin_1_passive2.Initialize(CareerID, "{=rf_career_merc_contract_income_20}Increases income from mercenary contract by 20%", "WarlordOfCoin1", new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.MercContractIncome));
            _warlord_of_coin_1_passive3.Initialize(CareerID, "{=rf_career_spotting_MoFo_20}+20% spotting range in mountains and forests", "WarlordOfCoin1", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.SpottingRange,
                (characterObject) => {
                if (characterObject.HeroObject != Hero.MainHero) return false;
                var party = characterObject.HeroObject.PartyBelongedTo;
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
                return faceTerrainType == TerrainType.Forest || faceTerrainType == TerrainType.Mountain;
            }, true));
            _warlord_of_coin_1_passive4.Initialize(CareerID, "{=rf_career_player_ranged_defence_15}Increases ranged damage resistance by 15%", "WarlordOfCoin1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 15)));
            _warlord_of_coin_1_passive5.Initialize(CareerID, "{=}Gain a magical artifact", "WarlordOfCoin1", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("poisoned_knife"), 1); }));

            _warlord_of_coin_2_passive1.Initialize(CareerID, "{=rf_career_troop_melee_15}Increases melee damage your troops by 15%", "WarlordOfCoin2", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.HealthRegeneration));
            _warlord_of_coin_2_passive2.Initialize(CareerID, "{=rf_career_workshop_income_20}Increases income from workshops by 20%", "WarlordOfCoin2", new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.MercContractIncome));
            _warlord_of_coin_2_passive3.Initialize(CareerID, "{=rf_career_spotting_DeSt}+20% spotting range in deserts and steppes", "WarlordOfCoin2", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.SpottingRange,
                (characterObject) => {
                    if (characterObject.HeroObject != Hero.MainHero) return false;
                    var party = characterObject.HeroObject.PartyBelongedTo;
                    TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
                    return faceTerrainType == TerrainType.Desert || faceTerrainType == TerrainType.Steppe;
                }, true));
            _warlord_of_coin_2_passive4.Initialize(CareerID, "{=rf_career_player_melee_defence_15}Increases melee damage resistance by 15%", "WarlordOfCoin2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalMelee, 15)));
            _warlord_of_coin_2_passive5.Initialize(CareerID, "{=}Gain a special armor set", "WarlordOfCoin2", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("blackheart_sword_rfonehanded50"), 1); }));

            //tier 3
            _mercenary_lord_1_passive1.Initialize(CareerID, "{=rf_career_party_speed_FoMoSw_20}+20% party movement speed in forest, mountain and swamp terrain", "MercenaryLord1", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.PartyMovementSpeed,
            (characterObject) => {
                if (characterObject.HeroObject != Hero.MainHero) return false;
                var party = characterObject.HeroObject.PartyBelongedTo;
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
                return faceTerrainType == TerrainType.Forest || faceTerrainType == TerrainType.Mountain || faceTerrainType == TerrainType.Swamp;
            }, true));
            _mercenary_lord_1_passive2.Initialize(CareerID, "{=rf_career_troop_ranged_attack_15}Increases the damage of all ranged troops by 15%", "MercenaryLord1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalRanged, 15),
                (attacker, victim) => attacker.BelongsToMainParty() && !attacker.IsMainAgent));
            _mercenary_lord_1_passive3.Initialize(CareerID, "{=rf_career_raid_more_items}After raiding, get additional 20% of items valued less than 60", "MercenaryLord1", new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.Special));
            _mercenary_lord_1_passive4.Initialize(CareerID, "{rf_career_heal_on_kill_5}Restore 5 hitpoints on kill", "MercenaryLord1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.OnKill,
            () =>
            {
                Agent.Main.Health += 5;
            }));
            _mercenary_lord_1_passive5.Initialize(CareerID, "{=rf_career_player_melee_attack_15}Increase damage of all melee weapons by 15%", "MercenaryLord1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalMelee, 15)));

            _mercenary_lord_2_passive1.Initialize(CareerID, "{=rf_career_party_speed_SnSt_10}+10% party movement speed in snow, and steppe terrain", "MercenaryLord2", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartyMovementSpeed,
            (characterObject) => {
                if (characterObject.HeroObject != Hero.MainHero) return false;
                var party = characterObject.HeroObject.PartyBelongedTo;
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
                return faceTerrainType == TerrainType.Snow || faceTerrainType == TerrainType.Steppe;
            }, true));
            _mercenary_lord_2_passive2.Initialize(CareerID, "{=rf_career_troop_melee_attack_15}Increases the damage of all melee troops by 15%", "MercenaryLord2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalMelee, 15),
                (attacker, victim) => attacker.BelongsToMainParty() && !attacker.IsMainAgent));
            _mercenary_lord_2_passive3.Initialize(CareerID, "{=rf_career_caravan_dest_more_items}After destroying a caravan, get additional 20% of items valued less than 60", "MercenaryLord2", new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.Special));
            _mercenary_lord_2_passive4.Initialize(CareerID, "{=rf_career_40_hitpoints}Increases Hitpoints by 40", "MercenaryLord2", new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _mercenary_lord_2_passive5.Initialize(CareerID, "{=rf_career_player_ranged_attack_15}Increase damage of all ranged weapons by 15%", "MercenaryLord2", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.PhysicalRanged, 15)));
        }
    }
}