using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RealmsForgotten.Utility;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    internal class RFEnchantedWeaponsBehavior : MissionBehavior
    {
        private static Dictionary<int, (SkillObject, int)> agentsInitialSkills = new Dictionary<int, (SkillObject, int)>();
        private static Dictionary<string, SkillObject> skillsDic = new()
        {
            { "rfonehanded", DefaultSkills.OneHanded}, { "rftwohanded", DefaultSkills.TwoHanded }, { "rfpolearm", DefaultSkills.Polearm }, {"rfbow", DefaultSkills.Bow}, {"rfcrossbow", DefaultSkills.Crossbow},
            { "rfthrowing", DefaultSkills.Throwing }
        };
        public static string[] skillsKeys = skillsDic.Keys.ToArray();
        public override void OnCreated()
        {
            HaveDemoralizingArmor = (false, 0);
            HaveMoralizingArmor = (false, 0);
        }

        private static (bool, int) HaveDemoralizingArmor = (false, 0);
        private static (bool, int) HaveMoralizingArmor = (false, 0);

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnDeploymentFinished()
        {
            skillsDic = new()
            {
                { "rfonehanded", DefaultSkills.OneHanded}, { "rftwohanded", DefaultSkills.TwoHanded }, { "rfpolearm", DefaultSkills.Polearm }, {"rfbow", DefaultSkills.Bow}, {"rfcrossbow", DefaultSkills.Crossbow},
                { "rfthrowing", DefaultSkills.Throwing }
            };

            if (isBattle())
            {
                bool ismainagent = false;
                int amount = 0;
                if (this.Mission.PlayerTeam.GetHeroAgents().Any(x =>
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
                            ismainagent = x.IsMainAgent;
                            return true;
                        }

                    }

                    return false;
                })) ;
                {
                    if (amount > 0)
                        amount = -amount;
                    foreach (Agent agent in this.Mission.PlayerEnemyTeam.ActiveAgents)
                    {
                        if (agent.Character != null)
                        {
                            agent.ChangeMorale(amount);
                        }
                    }
                    if (ismainagent && amount != 0)
                        InformationManager.DisplayMessage(new InformationMessage($"Your armor intimidated the enemies and lowered their morale by {amount} points.", Color.FromUint(16711680)));
                    HaveDemoralizingArmor = (true, amount);
                }

                int amount2 = 0;
                if (this.Mission.PlayerTeam.GetHeroAgents().Any(x =>
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
                            ismainagent = x.IsMainAgent;
                            return true;
                        }

                    }

                    return false;
                })) ;
                {
                    if (amount2 < 0)
                        amount2 = +amount;
                    foreach (Agent agent in this.Mission.PlayerTeam.ActiveAgents)
                    {
                        if (agent.Character != null)
                        {
                            agent.ChangeMorale(amount2);
                        }
                    }
                    if (ismainagent && amount2 != 0)
                        InformationManager.DisplayMessage(new InformationMessage($"Your armor instilled confidence in your army boosting their morale by {amount2} points.", Color.FromUint(9424384)));
                    HaveMoralizingArmor = (true, amount2);
                }
            }

        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {


            if (isBattle() && agent.Team != null)
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
            if (agent.IsMainAgent)
                agent.OnMainAgentWieldedItemChange = delegate
                {
                    CharacterObject characterObject = agent.Character as CharacterObject;

                    if (characterObject != null && agent.WieldedWeapon.Item != null)
                    {
                        Hero hero = characterObject.HeroObject;
                        string stringId = agent.WieldedWeapon.Item.StringId;
                        string skillString =
                            skillsKeys.FirstOrDefault(x => agent.WieldedWeapon.Item.StringId.Contains(x));


                        if (agentsInitialSkills.ContainsKey(agent.Index))
                        {
                            hero.SetSkillValue(agentsInitialSkills[agent.Index].Item1,
                                agentsInitialSkills[agent.Index].Item2);
                            agentsInitialSkills.Remove(agent.Index);
                        }

                        if (skillString != null)
                        {
                            if (!agentsInitialSkills.ContainsKey(agent.Index))
                                agentsInitialSkills.Add(agent.Index,
                                    (skillsDic[skillString],
                                        agent.Character.GetSkillValue(skillsDic[skillString])));
                            hero.SetSkillValue(skillsDic[skillString],
                                characterObject.GetSkillValue(skillsDic[skillString]) +
                                RFUtility.GetNumberAfterSkillWord(stringId, skillString, true));
                        }

                    }
                };
            else
                agent.OnAgentWieldedItemChange = delegate
                {
                    if (agent.Character != null && agent.WieldedWeapon.Item != null)
                    {
                        CharacterObject characterObject = agent.Character as CharacterObject;
                        string stringId = agent.WieldedWeapon.Item.StringId;
                        string skillString =
                            skillsKeys.FirstOrDefault(x => agent.WieldedWeapon.Item.StringId.Contains(x));

                        if (agentsInitialSkills.ContainsKey(agent.Index))
                        {
                            if (characterObject != null && characterObject.HeroObject != null)
                                characterObject.HeroObject.SetSkillValue(agentsInitialSkills[agent.Index].Item1,
                                    agentsInitialSkills[agent.Index].Item2);
                            else
                                RFUtility.ModifyCharacterSkillAttribute(agent.Character,
                                    agentsInitialSkills[agent.Index].Item1, agentsInitialSkills[agent.Index].Item2);
                            agentsInitialSkills.Remove(agent.Index);
                        }

                        if (skillString != null)
                        {
                            if (!agentsInitialSkills.ContainsKey(agent.Index))
                                agentsInitialSkills.Add(agent.Index,
                                    (skillsDic[skillString],
                                        agent.Character.GetSkillValue(skillsDic[skillString])));
                            if (characterObject != null && characterObject.HeroObject != null)
                                characterObject.HeroObject.SetSkillValue(skillsDic[skillString],
                                    characterObject.GetSkillValue(skillsDic[skillString]) +
                                    RFUtility.GetNumberAfterSkillWord(stringId, skillString));
                            else
                                RFUtility.ModifyCharacterSkillAttribute(agent.Character, skillsDic[skillString],
                                    agent.Character.GetSkillValue(skillsDic[skillString]) +
                                    RFUtility.GetNumberAfterSkillWord(stringId, skillString));
                        }
                    }
                };

        }

        protected bool isBattle() =>
            this.Mission.IsFieldBattle || this.Mission.IsSiegeBattle || this.Mission.IsSallyOutBattle;
        protected override void OnEndMission()
        {
            if (agentsInitialSkills != null)
                foreach (Agent agent in Mission.AllAgents.Where(x => x.IsHero))
                {
                    CharacterObject character = agent.Character as CharacterObject;

                    if (character != null && agentsInitialSkills.ContainsKey(agent.Index))
                        character.HeroObject.SetSkillValue(agentsInitialSkills[agent.Index].Item1, agentsInitialSkills[agent.Index].Item2);
                }

        }

    }
}
