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
            }
            DemonRaceDamageModel.CalculateDamage(attackInformation.AttackerAgent, weapon, ref baseNumber);

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

            // Get race IDs for special absorption and increase
            int bark = FaceGen.GetRaceOrDefault("bark");
            int nurh = FaceGen.GetRaceOrDefault("nurh");
            int daimo = FaceGen.GetRaceOrDefault("daimo");
            int sillok = FaceGen.GetRaceOrDefault("sillok");

            // Get race ID for zombies
            int zombie = FaceGen.GetRaceOrDefault("zombie");

            // List of races that partake in the same logic
            List<int> standardRaces = new List<int> { thog, shaitan, kharach, brute };
            List<int> specialRaces = new List<int> { bark, nurh, daimo, sillok };

            if (weapon.Item.ItemType == ItemObject.ItemTypeEnum.Polearm ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                weapon.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon)
            {
                // If victim is a half-giant, reduce damage by 70%
                if (attackInformation.VictimAgent?.Character != null && attackInformation.VictimAgent.Character.Race == half_giant)
                {
                    baseNumber -= ((70f / 100f) * baseNumber);
                }

                // If attacker is a half-giant, increase damage by 45%
                if (attackInformation.AttackerAgent?.Character != null && attackInformation.AttackerAgent.Character.Race == half_giant)
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
                if (attackInformation.VictimAgent?.Character != null && attackInformation.VictimAgent.Character.Race == zombie)
                {
                    baseNumber -= ((50f / 100f) * baseNumber);
                }

                // If attacker is a zombie, increase damage by 20%
                if (attackInformation.AttackerAgent?.Character != null && attackInformation.AttackerAgent.Character.Race == zombie)
                {
                    baseNumber += ((20f / 100f) * baseNumber);
                }
            }
        }
    }
}