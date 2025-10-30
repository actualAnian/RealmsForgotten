using System.Collections.Generic;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.CustomSkills;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using FaceGen = TaleWorlds.Core.FaceGen;
using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using System;

namespace RealmsForgotten.Models
{
    public class RFAgentApplyDamageModel : AgentApplyDamageModel
    {
        public static RFAgentApplyDamageModel Instance;

        private AgentApplyDamageModel _baseModel;

        public RFAgentApplyDamageModel(AgentApplyDamageModel previousModel)
        {
            _baseModel = previousModel;
            Instance = this;
        }
        public Dictionary<int, float> ModifiedDamageAgents = new();
        static int shaitan = FaceGen.GetRaceOrDefault("shaitan");
        static int kharach = FaceGen.GetRaceOrDefault("kharach");
        static int brute = FaceGen.GetRaceOrDefault("brute");
        static int balrog = FaceGen.GetRaceOrDefault("balrog");
        static int thog = FaceGen.GetRaceOrDefault("thog");
        static int half_giant = FaceGen.GetRaceOrDefault("half_giant");
        static int zombie = FaceGen.GetRaceOrDefault("zombie");
        static int bark = FaceGen.GetRaceOrDefault("bark");
        static int nurh = FaceGen.GetRaceOrDefault("nurh");
        static int daimo = FaceGen.GetRaceOrDefault("daimo");
        static int sillok = FaceGen.GetRaceOrDefault("sillok");

        static List<int> standardRaces = new() { thog, shaitan, kharach, brute };
        static List<int> specialRaces = new List<int> { bark, nurh, daimo, sillok };
        public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = _baseModel.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;

            return baseValue;
        }

        public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = _baseModel.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;

