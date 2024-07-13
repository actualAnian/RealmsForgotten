using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    public class SpecialDamageMissionLogic : MissionLogic
    {
        private SpecialDamageCalculator _damageCalculator = new SpecialDamageCalculator();

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            // Debugging information
            InformationManager.DisplayMessage(new InformationMessage("OnAgentHit triggered"));

            float damage = blow.InflictedDamage;
            string damageType = blow.DamageType.ToString();
            string weaponId = affectorWeapon.Item?.StringId ?? string.Empty;

            InformationManager.DisplayMessage(new InformationMessage($"Initial Damage: {damage}, DamageType: {damageType}, WeaponID: {weaponId}"));

            _damageCalculator.ApplyDamage(affectedAgent, ref damage, damageType, weaponId);

            // Log after applying special damage calculations
            InformationManager.DisplayMessage(new InformationMessage($"Adjusted Damage: {damage}"));

            // Since attackCollisionData is read-only, we can't directly modify it here.
            // Instead, we'll log the final damage to confirm it's being calculated correctly.
            InformationManager.DisplayMessage(new InformationMessage($"Final Inflicted Damage: {damage}"));

            // We need to find another way to apply the modified damage since attackCollisionData is read-only.
            affectedAgent.Health = Math.Max(0, affectedAgent.Health - blow.InflictedDamage + damage); // Adjust health based on the modified damage
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }
}