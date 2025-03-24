using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Logic
{
    public class CareerPerkMissionBehavior : MissionLogic
    {
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            List<string> choices = PlayerCareerExtension.GetAllCareerChoices();

            // Check if affectorAgent is not null before using it
            if (affectorAgent != null && affectorAgent.IsMainAgent)
            {
                foreach (var choiceID in choices)
                {
                    CareerChoiceObject choice = RFCareerChoices.GetChoice(choiceID);
                    if (choice?.Passive == null || choice.Passive.PassiveEffectType != PassiveEffectType.OnKill)
                        continue;

                    choice.Passive.Activate();
                }
            }
        }

        public override void OnAgentHit(
            Agent affectedAgent,
            Agent affectorAgent,
            in MissionWeapon affectorWeapon,
            in Blow blow,
            in AttackCollisionData attackCollisionData
        )
        {
            // Only process if it's a missile hit AND the one who hit is the main agent
            if (!blow.IsMissile || !affectorAgent.IsMainAgent) return;

            MissionEquipment equipment = Agent.Main.Equipment;
            for (int i = 0; i < 5; i++)
            {
                EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                MissionWeapon missionWeapon = equipment[equipmentIndex];
                if (missionWeapon.IsEmpty || missionWeapon.Item.StringId != affectorWeapon.Item.StringId)
                    continue;

                short value = (short)(missionWeapon.Amount + 1);
                affectorAgent.SetWeaponAmountInSlot(equipmentIndex, value, false);
            }
        }

        public override void OnMissileHit(
            Agent attacker,
            Agent victim,
            bool isCanceled,
            AttackCollisionData collisionData
        )
        {
            // Code for OnMissileHit, if needed, can be added here.
            // Example: checking for a certain career choice effect when a missile hits the head or neck.
        }
    }
}