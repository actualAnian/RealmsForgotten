using System.Collections.Generic;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.CustomSkills;
using SandBox.GameComponents;
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
    public class RFAgentApplyDamageModel : SandboxAgentApplyDamageModel
    {
        public static RFAgentApplyDamageModel Instance;

        private AgentApplyDamageModel _previousModel;

        public RFAgentApplyDamageModel(AgentApplyDamageModel previousModel)
        {
            _previousModel = previousModel;
            Instance = this;
        }
        public Dictionary<int, float> ModifiedDamageAgents = new();

        public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = _previousModel.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;

            return baseValue;
        }

        public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = _previousModel.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;

            return baseValue;
        }

        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            float baseNumber = _previousModel.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);

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

            CharacterObject captainCharacterObject =
                attackInformation.AttackerAgent?.Formation?.Captain?.Character as CharacterObject;

            if (weapon.Item != null)
            {
                CalculateRaceDamages(attackInformation, weapon, ref baseNumber);
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

                        Campaign.Current.Models.CombatXpModel.GetXpFromHit(attackerCharacterObject, captainCharacterObject, attackedCharacterObject, attackerCharacterObject.HeroObject?.PartyBelongedTo?.Party, (int)baseDamage, baseDamage >= attackInformation.VictimAgentHealth,
                            CombatXpModel.MissionTypeEnum.Battle, out int xpAmount);

                        attackerCharacterObject.HeroObject?.AddSkillXp(RFSkills.Alchemy, xpAmount);
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

        private void CalculateRaceDamages(AttackInformation attackInformation, MissionWeapon weapon, ref float baseNumber)
        {
            // Get race IDs for absorption and increase
            int half_giant = FaceGen.GetRaceOrDefault("half_giant");
            int thog = FaceGen.GetRaceOrDefault("thog");
            int shaitan = FaceGen.GetRaceOrDefault("shaitan");
            int kharach = FaceGen.GetRaceOrDefault("kharach");
            int brute = FaceGen.GetRaceOrDefault("brute");
            int balrog = FaceGen.GetRaceOrDefault("balrog");
           

            // Get race IDs for special absorption and increase
            int bark = FaceGen.GetRaceOrDefault("bark");
            int nurh = FaceGen.GetRaceOrDefault("nurh");
            int daimo = FaceGen.GetRaceOrDefault("daimo");
            int sillok = FaceGen.GetRaceOrDefault("sillok");
            int evil_witch = FaceGen.GetRaceOrDefault("evil_witch");
            // Get race ID for zombies
            int zombie = FaceGen.GetRaceOrDefault("zombie");
            int orc_base = FaceGen.GetRaceOrDefault("orc_base");
            // List of races that partake in the same logic
            List<int> standardRaces = new List<int> { thog, shaitan, kharach, brute };
            List<int> specialRaces = new List<int> { bark, nurh, daimo, sillok };

            if (weapon.Item.ItemType == ItemObject.ItemTypeEnum.Polearm ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon)
            {
                // If victim is a half-giant, reduce damage by 70%
                if (attackInformation.VictimAgent?.Character != null && (attackInformation.VictimAgent.Character.Race == half_giant || attackInformation.VictimAgent.Character.Race == balrog))
                {
                    baseNumber -= ((70f / 100f) * baseNumber);
                }

                // If attacker is a half-giant OR a balrog, increase damage by 45%
                if (attackInformation.AttackerAgent?.Character != null &&
                    (attackInformation.AttackerAgent.Character.Race == half_giant || attackInformation.AttackerAgent.Character.Race == balrog))
                {
                    baseNumber += ((45f / 100f) * baseNumber);
                }

                // If attacker is a half-giant, increase damage by 45%
                if (attackInformation.AttackerAgent?.Character != null && (attackInformation.AttackerAgent.Character.Race == half_giant || attackInformation.AttackerAgent.Character.Race == balrog))
                {
                    baseNumber += ((45f / 100f) * baseNumber);
                }

                // If victim is one of the standard races, reduce damage by 30%
                if (attackInformation.VictimAgent?.Character != null && standardRaces.Contains(attackInformation.VictimAgent.Character.Race))
                {
                    baseNumber -= ((30f / 100f) * baseNumber);
                }

                // If attacker is one of the standard races, increase damage by 30%
                if (attackInformation.AttackerAgent?.Character != null && standardRaces.Contains(attackInformation.AttackerAgent.Character.Race))
                {
                    baseNumber += ((30f / 100f) * baseNumber);
                }

                // If victim is one of the special races
                if (attackInformation.VictimAgent?.Character != null && specialRaces.Contains(attackInformation.VictimAgent.Character.Race))
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

                // If attacker is one of the special races, increase damage by 80%
                if (attackInformation.AttackerAgent?.Character != null && specialRaces.Contains(attackInformation.AttackerAgent.Character.Race))
                {
                    baseNumber += ((80f / 100f) * baseNumber);
                }

                // If victim is a zombie, reduce damage by 50%
                if ((attackInformation.VictimAgent?.Character != null && (attackInformation.VictimAgent.Character.Race == zombie)) || (attackInformation.AttackerAgent?.Character != null && (attackInformation.AttackerAgent.Character.Race == orc_base))
                )
                {
                    baseNumber -= ((50f / 100f) * baseNumber);
                }

                // If attacker is a zombie, increase damage by 20%
                if (attackInformation.AttackerAgent?.Character != null && (attackInformation.AttackerAgent.Character.Race == zombie || attackInformation.AttackerAgent.Character.Race == orc_base))
                {
                    baseNumber += ((20f / 100f) * baseNumber);
                }
            }
        }
    }
}