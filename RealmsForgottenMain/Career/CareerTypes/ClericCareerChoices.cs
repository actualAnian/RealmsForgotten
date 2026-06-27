using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.CareerTypes
{
    public class ClericCareerChoices : RFCareerChoicesBase
    {
#pragma warning disable CS8618
        public ClericCareerChoices(CareerObject id) : base(id) { }
#pragma warning restore CS8618

        private CareerChoiceObject TM_1, TM_2, TM_3, TM_4, TM_5;
        private CareerChoiceObject HS_1, HS_2, HS_3, HS_4, HS_5;
        private CareerChoiceObject CW_1, CW_2, CW_3, CW_4, CW_5;
        private CareerChoiceObject BM_1, BM_2, BM_3, BM_4, BM_5;
        private CareerChoiceObject WP_1, WP_2, WP_3, WP_4, WP_5;
        private CareerChoiceObject SI_1, SI_2, SI_3, SI_4, SI_5;

        protected override void RegisterAll()
        {
            CareerChoiceObject Reg(string id) => Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(id));

            TM_1 = Reg("ClericTempleMilitant1_1");
            TM_2 = Reg("ClericTempleMilitant1_2");
            TM_3 = Reg("ClericTempleMilitant1_3");
            TM_4 = Reg("ClericTempleMilitant1_4");
            TM_5 = Reg("ClericTempleMilitant1_5");

            HS_1 = Reg("ClericHumbleShepherd1_1");
            HS_2 = Reg("ClericHumbleShepherd1_2");
            HS_3 = Reg("ClericHumbleShepherd1_3");
            HS_4 = Reg("ClericHumbleShepherd1_4");
            HS_5 = Reg("ClericHumbleShepherd1_5");

            CW_1 = Reg("ClericConsecratedWarden1_1");
            CW_2 = Reg("ClericConsecratedWarden1_2");
            CW_3 = Reg("ClericConsecratedWarden1_3");
            CW_4 = Reg("ClericConsecratedWarden1_4");
            CW_5 = Reg("ClericConsecratedWarden1_5");

            BM_1 = Reg("ClericBearerOfMercy1_1");
            BM_2 = Reg("ClericBearerOfMercy1_2");
            BM_3 = Reg("ClericBearerOfMercy1_3");
            BM_4 = Reg("ClericBearerOfMercy1_4");
            BM_5 = Reg("ClericBearerOfMercy1_5");

            WP_1 = Reg("ClericWarPriest1_1");
            WP_2 = Reg("ClericWarPriest1_2");
            WP_3 = Reg("ClericWarPriest1_3");
            WP_4 = Reg("ClericWarPriest1_4");
            WP_5 = Reg("ClericWarPriest1_5");

            SI_1 = Reg("ClericSaintedIntercessor1_1");
            SI_2 = Reg("ClericSaintedIntercessor1_2");
            SI_3 = Reg("ClericSaintedIntercessor1_3");
            SI_4 = Reg("ClericSaintedIntercessor1_4");
            SI_5 = Reg("ClericSaintedIntercessor1_5");
        }

        protected override void InitializePassives()
        {
            TM_1.Initialize(CareerID, "{=rf_cleric_tm1}Increase mace damage by 10%", "ClericTempleMilitant1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass> { WeaponClass.Mace, WeaponClass.TwoHandedMace }, 10)));
            TM_2.Initialize(CareerID, "{=rf_cleric_tm2}Increase one handed sword damage by 10%", "ClericTempleMilitant1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass> { WeaponClass.OneHandedSword }, 10)));
            TM_3.Initialize(CareerID, "{=rf_cleric_tm3}Increase ranged damage resistance by 10%", "ClericTempleMilitant1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 10)));
            TM_4.Initialize(CareerID, "{=rf_cleric_tm4}Increase hitpoints by 10", "ClericTempleMilitant1",
                new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
            TM_5.Initialize(CareerID, "{=rf_cleric_tm5}Increase melee damage resistance by 10%", "ClericTempleMilitant1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));

            HS_1.Initialize(CareerID, "{=rf_cleric_hs1}+10 post-battle healing for the player", "ClericHumbleShepherd1",
                new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.HealthRegeneration));
            HS_2.Initialize(CareerID, "{=rf_cleric_hs2}Your troops heal 15% faster", "ClericHumbleShepherd1",
                new CareerChoiceObject.PassiveEffect(0.15f, PassiveEffectType.TroopRegeneration));
            HS_3.Initialize(CareerID, "{=rf_cleric_hs3}Increase companion limit by 1", "ClericHumbleShepherd1",
                new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.CompanionLimit));
            HS_4.Initialize(CareerID, "{=rf_cleric_hs4}+10% spotting range", "ClericHumbleShepherd1",
                new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.SpottingRange));
            HS_5.Initialize(CareerID, "{=rf_cleric_hs5}Your troops have 5% higher chance to be wounded instead of killed", "ClericHumbleShepherd1",
                new CareerChoiceObject.PassiveEffect(0.05f, PassiveEffectType.WoundedChance));

            CW_1.Initialize(CareerID, "{=rf_cleric_cw1}Increase melee damage resistance by 15%", "ClericConsecratedWarden1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalMelee, 15)));
            CW_2.Initialize(CareerID, "{=rf_cleric_cw2}Increase ranged damage resistance by 10%", "ClericConsecratedWarden1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.PhysicalRanged, 10)));
            CW_3.Initialize(CareerID, "{=rf_cleric_cw3}Your troops gain 10% melee damage resistance", "ClericConsecratedWarden1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));
            CW_4.Initialize(CareerID, "{=rf_cleric_cw4}Reduce troop losses when escaping a battle", "ClericConsecratedWarden1",
                new CareerChoiceObject.PassiveEffect(0.20f, PassiveEffectType.Special));
            CW_5.Initialize(CareerID, "{=rf_cleric_cw5}Increase companion hitpoints by 20", "ClericConsecratedWarden1",
                new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.CompanionHealth));

            BM_1.Initialize(CareerID, "{=rf_cleric_bm1}+15% post-battle healing for the player", "ClericBearerOfMercy1",
                new CareerChoiceObject.PassiveEffect(0.15f, PassiveEffectType.Special));
            BM_2.Initialize(CareerID, "{=rf_cleric_bm2}+15% post-battle healing for troops", "ClericBearerOfMercy1",
                new CareerChoiceObject.PassiveEffect(0.15f, PassiveEffectType.Special));
            BM_3.Initialize(CareerID, "{=rf_cleric_bm3}Bandit prisoners are easier to recruit", "ClericBearerOfMercy1",
                new CareerChoiceObject.PassiveEffect(0.25f, PassiveEffectType.Special));
            BM_4.Initialize(CareerID, "{=rf_cleric_bm4}Prisoner recruitment causes 50% less morale penalty", "ClericBearerOfMercy1",
                new CareerChoiceObject.PassiveEffect(0.50f, PassiveEffectType.Special));
            BM_5.Initialize(CareerID, "{=rf_cleric_bm5}Increase max party size by 10", "ClericBearerOfMercy1",
                new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));

            WP_1.Initialize(CareerID, "{=rf_cleric_wp1}Increase melee damage with swords and maces by 15%", "ClericWarPriest1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(new List<WeaponClass> { WeaponClass.OneHandedSword, WeaponClass.Mace, WeaponClass.TwoHandedMace }, 15)));
            WP_2.Initialize(CareerID, "{=rf_cleric_wp2}Your troops deal 10% more melee damage", "ClericWarPriest1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.PhysicalMelee, 10)));
            WP_3.Initialize(CareerID, "{=rf_cleric_wp3}Restore 5 hitpoints on kill", "ClericWarPriest1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.OnKill, () =>
                {
                    if (Agent.Main != null)
                        Agent.Main.Health = TaleWorlds.Library.MathF.Min(Agent.Main.HealthLimit, Agent.Main.Health + 5);
                }));
            WP_4.Initialize(CareerID, "{=rf_cleric_wp4}+10% campaign map movement speed while at war", "ClericWarPriest1",
                new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.PartyMovementSpeed,
                    characterObject =>
                    {
                        if (characterObject.HeroObject != Hero.MainHero)
                            return false;

                        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
                        return kingdom != null && kingdom.FactionsAtWarWith.Count > 0;
                    }, OperationType.Multiply));
            WP_5.Initialize(CareerID, "{=rf_cleric_wp5}Your troops gain 10% magical damage resistance", "ClericWarPriest1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Magical, 10)));

            SI_1.Initialize(CareerID, "{=rf_cleric_si1}Divine Restoration heals 20% more", "ClericSaintedIntercessor1",
                new CareerChoiceObject.PassiveEffect(0.20f, PassiveEffectType.Special));
            SI_2.Initialize(CareerID, "{=rf_cleric_si2}Divine Restoration lasts 5 seconds longer", "ClericSaintedIntercessor1",
                new CareerChoiceObject.PassiveEffect(5f, PassiveEffectType.Special));
            SI_3.Initialize(CareerID, "{=rf_cleric_si3}Divine Restoration also heals nearby allies", "ClericSaintedIntercessor1",
                new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.Special));
            SI_4.Initialize(CareerID, "{=rf_cleric_si4}Divine Restoration bolsters the morale of nearby allies", "ClericSaintedIntercessor1",
                new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.Special));
            SI_5.Initialize(CareerID, "{=rf_cleric_si5}After major battles, 10% of your wounded recover immediately", "ClericSaintedIntercessor1",
                new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.Special));
        }
    }
}
