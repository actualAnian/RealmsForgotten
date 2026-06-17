using System.Collections.Generic;
using Helpers;
using RealmsForgotten.Behaviors;
using RealmsForgotten.Career;
using RealmsForgotten.Career.Logic;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.ObjectExtensions;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static RealmsForgotten.Career.CareerChoiceObject;

namespace RealmsForgotten.Models
{
    internal class RFAgentStatCalculateModel : SandboxAgentStatCalculateModel
    {
        private AgentStatCalculateModel _previousModel;
        
        public RFAgentStatCalculateModel(AgentStatCalculateModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
        {
            _previousModel.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }

        public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            _previousModel.UpdateAgentStats(agent, agentDrivenProperties);
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }

        private void UpdateAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (agent.IsHuman)
            {
                if (RFEnchantedWeaponsMissionBehavior.Instance?.ModifiedAgents.TryGetValue(agent, out var properties) == true)
                {
                    foreach (var tuple in properties)
                    {
                        agent.SetAgentDrivenPropertyValueFromConsole(tuple.property, tuple.amount);
                        agent.UpdateCustomDrivenProperties();
                    }
                }
                AddSkillEffectsForAgent(agent, agentDrivenProperties);
                AddCareerAgentProperties(agent, agentDrivenProperties);
            }
        }

        private void AddCareerAgentProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (!agent.BelongsToMainParty()) return;
            PlayerClassInfo info = PlayerCareerExtension.PlayerCareerInfo;
            if (info == null) return;
            List<string> choices = info.CareerChoices;
            foreach (var choiceID in choices)
            {
                CareerChoiceObject choice = RFCareerChoices.GetChoice(choiceID);
                if(choice.Passive is AgentPropertiesPassiveEffect propertiesPassiveEffect)
                {
                    propertiesPassiveEffect.OnAgentCreated(agent, agentDrivenProperties);
                }
            }
        }

        private EquipmentIndex[] equipmentIndices = { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3};
        public override void InitializeMissionEquipment(Agent agent)
        {
            _previousModel.InitializeMissionEquipment(agent);

            if (agent?.IsMount == true || agent?.Equipment == null || agent?.Character is not CharacterObject agentCharacterObject)
                return;

            foreach (var equipmentIndex in equipmentIndices)
            {
                if (agent.Equipment[equipmentIndex].Item == null)
                    continue;
                if (agent.Equipment[equipmentIndex].Item.PrimaryWeapon.WeaponClass == WeaponClass.Cartridge)
                {
                    var number = new ExplainedNumber(agent.Equipment[equipmentIndex].Amount);
                    if (agent.Character?.StringId == "evil_witch")
                        number.Add(1000);
                    SkillHelper.AddSkillBonusForCharacter(RFSkillEffects.MagicStaffPower, agentCharacterObject, ref number);
                    agent.SetWeaponAmountInSlot(equipmentIndex, (short)number.ResultNumber, true);
                }
                else if (agent.Equipment[equipmentIndex].Item.StringId.Contains("anorit_fire"))
                {
                    ExplainedNumber number = new ExplainedNumber(agent.Equipment[equipmentIndex].Amount);
                    SkillHelper.AddSkillBonusForCharacter(RFSkillEffects.BombStackMultiplier, agentCharacterObject, ref number);

                    agent.SetWeaponAmountInSlot(equipmentIndex, (short)number.ResultNumber, true);
                }
            }
            if (agent == Agent.Main)
                CareerLogic.ApplyExtraAmmo();
        }
        private void AddSkillEffectsForAgent(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (agent.Character is CharacterObject character && agent.WieldedWeapon.Item?.Type == ItemObject.ItemTypeEnum.Musket)
            {
                int effectiveSkill = GetEffectiveSkill(agent, RFSkills.Arcane);
                ExplainedNumber reloadSpeed = new ExplainedNumber(agentDrivenProperties.ReloadSpeed);
                ExplainedNumber missileSpeed = new ExplainedNumber(agentDrivenProperties.MissileSpeedMultiplier);

                SkillHelper.AddSkillBonusForCharacter(RFSkillEffects.WandReloadSpeed, character, ref reloadSpeed);

                SkillHelper.AddSkillBonusForCharacter(RFSkillEffects.WandAccuracy, character, ref missileSpeed);


                agentDrivenProperties.ReloadSpeed = reloadSpeed.ResultNumber;
                agentDrivenProperties.MissileSpeedMultiplier = missileSpeed.ResultNumber;
            }

        }
        public override float GetEffectiveMaxHealth(Agent agent)
        {
            if (agent == null) return 0;
            ExplainedNumber explainedNumber = new ExplainedNumber(base.GetEffectiveMaxHealth(agent));
            if (agent.IsMount && agent.RiderAgent != null && agent.RiderAgent.IsHero && agent.RiderAgent == Agent.Main)
                CareerHelper.ApplyBasicCareerPassives(ref explainedNumber, PassiveEffectType.HorseHealth);
            return explainedNumber.ResultNumber;
        }
        public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
        {
            float baseValue = _previousModel.GetWeaponInaccuracy(agent, weapon, weaponSkill);
            ExplainedNumber accuracy = new ExplainedNumber(baseValue, false, null);
            var character = agent.Character as CharacterObject;
            if (character != null)
            {
                if (weapon.WeaponClass == WeaponClass.Musket)
                {
                    SkillHelper.AddSkillBonusForCharacter(RFSkillEffects.WandAccuracy, character, ref accuracy);
                }
            }
            return accuracy.ResultNumber;
        }
    }
}