            return baseValue;
        }

        public override float ApplyGeneralDamageModifiers(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
        {
            float baseNumber = _baseModel.ApplyGeneralDamageModifiers(attackInformation, collisionData, baseDamage);
            MissionWeapon weapon = attackInformation.AttackerWeapon;
            CharacterObject captainCharacterObject =
                attackInformation.AttackerAgent?.Formation?.Captain?.Character as CharacterObject;

            if (weapon.Item != null)
            {
                CalculateEffectsDamage(attackInformation, ref baseNumber);

                CharacterObject attackerCharacterObject = attackInformation.AttackerAgentCharacter as CharacterObject;
                CharacterObject attackedCharacterObject = attackInformation.VictimAgent?.Character as CharacterObject;

                // If weapon is a spell, do additional damage based on the alchemy skill of the attacker
                if (weapon.Item.StringId.Contains("anorit_fire"))
                {
                    if (attackerCharacterObject != null && attackedCharacterObject != null)
                    {
                        float factor = attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.NovicesLuck) ? RFPerks.Alchemy.NovicesLuck.PrimaryBonus :
                            (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.ApprenticesLuck) ? RFPerks.Alchemy.ApprenticesLuck.PrimaryBonus :
                                (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.AdeptsLuck) ? RFPerks.Alchemy.AdeptsLuck.PrimaryBonus :
                                    (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.MastersLuck) ? RFPerks.Alchemy.MastersLuck.PrimaryBonus : 0)));

                        if (factor > 0 &&
                            weapon.CurrentUsageItem.WeaponClass == WeaponClass.Stone)
                            baseNumber *= factor;

                        ExplainedNumber xpAmount = Campaign.Current.Models.CombatXpModel.GetXpFromHit(attackerCharacterObject, captainCharacterObject, attackedCharacterObject, attackerCharacterObject.HeroObject?.PartyBelongedTo?.Party, (int)baseDamage, baseDamage >= attackInformation.VictimAgentHealth,
                            CombatXpModel.MissionTypeEnum.Battle);

                        attackerCharacterObject.HeroObject?.AddSkillXp(RFSkills.Alchemy, xpAmount.ResultNumber);
                    }
                }
                else if (weapon.CurrentUsageItem.WeaponClass == WeaponClass.Cartridge)
                {
                    float DamageFactor = attackerCharacterObject.GetPerkValue(RFPerks.Arcane.NeophytesTalisman) ? RFPerks.Arcane.NeophytesTalisman.PrimaryBonus :
                        (attackerCharacterObject.GetPerkValue(RFPerks.Arcane.InitiatesTalisman) ? RFPerks.Arcane.InitiatesTalisman.PrimaryBonus :
                            (attackerCharacterObject.GetPerkValue(RFPerks.Arcane.HierophantsTalisman) ? RFPerks.Arcane.HierophantsTalisman.PrimaryBonus : 0));

                    if (DamageFactor > 0)
                        baseNumber *= DamageFactor;
                }

                CrusaderDamageModel.CalculateDamage(attackedCharacterObject, attackedCharacterObject, ref baseNumber);

                if (attackerCharacterObject == Hero.MainHero.CharacterObject)
                    CareerLogic.ApplyExtraShieldDamage(weapon);
            }
            DemonRaceDamageModel.CalculateDamage(attackInformation.AttackerAgent, weapon, ref baseNumber);


            if ((attackInformation.IsAttackerAgentMount ? attackInformation.AttackerRiderAgentCharacter : attackInformation.AttackerAgentCharacter) is CharacterObject attacker && collisionData.IsHorseCharge && attacker.IsMounted && attacker.IsPlayerCharacter && PlayerCareerExtension.HasAnyCareer())
            {
                var resultDamage = new ExplainedNumber(baseNumber);
                CareerHelper.ApplyBasicCareerPassives(ref resultDamage, PassiveEffectType.HorseChargeDamage);
                baseNumber = resultDamage.ResultNumber;
            }
            return baseNumber;
        }

        private void CalculateEffectsDamage(in AttackInformation attackInformation, ref float baseDamage)
        {
            // If in berserker mode disables damage
            if (attackInformation.VictimAgent == Agent.Main && PotionsMissionBehavior.berserkerMode)
                baseDamage = 0;

            if (ModifiedDamageAgents.TryGetValue(attackInformation.AttackerAgent.Index, out float factor))
            {
                baseDamage += baseDamage * factor;
            }

        }

        private float CalculateRaceDamagesAmplifiers(AttackInformation attackInformation, float baseNumber)
        {
            if (attackInformation.VictimAgent == null) return 0;
            MissionWeapon weapon = attackInformation.AttackerWeapon;
            BasicCharacterObject attackerCharacter = attackInformation.VictimAgent.Character;
            BasicCharacterObject victimCharacter = attackInformation.VictimAgent.Character;
            if (attackerCharacter == null || victimCharacter == null || weapon.Item == null) return baseNumber;
            if (weapon.Item.ItemType == ItemObject.ItemTypeEnum.Polearm ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon)
            {
                // If attacker is a half-giant OR a balrog, increase damage by 45%
                if (attackerCharacter.IsGiant() || attackerCharacter.IsBalrog())
                {
                    baseNumber += ((45f / 100f) * baseNumber);
                }

                // If attacker is one of the standard races, increase damage by 30%
                if (standardRaces.Contains(attackInformation.AttackerAgent.Character.Race))
                {
                    baseNumber += ((30f / 100f) * baseNumber);
                }
                // If attacker is one of the special races, increase damage by 80%
                if (specialRaces.Contains(attackInformation.AttackerAgent.Character.Race))
                {
                    baseNumber += ((80f / 100f) * baseNumber);
                }
                if (attackerCharacter.IsZombie() || attackerCharacter.IsOrcbase())
                {
                    baseNumber += ((20f / 100f) * baseNumber);
                }
            }
            return baseNumber;
        }
        public float CalculateRaceDamageReduction(in AttackInformation attackInformation, float baseNumber)
        {
            MissionWeapon weapon = attackInformation.AttackerWeapon;
            BasicCharacterObject attackerCharacter = attackInformation.VictimAgent.Character;
            BasicCharacterObject victimCharacter = attackInformation.VictimAgent.Character;
            if (attackerCharacter == null || victimCharacter == null) return baseNumber;
            if (
                weapon.Item != null && (
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.Polearm ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon))
            {
                // If victim is a half-giant, reduce damage by 70%
                if (attackInformation.VictimAgent?.Character != null && victimCharacter.IsGiant() || victimCharacter.IsBalrog())
                {
                    baseNumber -= ((70f / 100f) * baseNumber);
                }
                if (victimCharacter.IsZombie() || victimCharacter.IsOrcbase())
                {
                    baseNumber -= ((50f / 100f) * baseNumber);
                }
                // If victim is one of the standard races, reduce damage by 30%
                if (standardRaces.Contains(victimCharacter.Race))
                {
                    baseNumber -= ((30f / 100f) * baseNumber);
                }

                // If victim is one of the special races
                if (specialRaces.Contains(victimCharacter.Race))
                {
                    // If the weapon is not rfmisc_mistic_polearm, set damage to 0 (invulnerable)
                    if (weapon.Item.StringId != "rfmisc_mistic_polearm")
                    {
                        baseNumber = 0;
                    }
                    else
                    {
                        // Reduce damage by 90% if it's the special weapon
                        baseNumber -= ((90f / 100f) * baseNumber);
                    }
                }
            }
            return baseNumber;
        }

        public override bool IsDamageIgnored(in AttackInformation attackInformation, in AttackCollisionData collisionData) => _baseModel.IsDamageIgnored(in attackInformation, in collisionData);
        public override float ApplyDamageAmplifications(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage) 
        {
            float finalDamage = _baseModel.ApplyDamageAmplifications(in attackInformation, in collisionData, baseDamage);
            finalDamage = CalculateRaceDamagesAmplifiers(attackInformation, finalDamage);
            return finalDamage;
        }
        public override float ApplyDamageScaling(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage) => _baseModel.ApplyDamageScaling(in attackInformation, in collisionData, baseDamage);
        public override float ApplyDamageReductions(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
        {
            MissionWeapon weapon = attackInformation.AttackerWeapon;
            // PREVENIR DANO ENTRE MAGOS E ALIADOS, EXCETO PROJÉTIL DE CURA
            if (attackInformation.AttackerAgent != null && attackInformation.VictimAgent != null)
            {
                Agent attackerAgent = attackInformation.AttackerAgent;
                Agent victimAgent = attackInformation.VictimAgent;

                bool isSameTeam = attackerAgent.Team != null && victimAgent.Team != null && attackerAgent.Team == victimAgent.Team;
                bool isMage = attackerAgent.Character?.StringId?.Contains("mage") ?? false;

                if (isMage && isSameTeam)
                {
                    string weaponId = weapon.Item?.StringId ?? "";

                    if (weaponId == "rfmisc_spell_healing_force" ||
                    weaponId == "rfmisc_spell_healing_field" ||
                    weaponId == "rfmisc_spell_healing_force_area_big")// Substitua pelo ID real do seu projétil de cura
                    {
                        victimAgent.Health = Math.Min(victimAgent.Health + 15f, victimAgent.HealthLimit);
                        return 0f;
                    }

                    return 0f;
                }

            }
            float finalDamage = CalculateRaceDamageReduction(in attackInformation, baseDamage);
            _baseModel.ApplyDamageReductions(in attackInformation, in collisionData, finalDamage);
            return finalDamage;
        }
        public override void DecideMissileWeaponFlags(Agent attackerAgent, in MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags) => _baseModel.DecideMissileWeaponFlags(attackerAgent, in missileWeapon, ref missileWeaponFlags);
        public override void CalculateDefendedBlowStunMultipliers(Agent attackerAgent, Agent defenderAgent, CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon, WeaponComponentData defenderWeapon, ref float attackerStunPeriod, ref float defenderStunPeriod)
            => _baseModel.CalculateDefendedBlowStunMultipliers(attackerAgent, defenderAgent, collisionResult, attackerWeapon, defenderWeapon, ref attackerStunPeriod, ref defenderStunPeriod);
        public override float CalculateStaggerThresholdDamage(Agent defenderAgent, in Blow blow) => _baseModel.CalculateStaggerThresholdDamage(defenderAgent, in blow);
        public override float CalculateAlternativeAttackDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, WeaponComponentData weapon)
            => _baseModel.CalculateAlternativeAttackDamage(in attackInformation, in collisionData, weapon);
        public override float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter, in AttackCollisionData collisionData, float baseDamage) => _baseModel.CalculatePassiveAttackDamage(attackerCharacter, in collisionData, baseDamage);
        public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit) => _baseModel.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);
        public override void DecideWeaponCollisionReaction(in Blow registeredBlow, in AttackCollisionData collisionData, Agent attacker, Agent defender, in MissionWeapon attackerWeapon, bool isFatalHit, bool isShruggedOff, float momentumRemaining, out MeleeCollisionReaction colReaction)
            => _baseModel.DecideWeaponCollisionReaction(in registeredBlow, in collisionData, attacker, defender, attackerWeapon, isFatalHit, isShruggedOff, momentumRemaining, out colReaction);
        public override float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage) => _baseModel.CalculateShieldDamage(in attackInformation, baseDamage);
        public override float CalculateSailFireDamage(Agent attackerAgent, float baseDamage, bool damageFromShipMachine) => _baseModel.CalculateSailFireDamage(attackerAgent, baseDamage, damageFromShipMachine);
        public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman, bool isMissile) => _baseModel.GetDamageMultiplierForBodyPart(bodyPart, type, isHuman, isMissile);
        public override bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon) => _baseModel.CanWeaponIgnoreFriendlyFireChecks(weapon);
        public override bool CanWeaponDealSneakAttack(in AttackInformation attackInformation, WeaponComponentData weapon) => _baseModel.CanWeaponDealSneakAttack(in attackInformation, weapon);
        public override bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData) => _baseModel.CanWeaponDismount(attackerAgent, attackerWeapon, in blow, in collisionData);
        public override bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData) => _baseModel.CanWeaponKnockback(attackerAgent, attackerWeapon, in blow, in collisionData);
        public override bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData) => _baseModel.CanWeaponKnockDown(attackerAgent, victimAgent, attackerWeapon, in blow, in collisionData);
        public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsageHit) 
            => _baseModel.DecideCrushedThrough(attackerAgent, defenderAgent, totalAttackEnergy, attackDirection, strikeType,defendItem, isPassiveUsageHit);
        public override float CalculateRemainingMomentum(float originalMomentum, in Blow b, in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon, bool isCrushThrough)
            => _baseModel.CalculateRemainingMomentum(originalMomentum, in b, in collisionData, attacker, victim, attackerWeapon, isCrushThrough);
        public override bool DecideAgentShrugOffBlow(Agent victimAgent, in AttackCollisionData collisionData, in Blow blow) => _baseModel.DecideAgentShrugOffBlow(victimAgent, in collisionData, in blow);
        public override bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
            => _baseModel.DecideAgentDismountedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
        public override bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
            => _baseModel.DecideMountRearedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
        public override bool ShouldMissilePassThroughAfterShieldBreak(Agent attackerAgent, WeaponComponentData attackerWeapon) => _baseModel.ShouldMissilePassThroughAfterShieldBreak(attackerAgent, attackerWeapon);
        public override float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData) => _baseModel.GetDismountPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        public override float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData) => _baseModel.GetKnockBackPenetration(attackerAgent, attackerWeapon, in blow, collisionData);
        public override float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData) => _baseModel.GetKnockDownPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
        public override float GetHorseChargePenetration() => _baseModel.GetHorseChargePenetration();

        public override float CalculateHullFireDamage(float baseFireDamage)
        {
            return _baseModel.CalculateHullFireDamage(baseFireDamage);
        }
    }
}