using System;
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
using RealmsForgotten.Patches;
using TaleWorlds.CampaignSystem;
using static HarmonyLib.Code;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using static TaleWorlds.MountAndBlade.Mission;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace RealmsForgotten.Models
{
    public class RFAgentApplyDamageModel : SandboxAgentApplyDamageModel
    {
        public static RFAgentApplyDamageModel Instance;
        public RFAgentApplyDamageModel()
        {
            Instance = this;
        }
        public Dictionary<int, float> ModifiedDamageAgents = new();

        public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = base.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;

            return baseValue;
        }

        public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            bool baseValue = base.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            int half_giant = FaceGen.GetRaceOrDefault("half_giant");

            if (victimAgent.Character?.Race == half_giant && attackerAgent.Character?.Race != half_giant)
                return false;

            return baseValue;
        }

        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            float baseNumber = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);
            CharacterObject captainCharacterObject =
                attackInformation.AttackerAgent?.Formation?.Captain?.Character as CharacterObject;

            if (weapon.Item != null)
            {
                CalculateRaceDamages(attackInformation, weapon, ref baseNumber);
                CalculateEffectsDamage(attackInformation, ref baseNumber);

                CharacterObject attackerCharacterObject = attackInformation.AttackerAgentCharacter as CharacterObject;
                CharacterObject attackedCharacterObject = attackInformation.VictimAgent?.Character as CharacterObject;

                //If weapon is a spell, do additional damage based on the alchemy skill of the attacker
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
            }

            return baseNumber;
        }

        private void CalculateEffectsDamage(in AttackInformation attackInformation, ref float baseDamage)
        {
            //If in berserker mode disables damage
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
                    LogMessage($"CalculateRaceDamages: Victim is a half-giant, reduced damage to {baseNumber}");
                }

                // If attacker is a half-giant, increase damage by 45%
                if (attackInformation.AttackerAgent?.Character != null && attackInformation.AttackerAgent.Character.Race == half_giant)
                {
                    baseNumber += ((45f / 100f) * baseNumber);
                    LogMessage($"CalculateRaceDamages: Attacker is a half-giant, increased damage to {baseNumber}");
                }

                // If victim is one of the standard races, reduce damage by 30%
                if (attackInformation.VictimAgent?.Character != null && standardRaces.Contains(attackInformation.VictimAgent.Character.Race))
                {
                    baseNumber -= ((30f / 100f) * baseNumber);
                    LogMessage($"CalculateRaceDamages: Victim is a standard race (thog, shaitan, kharach, brute), reduced damage to {baseNumber}");
                }

                // If attacker is one of the standard races, increase damage by 30%
                if (attackInformation.AttackerAgent?.Character != null && standardRaces.Contains(attackInformation.AttackerAgent.Character.Race))
                {
                    baseNumber += ((30f / 100f) * baseNumber);
                    LogMessage($"CalculateRaceDamages: Attacker is a standard race (thog, shaitan, kharach, brute), increased damage to {baseNumber}");
                }

                // If victim is one of the special races
                if (attackInformation.VictimAgent?.Character != null && specialRaces.Contains(attackInformation.VictimAgent.Character.Race))
                {
                    // If the weapon is not rfmisc_mistic_polearm, set damage to 0 (invulnerable)
                    if (weapon.Item.StringId != "rfmisc_mistic_polearm")
                    {
                        baseNumber = 0;
                        LogMessage($"CalculateRaceDamages: Victim is a special race (bark, nurh, daimo, sillok) and weapon is not rfmisc_mistic_polearm, set damage to 0");
                    }
                    else
                    {
                        // Reduce damage by 90% if it's the special weapon
                        baseNumber -= ((90f / 100f) * baseNumber);
                        LogMessage($"CalculateRaceDamages: Victim is a special race (bark, nurh, daimo, sillok) and weapon is rfmisc_mistic_polearm, reduced damage by 90% to {baseNumber}");
                    }
                }

                // If attacker is one of the special races, increase damage by 80%
                if (attackInformation.AttackerAgent?.Character != null && specialRaces.Contains(attackInformation.AttackerAgent.Character.Race))
                {
                    baseNumber += ((80f / 100f) * baseNumber);
                    LogMessage($"CalculateRaceDamages: Attacker is a special race (bark, nurh, daimo, sillok), increased damage by 80% to {baseNumber}");
                }
            }
        }

        private void LogMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message));
        }
    }

}






