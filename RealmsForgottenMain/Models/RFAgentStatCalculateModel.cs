using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using RealmsForgotten.CustomSkills;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Models
{
    internal class RFAgentStatCalculateModel : SandboxAgentStatCalculateModel
    {
        public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
        {
            base.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }

        public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            base.UpdateAgentStats(agent, agentDrivenProperties);
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }

        private void UpdateAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (agent.IsHuman)
            {
                AddSkillEffectsForAgent(agent, agentDrivenProperties);
                //AddPerkEffectsForAgent(agent, agentDrivenProperties);
            }
        }
        private void AddSkillEffectsForAgent(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            WeaponComponentData weapon = (wieldedItemIndex != EquipmentIndex.None) ? agent.Equipment[wieldedItemIndex].CurrentUsageItem : null;
            var character = agent.Character as CharacterObject;
            var captain = agent.Team.Leader;
            if (weapon != null && character != null && weapon.WeaponClass == WeaponClass.Cartridge)
            {
                int effectiveSkill = GetEffectiveSkill(agent, RFSkills.Alchemy);
                ExplainedNumber reloadSpeed = new ExplainedNumber(agentDrivenProperties.ReloadSpeed);

                SkillHelper.AddSkillBonusForCharacter(RFSkills.Arcane, RFSkillEffects.WandReloadSpeed, character, ref reloadSpeed, effectiveSkill);
                

                agentDrivenProperties.ReloadSpeed = reloadSpeed.ResultNumber;
            }
        }
        public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
        {
            float baseValue = base.GetWeaponInaccuracy(agent, weapon, weaponSkill);
            ExplainedNumber accuracy = new ExplainedNumber(baseValue, false, null);
            var character = agent.Character as CharacterObject;
            if (character != null)
            {
                if (weapon.IsRangedWeapon && weapon.RelevantSkill == RFSkills.Arcane && weapon.WeaponClass == WeaponClass.Musket)
                {
                    SkillHelper.AddSkillBonusForCharacter(RFSkills.Arcane, RFSkillEffects.WandAccuracy, character, ref accuracy, weaponSkill, false, 0);

                }
            }

            return accuracy.ResultNumber;

        }
    }
    /*
     *     internal class RFAgentStatCalculateModel : SandboxAgentStatCalculateModel
    {

        public override int GetEffectiveSkillForWeapon(Agent agent, WeaponComponentData weapon)
        {
            int baseValue = base,GetEffectiveSkillForWeapon(agent, weapon);
            if (weapon.WeaponClass == WeaponClass.Musket)
                return agent.Character.GetSkillValue(RFSkills.Arcane);
            return baseValue;
        }
    }
     */
}
