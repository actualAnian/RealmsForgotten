using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
	public class RFMissionBehaviour : MissionBehavior
	{

		public override MissionBehaviorType BehaviorType
		{
			get
			{
                return MissionBehaviorType.Other;
			}
		}

		private bool IsInBattle()
		{
			return base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Duel || base.Mission.Mode == MissionMode.Stealth || base.Mission.Mode == MissionMode.Tournament;
		}

        public override void OnCreated()
        {
            base.OnCreated();
        }

        public override void OnAgentDeleted(Agent agent)
		{
			if (!this.IsInBattle())
			{
				return;
			}
			this.toBeRemoved.Add(agent);
		}

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
						Blow blow = this.CreateBlow(keyValuePair.Key, num, this.agentsUnderFire[keyValuePair.Key.Index]);
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
		private bool CheckAgent(Agent agent)
		{
			return agent == this.currVictim;
		}


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



        public Dictionary<Agent, double> victimsDamage = new Dictionary<Agent, double>();

		public List<Agent> toBeRemoved = new List<Agent>();

		public List<Agent> toBeAdded = new List<Agent>();

		private Agent currVictim;

		private double clockGeneratorTime;

		public Dictionary<int, int> agentsUnderFire = new Dictionary<int, int>();
	}
}
