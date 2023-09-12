﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RealmsForgotten.CustomSkills;
using SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using FaceGen = TaleWorlds.Core.FaceGen;
using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem;
using static HarmonyLib.Code;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.Models
{

    internal class RFAgentApplyDamageModel : SandboxAgentApplyDamageModel
    {
       
        public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent,
            in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = base.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;



            return baseValue;
        }

        public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent,
            in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = base.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;

            return baseValue;
        }

        public override float CalculateDamage(in AttackInformation attackInformation,
            in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            float baseNumber = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);

            //If in berserker mode disables damage
            if (attackInformation.VictimAgent == Agent.Main && HealingPotionMissionBehavior.berserkerMode)
                return 0;
            if (weapon.Item != null)
            {
                CalculateRaceDamages(attackInformation, weapon, ref baseNumber);

                //If weapon is a spell, do additional damage based on the alchemy skill of the attacker
                CharacterObject attackerCharacterObject = attackInformation.AttackerAgentCharacter as CharacterObject;
                if (attackerCharacterObject != null)
                {
                    float factor = attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.NovicesLuck) ? RFPerks.Alchemy.NovicesLuck.PrimaryBonus :
                        (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.ApprenticesLuck) ? RFPerks.Alchemy.ApprenticesLuck.PrimaryBonus :
                            (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.AdeptsLuck) ? RFPerks.Alchemy.AdeptsLuck.PrimaryBonus : 
                                (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.MastersLuck) ? RFPerks.Alchemy.MastersLuck.PrimaryBonus : 0)));

                    if (factor > 0 &&      weapon.Item.StringId.Contains("anorit_fire") &&
                        weapon.CurrentUsageItem.WeaponClass == WeaponClass.Stone)
                        baseNumber *= factor;
                }
            }

            return baseNumber;
        }

        private void CalculateRaceDamages(AttackInformation attackInformation, MissionWeapon weapon, ref float baseNumber)
        {
            int half_giant = FaceGen.GetRaceOrDefault("half_giant");
            int mull = FaceGen.GetRaceOrDefault("mull");

            if (weapon.Item.Type == ItemObject.ItemTypeEnum.Polearm ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon)
            {
                //If victim is a giant, 30% less damage
                if (attackInformation.VictimAgent?.Character?.Race == half_giant)
                    baseNumber -= ((30f / 100f) * baseNumber);

                //If attacker is a giant, 30% more damage
                if (attackInformation.AttackerAgent?.Character?.Race == half_giant)
                    baseNumber += ((30f / 100f) * baseNumber);

                //If victim is a mull, 15% less damage
                if (attackInformation.VictimAgent?.Character?.Race == mull)
                    baseNumber -= ((15f / 100f) * baseNumber);
            }
        }
    }

}


/*
public abstract float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage);

public abstract void DecideMissileWeaponFlags(Agent attackerAgent, MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags);

public abstract void CalculateCollisionStunMultipliers(Agent attackerAgent, Agent defenderAgent, bool isAlternativeAttack, CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon, WeaponComponentData defenderWeapon, out float attackerStunMultiplier, out float defenderStunMultiplier);

public abstract float CalculateStaggerThresholdMultiplier(Agent defenderAgent);

public abstract float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter, in AttackCollisionData collisionData, float baseDamage);

public abstract MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit);

public abstract float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage);

public abstract float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman);

public abstract bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon);

public abstract bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

public abstract bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

public abstract bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

public abstract bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsageHit);

public abstract bool DecideAgentShrugOffBlow(Agent victimAgent, AttackCollisionData collisionData, in Blow blow);

public abstract bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

public abstract bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

public abstract bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

public abstract bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

public abstract float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

public abstract float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

public abstract float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

public abstract float GetHorseChargePenetration();
 */
    

