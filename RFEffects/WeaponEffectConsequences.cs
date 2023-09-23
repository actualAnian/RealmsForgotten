using HarmonyLib;
using RealmsForgotten.RFEffects;
using RealmsForgotten.RFEffects.Utilities;
using RealmsForgotten.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealmsForgotten.Models;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using static TaleWorlds.MountAndBlade.Mission;

namespace RFEffects
{
    public delegate void VictimAgentConsequence(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null);
    public static class WeaponEffectConsequences
    {
        public static Dictionary<string, VictimAgentConsequence> AllMethods = new();
        public static void Fire(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {

            Timer burningTimer = new Timer(Time.ApplicationTime, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Duration, false);
            Timer fireHitTimer = new Timer(Time.ApplicationTime, 2f);

            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(affectedAgent, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Effect, burningTimer, gameEntity));


            if (!MagicEffectsBehavior.Instance.BurningEffectStopwatch.ContainsKey(affectedAgent.Index))
                MagicEffectsBehavior.Instance.BurningEffectStopwatch.Add(affectedAgent.Index, fireHitTimer);
            else
                MagicEffectsBehavior.Instance.BurningEffectStopwatch[affectedAgent.Index] = fireHitTimer;

        }
        public static void GreenSpark(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];

            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow);

            foreach (Agent agent in agentsInRadius)
            {
                ApplyParticleOnSingleAgent(agent, weaponEffectData, agent == affectedAgent ? gameEntity : null);

                agent.AgentDrivenProperties.CombatMaxSpeedMultiplier +=
                    agent.AgentDrivenProperties.CombatMaxSpeedMultiplier * 0.25f;

                agent.AgentDrivenProperties.MaxSpeedMultiplier +=
                    agent.AgentDrivenProperties.MaxSpeedMultiplier * 0.25f;

                agent.UpdateCustomDrivenProperties();
            }
        }
        public static void PurpleSpark(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];

            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow);

            foreach (Agent agent in agentsInRadius)
            {
                ApplyParticleOnSingleAgent(agent, weaponEffectData, agent == affectedAgent ? gameEntity : null);

                RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.Add(agent.Index, -0.15f);
                agent.UpdateCustomDrivenProperties();
            }
        }

        public static void Terror(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            affectedAgent.ChangeMorale(-25);

            Timer timer = new Timer(Time.ApplicationTime, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Duration, false);
            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(affectedAgent, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Effect, timer, gameEntity));

        }
        public static void Force(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];
            float areaOfEffect = weaponEffectData.AreaOfEffect;

            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow);

            foreach (Agent agent in agentsInRadius)
            {
                ApplyParticleOnSingleAgent(agent, weaponEffectData, agent == affectedAgent ? gameEntity : null);

                RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.Add(agent.Index, 0.15f);

                agent.UpdateCustomDrivenProperties();
            }
        }
        public static void Ice(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            affectedAgent.AgentDrivenProperties.MaxSpeedMultiplier = 0.01f;
            affectedAgent.AgentDrivenProperties.CombatMaxSpeedMultiplier = 0.01f;

            affectedAgent.UpdateCustomDrivenProperties();

            Timer timer = new Timer(Time.ApplicationTime, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Duration, false);


            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(affectedAgent, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Effect, timer, gameEntity));

        }

        public static void Heal(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];
            float areaOfEffect = weaponEffectData.AreaOfEffect;

            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow);

            foreach (Agent agent in agentsInRadius)
            {
                ApplyParticleOnSingleAgent(agent, weaponEffectData, agent == affectedAgent ? gameEntity : null);

                float HealedAgentHealth = agent.Health + 20f;
                agent.Health = HealedAgentHealth > 100 ? 100 : HealedAgentHealth;
            }
        }
        private static void ApplyParticleOnSingleAgent(Agent agent, WeaponEffectData weaponEffectData, GameEntity firstAffectedGameEntity = null)
        {
            bool isFirstAffected = firstAffectedGameEntity != null;
            if (!isFirstAffected)
                TOWParticleSystem.ApplyParticleToAgent(agent, weaponEffectData.VictimParticle, out firstAffectedGameEntity, TOWParticleSystem.ParticleIntensity.Low, false);

            Timer timer = new Timer(Time.ApplicationTime, weaponEffectData.Duration, false);

            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(agent, weaponEffectData.Effect, timer, firstAffectedGameEntity));
        }
        private static List<Agent> GetRadiusAgentsOrSingle(Agent affectedAgent, Agent affectorAgent, WeaponEffectData weaponEffectData, Blow blow)
        {
            float areaOfEffect = weaponEffectData.AreaOfEffect;

            if (areaOfEffect <= 0)
                return new List<Agent>() { affectedAgent };

            return RFUtility.GetAgentsInRadius(blow.GlobalPosition.AsVec2, areaOfEffect).Where(agent => !agent.IsEnemyOf(affectorAgent)).ToList();
        }
    }
}
