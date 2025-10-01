using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.Models;
using RealmsForgotten.Utility;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
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
            return Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Duel || Mission.Mode == MissionMode.Stealth || Mission.Mode == MissionMode.Tournament;
        }

        public override void OnAgentDeleted(Agent agent)
        {
            if (!IsInBattle())
            {
                return;
            }
            AgentsUnderEffect.RemoveAll(x => x.Agent == agent);
        }

        public static void ApplyEffect(Agent victim, string effectId, float durationSeconds)
        {
            if (Instance == null || victim == null || !victim.IsActive()) return;

            // refresh if already present
            int idx = Instance.AgentsUnderEffect.FindIndex(e => e.Agent == victim && e.Effect == effectId);
            if (idx >= 0)
            {
                var data = Instance.AgentsUnderEffect[idx];
                data.Timer = new Timer(Time.ApplicationTime, durationSeconds, false);
                Instance.AgentsUnderEffect[idx] = data;
            }
            else
            {
                var timer = new Timer(Time.ApplicationTime, durationSeconds, false);
                Instance.AgentsUnderEffect.Add(new AgentEffectData(victim, effectId, timer, null));
            }

            // special handling for burn tick cadence
            if (effectId == "Fire")
                Instance.BurningEffectStopwatch[victim.Index] = new Timer(Time.ApplicationTime, 2f);
        }
        private void FireTick(AgentEffectData agentEffect)
        {

            if (BurningEffectStopwatch.TryGetValue(agentEffect.Agent.Index, out Timer timer) && timer.Check(Time.ApplicationTime))
            {
                Agent victim = agentEffect.Agent;
                Blow blow = CreateBlow(victim, MBRandom.RandomInt(5, 10), victim.Index);

                // --- VERSÃO FINAL E CORRIGIDA DO ATTACKCOLLISIONDATA ---
                AttackCollisionData attackCollisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                    false, false, false, false, false, false, false, false, false, false, false, false,
                    CombatCollisionResult.StrikeAgent,
                    -1, 0, victim.Index,
                    blow.BoneIndex, BoneBodyPartType.Head,
                    victim.Monster.MainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1,
                    CombatHitResultFlags.NormalHit,
                    0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                    victim.LookDirection, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, victim.Velocity,
                    Vec3.Zero);


                victim.RegisterBlow(blow, attackCollisionData);
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

            for (int i = AgentsUnderEffect.Count - 1; i >= 0; i--)
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
