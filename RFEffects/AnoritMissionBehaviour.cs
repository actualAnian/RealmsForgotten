using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
	// Token: 0x02000004 RID: 4
	public class AnoritMissionBehaviour : MissionBehavior
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000011 RID: 17 RVA: 0x000025DE File Offset: 0x000007DE
		public override MissionBehaviorType BehaviorType
		{
			get
			{
                return MissionBehaviorType.Other;
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000025E1 File Offset: 0x000007E1
		private bool IsInBattle()
		{
			return base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Duel || base.Mission.Mode == MissionMode.Stealth || base.Mission.Mode == MissionMode.Tournament;
		}



		// Token: 0x06000013 RID: 19 RVA: 0x0000261D File Offset: 0x0000081D
		public override void OnAgentDeleted(Agent agent)
		{
			if (!this.IsInBattle())
			{
				return;
			}
			this.toBeRemoved.Add(agent);
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002634 File Offset: 0x00000834
		public override void OnMissionTick(float dt)
		{
			if (!this.IsInBattle() || (this.victimsDamage.Count == 0 && this.toBeAdded.Count == 0))
			{
				return;
			}
			this.clockGeneratorTime += (double)dt;
			if (this.clockGeneratorTime >= 1.0)
			{
				this.clockGeneratorTime = 0.0;
				for (int i = this.toBeRemoved.Count - 1; i >= 0; i--)
				{
					if (i < this.toBeRemoved.Count)
					{
						this.currVictim = this.toBeRemoved[i];
						this.victimsDamage.Remove(this.currVictim);
						this.toBeRemoved.RemoveAll(new Predicate<Agent>(this.CheckAgent));
						this.toBeAdded.RemoveAll(new Predicate<Agent>(this.CheckAgent));
					}
				}
				for (int j = this.toBeAdded.Count - 1; j >= 0; j--)
				{
					if (j < this.toBeAdded.Count)
					{
						this.currVictim = this.toBeAdded[j];
						if (!this.victimsDamage.ContainsKey(this.currVictim))
						{
							this.victimsDamage.Add(this.currVictim, 0.0);
						}
						else
						{
							this.victimsDamage[this.currVictim] = 0.0;
						}
						this.toBeAdded.RemoveAll(new Predicate<Agent>(this.CheckAgent));
					}
				}
				List<KeyValuePair<Agent, double>> list = this.victimsDamage.ToList<KeyValuePair<Agent, double>>();
				for (int k = 0; k < this.victimsDamage.Count; k++)
				{
					KeyValuePair<Agent, double> keyValuePair = list[k];
					if (keyValuePair.Value < 500.0 && keyValuePair.Key.IsActive())
					{
						int num = 5;
						Dictionary<Agent, double> dictionary = this.victimsDamage;
						Agent key = keyValuePair.Key;
						dictionary[key] += (double)num;
						Blow blow = this.CreateBlow(keyValuePair.Key, num, this.attackerId[keyValuePair.Key.Index]);
						AttackCollisionData attackCollisionData = default(AttackCollisionData);
						ref AttackCollisionData collisionData = ref attackCollisionData;
						keyValuePair.Key.RegisterBlow(blow, collisionData);
					}
					else

					{
						this.toBeRemoved.Add(keyValuePair.Key);
					}
				}
			}
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002882 File Offset: 0x00000A82
		private bool CheckAgent(Agent agent)
		{
			return agent == this.currVictim;
		}



        // Token: 0x06000016 RID: 22 RVA: 0x00002890 File Offset: 0x00000A90
        private Blow CreateBlow(Agent victim, int damagePerSecond, int attackerId)
		{
			Blow blow = new Blow(attackerId);
			blow.DamageType = DamageTypes.Blunt;
			blow.BlowFlag = BlowFlags.ShrugOff;
			blow.BlowFlag |= BlowFlags.NoSound;
			blow.BoneIndex = victim.Monster.HeadLookDirectionBoneIndex;
			blow.GlobalPosition = victim.Position;
			blow.GlobalPosition.z = blow.GlobalPosition.z + victim.GetEyeGlobalHeight();
			blow.BaseMagnitude = 0f;
			blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
			blow.InflictedDamage = damagePerSecond;
			blow.SwingDirection = victim.LookDirection;
			blow.Direction = blow.SwingDirection;
			blow.DamageCalculated = true;
			return blow;
		}

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
                                amount = GetNumberAfterSkillWord(
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
                                amount2 = GetNumberAfterSkillWord(
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
                    if(ismainagent && amount2 != 0)
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
                                    GetNumberAfterSkillWord(stringId, skillString, true));
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
                                    ModifyCharacterSkillAttribute(agent.Character,
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
                                        GetNumberAfterSkillWord(stringId, skillString));
                                else
                                    ModifyCharacterSkillAttribute(agent.Character, skillsDic[skillString],
                                        agent.Character.GetSkillValue(skillsDic[skillString]) +
                                        GetNumberAfterSkillWord(stringId, skillString));
                            }
                        }
                    };
            
        }

        protected bool isBattle() =>
            this.Mission.IsFieldBattle || this.Mission.IsSiegeBattle || this.Mission.IsSallyOutBattle;
        protected override void OnEndMission()
        {
            if(agentsInitialSkills != null)
            foreach (Agent agent in Mission.AllAgents.Where(x=>x.IsHero))
            {
				CharacterObject character = agent.Character as CharacterObject;
                    
                if (character != null && agentsInitialSkills.ContainsKey(agent.Index))
                    character.HeroObject.SetSkillValue(agentsInitialSkills[agent.Index].Item1, agentsInitialSkills[agent.Index].Item2);
            }

        }
        public void ModifyCharacterSkillAttribute(BasicCharacterObject character, SkillObject skill, int value)
        {

            FieldInfo characterSkillsProperty = AccessTools.Field(typeof(BasicCharacterObject), "DefaultCharacterSkills");
            if (characterSkillsProperty == null)
                return;
            object characterSkills = characterSkillsProperty.GetValue(character);

            PropertyInfo skillsInfo = AccessTools.Property(characterSkills.GetType(), "Skills");
            object skillValue = skillsInfo.GetValue(characterSkills);
            FieldInfo attributesField = AccessTools.Field(skillValue.GetType(), "_attributes");
            Dictionary<SkillObject, int> attributes = (Dictionary<SkillObject, int>)attributesField.GetValue(skillValue);

            attributes[skill] = value;
            attributesField.SetValue(skillValue, attributes);
        }
        public static int GetNumberAfterSkillWord(string inputString, string word, bool isMainAgent = false)
        {
            int result = -1; 
            int wordIndex = inputString.IndexOf(word);

            if (wordIndex >= 0)
            {
                string textAfterWord = inputString.Substring(wordIndex + word.Length);

                Match match = Regex.Match(textAfterWord, @"\d+");

                if (match.Success)
                {
                    result = int.Parse(match.Value);
                }
            }

            if (isMainAgent)
            {
                string skill = null;
                switch (word)
                {
                    case "rfonehanded":
                        skill = "One Handed";
                        break;
                    case "rftwohanded":
                        skill = "Two Handed";
                        break;
                    case "rfpolearm":
                        skill = "Polearm";
                        break;
                    case "rfbow":
                        skill = "Bow";
                        break;
                    case "rfcrossbow":
                        skill = "Crossbow";
                        break;
                    case "rfthrowing":
                        skill = "Throwing";
                        break;

                }

                InformationManager.DisplayMessage(new InformationMessage($"The weapon you are wielding has enhanced your skill in combat, increasing your {skill} by {result} points.", Color.FromUint(9424384)));
            }

            return result;
        }

        // Token: 0x0400000A RID: 10
        public Dictionary<Agent, double> victimsDamage = new Dictionary<Agent, double>();

		// Token: 0x0400000B RID: 11
		public List<Agent> toBeRemoved = new List<Agent>();

		// Token: 0x0400000C RID: 12
		public List<Agent> toBeAdded = new List<Agent>();

		// Token: 0x0400000D RID: 13
		private Agent currVictim;

		// Token: 0x0400000E RID: 14
		private double clockGeneratorTime;

		// Token: 0x0400000F RID: 15
		public Dictionary<int, int> attackerId = new Dictionary<int, int>();
	}
}
