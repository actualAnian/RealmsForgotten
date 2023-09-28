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

        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            float baseNumber = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);


            if (weapon.Item != null)
            {
                CalculateRaceDamages(attackInformation, weapon, ref baseNumber);
                CalculateEffectsDamage(attackInformation, ref baseNumber);

                CharacterObject attackerCharacterObject = attackInformation.AttackerAgentCharacter as CharacterObject;
                //If weapon is a spell, do additional damage based on the alchemy skill of the attacker
                if (weapon.Item.StringId.Contains("anorit_fire"))
                {
                    if (attackerCharacterObject != null)
                    {
                        float factor = attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.NovicesLuck) ? RFPerks.Alchemy.NovicesLuck.PrimaryBonus :
                            (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.ApprenticesLuck) ? RFPerks.Alchemy.ApprenticesLuck.PrimaryBonus :
                                (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.AdeptsLuck) ? RFPerks.Alchemy.AdeptsLuck.PrimaryBonus :
                                    (attackerCharacterObject.GetPerkValue(RFPerks.Alchemy.MastersLuck) ? RFPerks.Alchemy.MastersLuck.PrimaryBonus : 0)));

                        if (factor > 0 &&
                            weapon.CurrentUsageItem.WeaponClass == WeaponClass.Stone)
                            baseNumber *= factor;
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

        private float CalculateEffectsDamage(in AttackInformation attackInformation, ref float baseDamage)
        {
            //If in berserker mode disables damage
            if (attackInformation.VictimAgent == Agent.Main && PotionsMissionBehavior.berserkerMode)
                return 0;

            if (ModifiedDamageAgents.TryGetValue(attackInformation.AttackerAgent.Index, out float factor))
            {
                return baseDamage + (baseDamage * factor);
            }

            return baseDamage;

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


    

