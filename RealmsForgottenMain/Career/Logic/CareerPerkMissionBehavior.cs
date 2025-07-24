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
    public class CareerPerkMissionBehavior : TaleWorlds.MountAndBlade.MissionLogic
    {
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            List<string> choices = PlayerCareerExtension.GetAllCareerChoices();
            if (affectorAgent != null && affectorAgent.IsMainAgent)
            {
                foreach (var choiceID in choices)
                {
                    CareerChoiceObject choice = RFCareerChoices.GetChoice(choiceID);
                    if (choice?.Passive == null || choice.Passive.PassiveEffectType != PassiveEffectType.OnKill) continue;
                    choice.Passive.Activate();
                }
            }
        }
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            CareerObject? career = PlayerCareerExtension.GetCareer();
            if (career == null) return;
            //if (!blow.IsMissile || !affectorAgent.IsMainAgent) return;  
            //MissionEquipment equipment = Agent.Main.Equipment;
            //for (int i = 0; i < 5; i++)
            //{
            //    EquipmentIndex equipmentIndex = (EquipmentIndex)i;
            //    MissionWeapon missionWeapon = equipment[equipmentIndex];
            //    if (missionWeapon.IsEmpty || missionWeapon.Item.StringId != affectorWeapon.Item.StringId) continue;
            //    //WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
            //    short value = (short)(missionWeapon.Amount + 1);
            //    affectorAgent.SetWeaponAmountInSlot(equipmentIndex, value, false);
            //    //equipment.SetAmountOfSlot(equipmentIndex, value, true);
            //    //affectorAgent.TryToWieldWeaponInSlot(slotIndex, Agent.WeaponWieldActionType.InstantAfterPickUp, false);
            //}
            Ability.ClassAbility ability = career.Ability;
            if (ability.IsActiveInMission)
                ability.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData);
        }
        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            //List<string> choices = PlayerCareerExtension.GetAllCareerChoices();
            //if (attacker.IsMainAgent 
            //    && choices.Contains("SurvivalistKeystone") 
            //    && collisionData.VictimHitBodyPart == BoneBodyPartType.Head || collisionData.VictimHitBodyPart == BoneBodyPartType.Neck)
            //{
            //    attacker.getatt
            //}
        }
    }
}
