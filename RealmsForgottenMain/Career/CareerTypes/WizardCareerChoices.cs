using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.ObjectExtensions;
using RealmsForgotten.Career.Ability;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.CareerTypes
{
    public class WizardCareerChoices : RFCareerChoicesBase
    {
#pragma warning disable CS8618
        public WizardCareerChoices(CareerObject id) : base(id) { }
#pragma warning restore CS8618

        // Apprentice
        private CareerChoiceObject A1_1, A1_2, A1_3, A1_4, A1_5;
        private CareerChoiceObject A2_1, A2_2, A2_3, A2_4, A2_5;

        // Magus
        private CareerChoiceObject M1_1, M1_2, M1_3, M1_4, M1_5;
        private CareerChoiceObject M2_1, M2_2, M2_3, M2_4, M2_5;

        // Archmage
        private CareerChoiceObject R1_1, R1_2, R1_3, R1_4, R1_5;
        private CareerChoiceObject R2_1, R2_2, R2_3, R2_4, R2_5;

        protected override void RegisterAll()
        {
            CareerChoiceObject Reg(string id) =>
                Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(id));

            // Apprentice
            A1_1 = Reg("WizardApprentice1_1");
            A1_2 = Reg("WizardApprentice1_2");
            A1_3 = Reg("WizardApprentice1_3");
            A1_4 = Reg("WizardApprentice1_4");
            A1_5 = Reg("WizardApprentice1_5");

            A2_1 = Reg("WizardApprentice2_1");
            A2_2 = Reg("WizardApprentice2_2");
            A2_3 = Reg("WizardApprentice2_3");
            A2_4 = Reg("WizardApprentice2_4");
            A2_5 = Reg("WizardApprentice2_5");

            // Magus
            M1_1 = Reg("WizardMagus1_1");
            M1_2 = Reg("WizardMagus1_2");
            M1_3 = Reg("WizardMagus1_3");
            M1_4 = Reg("WizardMagus1_4");
            M1_5 = Reg("WizardMagus1_5");

            M2_1 = Reg("WizardMagus2_1");
            M2_2 = Reg("WizardMagus2_2");
            M2_3 = Reg("WizardMagus2_3");
            M2_4 = Reg("WizardMagus2_4");
            M2_5 = Reg("WizardMagus2_5");

            // Archmage
            R1_1 = Reg("WizardArchmage1_1");
            R1_2 = Reg("WizardArchmage1_2");
            R1_3 = Reg("WizardArchmage1_3");
            R1_4 = Reg("WizardArchmage1_4");
            R1_5 = Reg("WizardArchmage1_5");

            R2_1 = Reg("WizardArchmage2_1");
            R2_2 = Reg("WizardArchmage2_2");
            R2_3 = Reg("WizardArchmage2_3");
            R2_4 = Reg("WizardArchmage2_4");
            R2_5 = Reg("WizardArchmage2_5");
        }

        protected override void InitializePassives()
        {
            // CORREÇÃO: IDs de itens mágicos foram substituídos por um item real ("poisoned_knife") para evitar crashes.
            // Lembre-se de substituir "poisoned_knife" pelos IDs dos seus itens customizados.
            const string placeholderItemId = "poisoned_knife"; // TODO: Replace with your actual magic item IDs

            // ─────────────────────────────────────────────────────────────────
            // TIER 1 — APPRENTICE (Lane A)
            A1_1.Initialize(CareerID, "{=rf_wiz_t1a1}+10% melee damage resistance (player)",
                "WizardApprentice1",
                new CareerChoiceObject.PassiveEffect(
                    PassiveEffectType.Resistance,
                    new DamageProportionTuple(DamageType.PhysicalMelee, 10)));

            A1_2.Initialize(CareerID, "{=rf_wiz_t1a2}+20% spotting range",
                "WizardApprentice1",
                new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.SpottingRange));

            A1_3.Initialize(CareerID, "{=rf_wiz_t1a3}-30% bandit bribe cost",
                "WizardApprentice1",
                new CareerChoiceObject.PassiveEffect(-0.30f, PassiveEffectType.Special)); // handled in CampaignBehavior

            A1_4.Initialize(CareerID, "{=rf_wiz_t1a4}+10% magic damage resistance (player)",
                "WizardApprentice1",
                new CareerChoiceObject.PassiveEffect(
                    PassiveEffectType.Resistance,
                    new DamageProportionTuple(DamageType.Magical, 10)));

            {   // A1_5: +10% magical vs RANGED when PLAYER attacks with Cartridge
                var tuple = new DamageProportionTuple(DamageType.Magical, 10);
                tuple.WeaponClasses = new System.Collections.Generic.List<WeaponClass> { WeaponClass.Cartridge };

                A1_5.Initialize(CareerID, "{=rf_wiz_t1a5}+10% spell damage vs RANGED troops (player; cartridge only)",
                    "WizardApprentice1",
                    new CareerChoiceObject.PassiveEffect(
                        PassiveEffectType.Damage,
                        tuple,
                        (Agent attacker, Agent victim) =>
                            attacker.IsMainAgent &&
                            victim != null &&
                            victim.Character != null &&
                            victim.Character.DefaultFormationClass == FormationClass.Ranged
                    ));
            }

            // ───────────────────────────────────────────────────────────
            // TIER 1 — APPRENTICE (Lane B)
            A2_1.Initialize(CareerID, "{=rf_wiz_t1b1}+10% ranged damage resistance (player)",
                "WizardApprentice2",
                new CareerChoiceObject.PassiveEffect(
                    PassiveEffectType.Resistance,
                    new DamageProportionTuple(DamageType.PhysicalRanged, 10)));

            A2_2.Initialize(CareerID, "{=rf_wiz_t1b2}+10% party movement speed",
                "WizardApprentice2",
                new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartyMovementSpeed));

            A2_3.Initialize(CareerID, "{=rf_wiz_t1b3}Reduce troop losses when escaping a battle",
                "WizardApprentice2",
                new CareerChoiceObject.PassiveEffect(0.30f, PassiveEffectType.Special)); // handled in CampaignBehavior

            A2_4.Initialize(CareerID, "{=rf_wiz_t1b4}Troops +10% magic damage resistance",
                "WizardApprentice2",
                new CareerChoiceObject.PassiveEffect(
                    PassiveEffectType.TroopResistance,
                    new DamageProportionTuple(DamageType.Magical, 10),
                    (Agent attacker, Agent victim) =>
                        victim.BelongsToMainParty() && !victim.IsMainAgent));

            {   // A2_5: +10% magical vs MELEE (Infantry) when YOUR TROOPS attack with Cartridge
                var tuple = new DamageProportionTuple(DamageType.Magical, 10);
                tuple.WeaponClasses = new System.Collections.Generic.List<WeaponClass> { WeaponClass.Cartridge };

                A2_5.Initialize(CareerID, "{=rf_wiz_t1b5}Troops: +10% spell damage vs MELEE troops (cartridge only)",
                    "WizardApprentice2",
                    new CareerChoiceObject.PassiveEffect(
                        PassiveEffectType.TroopDamage,
                        tuple,
                        (Agent attacker, Agent victim) =>
                            attacker.BelongsToMainParty() && !attacker.IsMainAgent &&
                            victim != null &&
                            victim.Character != null &&
                            victim.Character.DefaultFormationClass == FormationClass.Infantry
                    ));
            }

            // ───────────────────────────────────────────────────────────
            // TIER 2 — MAGUS
            // Lane 1
            M1_1.Initialize(CareerID, "{=rf_wiz_t2_m1_1}Increase spell stack amount by 10%",
                "WizardMagus1",
                new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.Special)); // handled in code

            M1_2.Initialize(CareerID, "{=rf_wiz_t2_m1_2}+10 post‑battle healing (player)",
                "WizardMagus1",
                new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.HealthRegeneration));

            M1_3.Initialize(CareerID, "{=rf_wiz_t2_m1_3}Increase spell area effect by 5%",
                "WizardMagus1",
                new CareerChoiceObject.PassiveEffect(0.05f, PassiveEffectType.Special)); // handled in code

            M1_4.Initialize(CareerID, "{=rf_wiz_t2_m1_4}Gain a magic item",
                "WizardMagus1",
                new CareerChoiceObject.ActiveEffect(() => AbilityEffects.GiveItemById(placeholderItemId)));

            M1_5.Initialize(CareerID, "{=rf_wiz_t2_m1_5}Immune to melee damage for 30s (player)",
                "WizardMagus1",
                new CareerChoiceObject.PassiveEffect(30f, PassiveEffectType.Special)); // handled in code

            // Lane 2
            M2_1.Initialize(CareerID, "{=rf_wiz_t2_m2_1}Increase spell damage by 5%",
                "WizardMagus2",
                new CareerChoiceObject.PassiveEffect(
                    PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 5)));

            M2_2.Initialize(CareerID, "{=rf_wiz_t2_m2_2}+10 post‑battle healing (troops)",
                "WizardMagus2",
                new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special)); // handled in campaign behavior

            M2_3.Initialize(CareerID, "{=rf_wiz_t2_m2_3}Increase spell damage by 10%",
                "WizardMagus2",
                new CareerChoiceObject.PassiveEffect(
                    PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10)));

            M2_4.Initialize(CareerID, "{=rf_wiz_t2_m2_4}Gain a magic item",
                "WizardMagus2",
                new CareerChoiceObject.ActiveEffect(() => AbilityEffects.GiveItemById(placeholderItemId)));

            M2_5.Initialize(CareerID, "{=rf_wiz_t2_m2_5}Immune to ranged damage for 30s (player)",
                "WizardMagus2",
                new CareerChoiceObject.PassiveEffect(30f, PassiveEffectType.Special)); // handled in code

            // ───────────────────────────────────────────────────────────
            // TIER 3 — ARCHMAGE
            // Lane 1
            R1_1.Initialize(CareerID, "{=rf_wiz_t3_r1_1}On kill restore 5 HP (player)",
                "WizardArchmage1",
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.OnKill, () =>
                {
                    if (Agent.Main != null)
                        Agent.Main.Health = MathF.Min(Agent.Main.HealthLimit, Agent.Main.Health + 5);
                }));

            R1_2.Initialize(CareerID, "{=rf_wiz_t3_r1_2}+15% post‑battle healing (player)",
                "WizardArchmage1",
                new CareerChoiceObject.PassiveEffect(0.15f, PassiveEffectType.Special)); // handled in campaign behavior

            R1_3.Initialize(CareerID, "{=rf_wiz_t3_r1_3}Increase spell area effect by 10%",
                "WizardArchmage1",
                new CareerChoiceObject.PassiveEffect(0.10f, PassiveEffectType.Special)); // handled in code

            R1_4.Initialize(CareerID, "{=rf_wiz_t3_r1_4}Gain a new magic item",
                "WizardArchmage1",
                new CareerChoiceObject.ActiveEffect(() => AbilityEffects.GiveItemById(placeholderItemId)));

            R1_5.Initialize(CareerID, "{=rf_wiz_t3_r1_5}Immune to melee damage for 30s (player)",
                "WizardArchmage1",
                new CareerChoiceObject.PassiveEffect(30f, PassiveEffectType.Special)); // handled in code

            // Lane 2
            R2_1.Initialize(CareerID, "{=rf_wiz_t3_r2_1}Companions gain 5 HP on kill",
            "WizardArchmage2",
            new CareerChoiceObject.PassiveEffect(5f, PassiveEffectType.Special));

            R2_2.Initialize(CareerID, "{=rf_wiz_t3_r2_2}+15% post‑battle healing (troops)",
                "WizardArchmage2",
                new CareerChoiceObject.PassiveEffect(0.15f, PassiveEffectType.Special)); // handled in campaign behavior

            R2_3.Initialize(CareerID, "{=rf_wiz_t3_r2_3}Increase spell damage by 10%",
                "WizardArchmage2",
                new CareerChoiceObject.PassiveEffect(
                    PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10)));

            R2_4.Initialize(CareerID, "{=rf_wiz_t3_r2_4}Gain a new magic item",
                "WizardArchmage2",
                new CareerChoiceObject.ActiveEffect(() => AbilityEffects.GiveItemById(placeholderItemId)));

            R2_5.Initialize(CareerID, "{=rf_wiz_t3_r2_5}Immune to ranged damage for 30s (player)",
                "WizardArchmage2",
                new CareerChoiceObject.PassiveEffect(30f, PassiveEffectType.Special)); // handled in code
        }
    }
}
