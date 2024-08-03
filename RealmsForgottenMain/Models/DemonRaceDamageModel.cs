using SandBox.GameComponents;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace Bannerlord.Module1.Models
{
    internal class DemonRaceDamageModel : SandboxAgentApplyDamageModel
    {
        public static DemonRaceDamageModel Instance;
        public DemonRaceDamageModel()
        {
            Instance = this;
        }

        private readonly HashSet<string> customRaceIds = new HashSet<string> { "tlachiquiy", "shaitan", "thog", "kharach", "brute", "bark", "sillok", "nurh", "daimo" };

        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            float calculatedDamage = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);

            Agent attacker = attackInformation.AttackerAgent;
            Agent defender = attackInformation.VictimAgent;

            bool isUnarmed = weapon.IsEmpty || weapon.CurrentUsageItem == null;

            if (attacker != null && attacker.Character != null && customRaceIds.Contains(attacker.Character.Race.ToString()) && isUnarmed)
            {
                calculatedDamage *= 10.0f;
            }

            return calculatedDamage;
        }
    }
}