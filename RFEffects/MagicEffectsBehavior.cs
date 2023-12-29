using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using RealmsForgotten.Models;
using RealmsForgotten.Utility;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
    public class MagicEffectsBehavior : MissionBehavior
    {
        public static MagicEffectsBehavior Instance;
        public List<AgentEffectData> AgentsUnderEffect = new();
        public Dictionary<int, Timer> BurningEffectStopwatch = new();
        

        public MagicEffectsBehavior()
        {
            Instance = this;
        }
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

        public override void OnAgentDeleted(Agent agent)
        {
            if (!IsInBattle())
            {
                return;
            }
            AgentsUnderEffect.RemoveAll(x => x.Agent == agent);
        }

        private void FireTick(AgentEffectData agentEffect)
        {

            if (BurningEffectStopwatch.TryGetValue(agentEffect.Agent.Index, out Timer timer) && timer.Check(Time.ApplicationTime))
            {
                Blow blow = CreateBlow(agentEffect.Agent, MBRandom.RandomInt(5, 10), agentEffect.Agent.Index);
                AttackCollisionData attackCollisionData = default(AttackCollisionData);
                ref AttackCollisionData collisionData = ref attackCollisionData;
                agentEffect.Agent.RegisterBlow(blow, collisionData);
                timer.Reset(Time.ApplicationTime);

                if (agentEffect.Timer.Check(Time.ApplicationTime))
                {
                    agentEffect.RemoveEffect();
                    AgentsUnderEffect.Remove(agentEffect);
                    BurningEffectStopwatch.Remove(agentEffect.Agent.Index);
                }
            }
        }


        private void ModifiedDrivenPropertiesTick(AgentEffectData agentEffect)
        {
            if (agentEffect.Timer.Check(Time.ApplicationTime))
            {
                agentEffect.RemoveEffect();

                MissionGameModels.Current.AgentStatCalculateModel.UpdateAgentStats(agentEffect.Agent, agentEffect.Agent.AgentDrivenProperties);

                agentEffect.Agent.UpdateCustomDrivenProperties();

                AgentsUnderEffect.Remove(agentEffect);

            }
        }

        private void ModifyDamageTick(AgentEffectData agentEffect)
        {
            if (agentEffect.Timer.Check(Time.ApplicationTime))
            {
                RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.Remove(agentEffect.Agent.Index);
                agentEffect.RemoveEffect();
                AgentsUnderEffect.Remove(agentEffect);
            }
        }
        private void DefaultTick(AgentEffectData agentEffect)
        {
            if (agentEffect.Timer.Check(Time.ApplicationTime))
            {
                agentEffect.RemoveEffect();

                AgentsUnderEffect.Remove(agentEffect);

            }
        }
        public override void OnMissionTick(float dt)
        {
            if (!IsInBattle() || AgentsUnderEffect.Count == 0)
            {
                return;
            }

            for (int i = 0; i < AgentsUnderEffect.Count; i++)
            {
                AgentEffectData agentEffectData = AgentsUnderEffect[i];

                switch (agentEffectData.Effect)
                {
                    case "Fire":
                        FireTick(agentEffectData);
                        break;
                    case "Ice":
                    case "GreenSpark":
                        ModifiedDrivenPropertiesTick(agentEffectData);
                        break;
                    case "PurpleSpark":
                    case "Force":
                        ModifyDamageTick(agentEffectData);
                        break;
                    default:
                        DefaultTick(agentEffectData);
                        break;
                }
            }



        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            foreach (AgentEffectData agentEffectData in AgentsUnderEffect.Where(x => x.Effect == "power"))
            {
                RFUtility.ModifyCharacterSkillAttribute(agentEffectData.Agent.Character, DefaultSkills.Athletics, agentEffectData.Agent.Character.GetSkillValue(DefaultSkills.Athletics) / 3);
            }
        }

        private Blow CreateBlow(Agent victim, int damage, int attackerId)
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
            blow.InflictedDamage = damage;
            blow.SwingDirection = victim.LookDirection;
            blow.Direction = blow.SwingDirection;
            blow.DamageCalculated = true;
            return blow;
        }


    }
}
