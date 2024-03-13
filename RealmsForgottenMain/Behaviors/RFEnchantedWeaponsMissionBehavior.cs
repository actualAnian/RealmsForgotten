﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.Utility;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.Patches;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace RealmsForgotten.Behaviors
{
    internal class RFEnchantedWeaponsMissionBehavior : MissionBehavior
    {
        public Dictionary<Agent, List<(DrivenProperty property, float amount)>> BoostedAgents = new();
        private static Dictionary<string, WeaponFlags> weaponFlags = new()
        {
            { "rfonehanded", WeaponFlags.MeleeWeapon}, { "rftwohanded", WeaponFlags.MeleeWeapon }, { "rfpolearm", WeaponFlags.MeleeWeapon },
            {"rfbow", WeaponFlags.RangedWeapon}, {"rfcrossbow", WeaponFlags.RangedWeapon}, { "rfthrowing", WeaponFlags.RangedWeapon }
        };
        private static string[] skillsKeys = weaponFlags.Keys.ToArray();
        public override void OnCreated()
        {
            HaveDemoralizingArmor = (false, 0);
            HaveMoralizingArmor = (false, 0);
        }

        private static (bool, int) HaveDemoralizingArmor = (false, 0);
        private static (bool, int) HaveMoralizingArmor = (false, 0);

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public static RFEnchantedWeaponsMissionBehavior Instance;

        public RFEnchantedWeaponsMissionBehavior() => Instance = this;

        public override void OnDeploymentFinished() 
        {
            weaponFlags = new()
            {
                { "rfonehanded", WeaponFlags.MeleeWeapon}, { "rftwohanded", WeaponFlags.MeleeWeapon }, { "rfpolearm", WeaponFlags.MeleeWeapon },
                {"rfbow", WeaponFlags.RangedWeapon}, {"rfcrossbow", WeaponFlags.RangedWeapon}, { "rfthrowing", WeaponFlags.RangedWeapon }
            };

            if (IsBattle())
            {
                bool isMainAgent = false;
                int amount = 0;
                if (Mission.PlayerTeam.GetHeroAgents().Any(x =>
                {
                    BasicCharacterObject character = x.Character;
                    if (character != null)
                    {
                        ItemObject armor = x.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.Body).Item;
                        if (armor != null && armor.StringId.Contains("rfdemoralizing"))
                        {
                            amount = RFUtility.GetNumberAfterSkillWord(
                                x.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.Body).Item.StringId,
                                "rfdemoralizing");
                            isMainAgent = x.IsMainAgent;
                            return true;
                        }

                    }

                    return false;
                })) ;
                {
                    if (amount > 0)
                        amount = -amount;
                    foreach (Agent agent in Mission.PlayerEnemyTeam.ActiveAgents)
                    {
                        if (agent.Character != null)
                        {
                            agent.ChangeMorale(amount);
                        }
                    }

                    if (isMainAgent && amount != 0)
                    {
                        TextObject txt = new TextObject("{=enchanted_item_text.1}Your armor intimidated the enemies and lowered their morale by {AMOUNT} points.");
                        txt.SetTextVariable("AMOUNT", amount);
                        InformationManager.DisplayMessage(new InformationMessage(txt.ToString(), Color.FromUint(16711680)));
                    }
                    HaveDemoralizingArmor = (true, amount);
                }

                int amount2 = 0;
                if (Mission.PlayerTeam.GetHeroAgents().Any(x =>
                {
                    BasicCharacterObject character = x.Character;
                    if (character != null)
                    {
                        ItemObject armor = x.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.Body).Item;
                        if (armor != null && armor.StringId.Contains("rfmoralizing"))
                        {
                            amount2 = RFUtility.GetNumberAfterSkillWord(
                                x.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.Body).Item.StringId,
                                "rfmoralizing");
                            isMainAgent = x.IsMainAgent;
                            return true;
                        }

                    }

                    return false;
                })) ;
                {
                    if (amount2 < 0)
                        amount2 = +amount;
                    foreach (Agent agent in Mission.PlayerTeam.ActiveAgents)
                    {
                        if (agent.Character != null)
                        {
                            agent.ChangeMorale(amount2);
                        }
                    }
                    if (isMainAgent && amount2 != 0)
                    {
                        TextObject txt = new TextObject("{=enchanted_item_text.2}Your armor instilled confidence in your army boosting their morale by {AMOUNT} points.");
                        txt.SetTextVariable("AMOUNT", amount2);
                        InformationManager.DisplayMessage(new InformationMessage(txt.ToString(), Color.FromUint(9424384)));
                    }
                    HaveMoralizingArmor = (true, amount2);
                }
            }

        }
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (affectorWeapon.Item != null && affectorWeapon.CurrentUsageItem?.WeaponClass == WeaponClass.Cartridge && IncreaseAreaOfDamagePatch.CurrentBlow.OwnerId != blow.OwnerId)
            {
                if (affectorAgent.Character is CharacterObject attackerCharacterObject)
                {
                    float areaFactor = attackerCharacterObject.GetPerkValue(RFPerks.Arcane.NeophytesStaff) ? RFPerks.Arcane.NeophytesStaff.PrimaryBonus :
                        (attackerCharacterObject.GetPerkValue(RFPerks.Arcane.InitiatesStaff) ? RFPerks.Arcane.InitiatesStaff.PrimaryBonus :
                            (attackerCharacterObject.GetPerkValue(RFPerks.Arcane.HierophantsStaff) ? RFPerks.Arcane.HierophantsStaff.PrimaryBonus : 0));
                    if (areaFactor > 0)
                    {
                        AttackCollisionData collisionData = attackCollisionData;
                        Blow blow1 = blow;
                        IncreaseAreaOfDamagePatch.isWand = areaFactor;


                        IncreaseAreaOfDamagePatch.Prefix(ref collisionData, ref blow1, affectedAgent, affectorAgent, false, Mission.Current);
                    }
                }
            }
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.Character == null || agent.IsMount)
                return;
            BasicCharacterObject basicCharacterObject = agent.Character;
            if (IsBattle() && agent.Team != null)
            {
                if (HaveDemoralizingArmor.Item1 && !agent.Team.IsPlayerTeam && !agent.Team.IsPlayerAlly)
                {
                    agent.ChangeMorale(HaveDemoralizingArmor.Item2);
                }
                if (HaveMoralizingArmor.Item1 && agent.Team.IsPlayerTeam)
                {
                    agent.ChangeMorale(HaveMoralizingArmor.Item2);
                }
            }
            
            for (EquipmentIndex equipmentIndex = EquipmentIndex.Weapon0; equipmentIndex <= EquipmentIndex.Weapon3; equipmentIndex++)
            {
                string skillString = skillsKeys.FirstOrDefault(x => basicCharacterObject.Equipment[equipmentIndex].Item?.StringId.Contains(x) == true);
                if (skillString != null && weaponFlags.TryGetValue(skillString, out WeaponFlags flag))
                {
                    if (!BoostedAgents.ContainsKey(agent))
                    {
                        BoostedAgents.Add(agent, new List<(DrivenProperty property, float amount)>());
                    }
                    int increaseAmount = RFUtility.GetNumberAfterSkillWord(basicCharacterObject.Equipment[equipmentIndex].Item?.StringId, skillString, false);
                    
                    BoostedAgents[agent].Add((DrivenProperty.WeaponsEncumbrance, agent.AgentDrivenProperties.WeaponsEncumbrance + increaseAmount * 0.012f));
                    if (flag == WeaponFlags.MeleeWeapon)
                    {
                        BoostedAgents[agent].Add((DrivenProperty.SwingSpeedMultiplier, agent.AgentDrivenProperties.SwingSpeedMultiplier + increaseAmount * 0.0125f));
                        BoostedAgents[agent].Add((DrivenProperty.ThrustOrRangedReadySpeedMultiplier, agent.AgentDrivenProperties.ThrustOrRangedReadySpeedMultiplier + increaseAmount * 0.0125f));
                        if(agent.IsMainAgent)
                            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=increased_melee}A weapon you're carrying has enhanced your skill in combat, increasing your melee skills.").ToString(), Color.FromUint(9424384)));
                    }
                    else
                    {
                        BoostedAgents[agent].Add((DrivenProperty.ReloadSpeed, agent.AgentDrivenProperties.ReloadSpeed + increaseAmount * 0.0009f));
                        BoostedAgents[agent].Add((DrivenProperty.WeaponInaccuracy, agent.AgentDrivenProperties.WeaponInaccuracy - (increaseAmount * 0.0009f)));
                        if(agent.IsMainAgent)
                            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=increased_ranged}A weapon you're carrying has enhanced your skill in combat, increasing your ranged skills.").ToString(), Color.FromUint(9424384)));
                    }
                }
            }
        }

        private bool IsBattle() => Mission.IsFieldBattle || Mission.IsSiegeBattle || Mission.IsSallyOutBattle;
    }
}
