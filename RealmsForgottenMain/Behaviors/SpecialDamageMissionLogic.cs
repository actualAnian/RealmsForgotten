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

            float damage = blow.InflictedDamage;
            string damageType = blow.DamageType.ToString();
            string weaponId = affectorWeapon.Item?.StringId ?? string.Empty;

            _damageCalculator.ApplyDamage(affectedAgent, ref damage, damageType, weaponId);

            affectedAgent.Health = Math.Max(0, affectedAgent.Health - blow.InflictedDamage + damage);
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }
}