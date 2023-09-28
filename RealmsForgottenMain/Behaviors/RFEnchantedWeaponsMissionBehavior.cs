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
using RealmsForgotten.CustomSkills;
using RealmsForgotten.Patches;
using TaleWorlds.CampaignSystem.Actions;

namespace RealmsForgotten.Behaviors
{
    internal class RFEnchantedWeaponsMissionBehavior : MissionBehavior
    {
        private static Dictionary<int, (SkillObject, int)> heroesInitialSkills = new();
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

            if (IsBattle())
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
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {

            if (affectorWeapon.Item != null && affectorWeapon.CurrentUsageItem?.WeaponClass == WeaponClass.Cartridge && IncreaseAreaOfDamagePatch.CurrentBlow.OwnerId != blow.OwnerId)
            {
                if (affectorAgent.Character is CharacterObject attackerCharacterObject)
                {
                    float AreaFactor = attackerCharacterObject.GetPerkValue(RFPerks.Arcane.NeophytesStaff) ? RFPerks.Arcane.NeophytesStaff.PrimaryBonus :
                        (attackerCharacterObject.GetPerkValue(RFPerks.Arcane.InitiatesStaff) ? RFPerks.Arcane.InitiatesStaff.PrimaryBonus :
                            (attackerCharacterObject.GetPerkValue(RFPerks.Arcane.HierophantsStaff) ? RFPerks.Arcane.HierophantsStaff.PrimaryBonus : 0));
                    if (AreaFactor > 0)
                    {
                        AttackCollisionData CollisionData = attackCollisionData;
                        Blow blow1 = blow;
                        IncreaseAreaOfDamagePatch.isWand = AreaFactor;


                        IncreaseAreaOfDamagePatch.Prefix(ref CollisionData, ref blow1, affectedAgent, affectorAgent, false, Mission.Current);
                    }
                }
            }
        }

        private BasicCharacterObject[] modifiedCharacterObjects = { };
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
            if (agent.IsHero)
            {
                if (basicCharacterObject is CharacterObject characterObject)
                {
                    string skillString;
                    for (EquipmentIndex equipmentIndex = EquipmentIndex.Weapon0; equipmentIndex <= EquipmentIndex.Weapon3; equipmentIndex++)
                    {
                        skillString = skillsKeys.FirstOrDefault(x => characterObject.Equipment[equipmentIndex].Item?.StringId.Contains(x) == true);
                        if (skillString != null)
                        {
                            Hero hero = characterObject.HeroObject;
                            if (heroesInitialSkills.ContainsKey(agent.Index))
                            {
                                hero.SetSkillValue(heroesInitialSkills[agent.Index].Item1,
                                    heroesInitialSkills[agent.Index].Item2);
                                heroesInitialSkills.Remove(agent.Index);
                            }
                            else
                                heroesInitialSkills.Add(agent.Index, (skillsDic[skillString], agent.Character.GetSkillValue(skillsDic[skillString])));

                            hero.SetSkillValue(skillsDic[skillString], characterObject.GetSkillValue(skillsDic[skillString]) +
                                                                      RFUtility.GetNumberAfterSkillWord(characterObject.Equipment[equipmentIndex].Item?.StringId, skillString, hero == Hero.MainHero));
                        }
                    }
                }
            }
            else if(!modifiedCharacterObjects.Contains(basicCharacterObject))
            {
                bool haveEnchantedWeapon = false;
                string skillString;
                for (EquipmentIndex equipmentIndex = EquipmentIndex.Weapon0; equipmentIndex <= EquipmentIndex.Weapon3; equipmentIndex++)
                {
                    skillString = skillsKeys.FirstOrDefault(x => basicCharacterObject.Equipment[equipmentIndex].Item?.StringId.Contains(x) == true);
                    if (skillString != null)
                    {
                        haveEnchantedWeapon = true;

                        RFUtility.ModifyCharacterSkillAttribute(basicCharacterObject, skillsDic[skillString], basicCharacterObject.GetSkillValue(skillsDic[skillString]) +
                            RFUtility.GetNumberAfterSkillWord(basicCharacterObject.Equipment[equipmentIndex].Item?.StringId, skillString, false));
                    }
                }
                if(haveEnchantedWeapon)
                    modifiedCharacterObjects.AddItem(basicCharacterObject);
            }
        }

        protected bool IsBattle() =>
            this.Mission.IsFieldBattle || this.Mission.IsSiegeBattle || this.Mission.IsSallyOutBattle;
        protected override void OnEndMission()
        {
            if (heroesInitialSkills != null)
                foreach (Agent agent in Mission.AllAgents.Where(x => x.IsHero))
                {
                    if (agent.Character is CharacterObject character && heroesInitialSkills.ContainsKey(agent.Index))
                        character.HeroObject.SetSkillValue(heroesInitialSkills[agent.Index].Item1, heroesInitialSkills[agent.Index].Item2);
                }

        }

    }
}
