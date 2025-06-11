using RealmsForgotten.ObjectExtensions;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Career.CareerTypes
{
    public class LightwardenCareerChoice : RFCareerChoicesBase
    {
        public LightwardenCareerChoice(CareerObject id) : base(id) { }

        private CareerChoiceObject _word_spreader_1_passive1;
        private CareerChoiceObject _word_spreader_1_passive2;
        private CareerChoiceObject _word_spreader_1_passive3;
        private CareerChoiceObject _word_spreader_1_passive4;
        private CareerChoiceObject _word_spreader_1_passive5;

        private CareerChoiceObject _word_spreader_2_passive1;
        private CareerChoiceObject _word_spreader_2_passive2;
        private CareerChoiceObject _word_spreader_2_passive3;
        private CareerChoiceObject _word_spreader_2_passive4;
        private CareerChoiceObject _word_spreader_2_passive5;

        private CareerChoiceObject _blessed_one_1_passive1;
        private CareerChoiceObject _blessed_one_1_passive2;
        private CareerChoiceObject _blessed_one_1_passive3;
        private CareerChoiceObject _blessed_one_1_passive4;
        private CareerChoiceObject _blessed_one_1_passive5;

        private CareerChoiceObject _blessed_one_2_passive1;
        private CareerChoiceObject _blessed_one_2_passive2;
        private CareerChoiceObject _blessed_one_2_passive3;
        private CareerChoiceObject _blessed_one_2_passive4;
        private CareerChoiceObject _blessed_one_2_passive5;

        private CareerChoiceObject _beacon_of_light_1_passive1;
        private CareerChoiceObject _beacon_of_light_1_passive2;
        private CareerChoiceObject _beacon_of_light_1_passive3;
        private CareerChoiceObject _beacon_of_light_1_passive4;
        private CareerChoiceObject _beacon_of_light_1_passive5;

        private CareerChoiceObject _beacon_of_light_2_passive1;
        private CareerChoiceObject _beacon_of_light_2_passive2;
        private CareerChoiceObject _beacon_of_light_2_passive3;
        private CareerChoiceObject _beacon_of_light_2_passive4;
        private CareerChoiceObject _beacon_of_light_2_passive5;
        protected override void RegisterAll()
        {

            _word_spreader_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader1_1"));
            _word_spreader_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader1_2"));
            _word_spreader_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader1_3"));
            _word_spreader_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader1_4"));
            _word_spreader_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader1_5"));

            _word_spreader_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader2_1"));
            _word_spreader_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader2_2"));
            _word_spreader_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader2_3"));
            _word_spreader_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader2_4"));
            _word_spreader_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WordSpeader2_5"));

            _blessed_one_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne1_1"));
            _blessed_one_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne1_2"));
            _blessed_one_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne1_3"));
            _blessed_one_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne1_4"));
            _blessed_one_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne1_5"));

            _blessed_one_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne2_1"));
            _blessed_one_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne2_2"));
            _blessed_one_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne2_3"));
            _blessed_one_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne2_4"));
            _blessed_one_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BlessedOne2_5"));

            _beacon_of_light_1_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight1_1"));
            _beacon_of_light_1_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight1_2"));
            _beacon_of_light_1_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight1_3"));
            _beacon_of_light_1_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight1_4"));
            _beacon_of_light_1_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight1_5"));

            _beacon_of_light_2_passive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight2_1"));
            _beacon_of_light_2_passive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight2_2"));
            _beacon_of_light_2_passive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight2_3"));
            _beacon_of_light_2_passive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight2_4"));
            _beacon_of_light_2_passive5 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BeaconOfLight2_5"));
        }

        protected override void InitializePassives()
        {
            //Tier 1
            _word_spreader_1_passive1.Initialize(CareerID, "{=rf_career_influence}Increases influence gain by 5%", "WordSpeader1", new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.InfluenceIncome));
            _word_spreader_1_passive2.Initialize(CareerID, "{=rf_career_player_two_handed_10}Increases two handed weapon melee damage by 10%", "WordSpeader1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass>() { WeaponClass.TwoHandedSword, WeaponClass.TwoHandedPolearm, WeaponClass.TwoHandedMace }, 10)));
            _word_spreader_1_passive3.Initialize(CareerID, "{=rf_career_party_morale}Increases the party morale gain by 5%", "WordSpeader1", new CareerChoiceObject.PassiveEffect(0.05f, PassiveEffectType.PartyMorale));
            _word_spreader_1_passive4.Initialize(CareerID, "{=rf_career_player_melee_defence_10}Increases melee damage resistance by 10%", "WordSpeader1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));
            _word_spreader_1_passive5.Initialize(CareerID, "{=rf_career_party_reduced_5}Party wages are reduced by 10%", "WordSpeader1", new CareerChoiceObject.PassiveEffect(-0.1f, PassiveEffectType.Special));

            _word_spreader_2_passive1.Initialize(CareerID, "{=rf_career_recruitment_cost}Recruitment and upgrade cost of infantry troops 5% cheaper", "WordSpeader2", new CareerChoiceObject.PassiveEffect(-0.05f, PassiveEffectType.Special));
            _word_spreader_2_passive2.Initialize(CareerID, "{=rf_career_20_hitpoints}Increases hitpoints by 20", "WordSpeader2", new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Health));
            _word_spreader_2_passive3.Initialize(CareerID, "{=rf_career_influence}Increases influence gain by 5%", "WordSpeader2", new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.InfluenceIncome));
            _word_spreader_2_passive4.Initialize(CareerID, "{=}Gain a magical artifact", "WordSpeader2", new CareerChoiceObject.ActiveEffect(() => { MobileParty.MainParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("Sworn Sword"), 1); }));
            _word_spreader_2_passive5.Initialize(CareerID, "{=rf_career_5_health_in_map}Increases health regeneration in map", "WordSpeader2", new CareerChoiceObject.PassiveEffect(0.05f, PassiveEffectType.HealthRegeneration));

            //Tier 2
            _word_spreader_1_passive1.Initialize(CareerID, "{=rf_career_renown}Increases renown gain by 10%", "BlessedOne1", new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.RenownIncome));
            _word_spreader_1_passive2.Initialize(CareerID, "{=rf_career_troop_melee_attack_20_infantary_player_captain}Increases melee damage of infantry troops, if you are their formation leader by 20%", "BlessedOne1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalMelee, 20), (Agent attacker, Agent victim) => {
                return attacker.BelongsToMainParty() && attacker.Formation != null && attacker.Formation.Captain == Agent.Main;
            }));
            _word_spreader_1_passive3.Initialize(CareerID, "{=rf_career_party_morale_10}Increases the party morale gain by 10%", "BlessedOne1", new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.PartyMorale));
            _word_spreader_1_passive4.Initialize(CareerID, "{=rf_career_player_ranged_defence_15}Increases ranged damage resistance by 15%.", "BlessedOne1", new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 15)));
            _word_spreader_1_passive5.Initialize(CareerID, "{=rf_career_temples_generate}Owned Temples generate 1 Virtue and Sacred Favor per day", "BlessedOne1", new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special));

            _word_spreader_2_passive1.Initialize(CareerID, "{=rf_career_recruitment_cost}Recruitment and upgrade cost of infantry troops 5% cheaper", "BlessedOne2", new CareerChoiceObject.PassiveEffect(-0.05f, PassiveEffectType.Special));
            _word_spreader_2_passive2.Initialize(CareerID, "{=rf_career_40_hitpoints}Increases Hitpoints by 40", "BlessedOne2", new CareerChoiceObject.PassiveEffect(40, PassiveEffectType.Health));
            _word_spreader_2_passive3.Initialize(CareerID, "{=rf_career_militia_fief}Increases militia of owned fiefs by 10% ", "BlessedOne2", new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.Special));
            _word_spreader_2_passive4.Initialize(CareerID, "{=rf_career_damage_bandits}When fighting against bandits the melee damage of you and your troops is raised by 15%", "BlessedOne2", new CareerChoiceObject.PassiveEffect(0.15f, PassiveEffectType.Special));
            _word_spreader_2_passive5.Initialize(CareerID, "{=rf_career_520_health_in_map}Increases health regeneration in map", "BlessedOne2", new CareerChoiceObject.PassiveEffect(0.20f, PassiveEffectType.HealthRegeneration));

            //Tier 3
            _beacon_of_light_1_passive1.Initialize(CareerID, "{=rf_career_influence_10}Increases influence gain by 10%", "BeaconOfLight1", new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.InfluenceIncome));

            _beacon_of_light_1_passive2.Initialize(CareerID, "{=rf_career_mount_hp_150}Increases mount hitpoints by 150", "BeaconOfLight1", new CareerChoiceObject.PassiveEffect(150, PassiveEffectType.HorseHealth));

            _beacon_of_light_1_passive3.Initialize(CareerID,"{=rf_career_governor_bonus}While governor of a city/castle, loyalty, food, and militia stats are increased by 10%", "BeaconOfLight1", new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.Special));

            _beacon_of_light_1_passive4.Initialize(CareerID, "{=rf_career_troop_hp_leader}Increases max hitpoints of troops by 15% when you are their formation leader", "BeaconOfLight1", new CareerChoiceObject.PassiveEffect(0.15f, PassiveEffectType.Special));

            _beacon_of_light_1_passive5.Initialize(CareerID, "{=rf_career_temples_virtue}Owned temples generate 5 Virtue and Sacred Favor per day and unlock artifact purchase", "BeaconOfLight1", new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special));

            _beacon_of_light_2_passive1.Initialize(CareerID, "{=rf_career_fief_militia_10}Increases militia of owned fiefs by 10%",
                "BeaconOfLight2",
                new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.Special));

            _beacon_of_light_2_passive2.Initialize(CareerID,
                "{=rf_career_onehanded_15}Increases one-handed weapon damage (sword, axe, mace) by 15%",
                "BeaconOfLight2",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage,
                    new DamageProportionTuple(new List<WeaponClass>
                    {
                        WeaponClass.OneHandedSword,
                        WeaponClass.OneHandedAxe,
                        WeaponClass.Mace
                    }, 15)));

            _beacon_of_light_2_passive3.Initialize(CareerID,
                "{=rf_career_wages_20_reduction}Party wages cost decreased by 20%",
                "BeaconOfLight2",
                new CareerChoiceObject.PassiveEffect(-0.20f, PassiveEffectType.Special));

            _beacon_of_light_2_passive4.Initialize(CareerID,
                "{=rf_career_scourgebane_artifact}Gain a magical artifact (Scourgebane)",
                "BeaconOfLight2",
                new CareerChoiceObject.ActiveEffect(() =>
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>("Scourgebane");
                    MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
                }));

            _beacon_of_light_2_passive5.Initialize(CareerID,
                "{=rf_career_infantry_resist_cav_buff}Infantry units gain 30% melee resistance; when Radiant Benediction is active, cavalry gain this resistance for 10 seconds",
                "BeaconOfLight2",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance,
                    new DamageProportionTuple(DamageType.PhysicalMelee, 30),
                    (Agent attacker, Agent victim) => victim.BelongsToMainParty() && victim.Character != null && victim.Character.IsInfantry));


        }

    }
}
