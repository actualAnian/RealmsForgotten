
using System;
using RealmsForgotten.Utility;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.Models;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
    public delegate void VictimAgentConsequence(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null);
    public static class WeaponEffectConsequences
    {
        public static Dictionary<string, VictimAgentConsequence> Methods = new();
        public static void Fire(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {

            Timer burningTimer = new(Time.ApplicationTime, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Duration, false);
            Timer fireHitTimer = new(Time.ApplicationTime, 2f);

            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(affectedAgent, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Effect, burningTimer, gameEntity));


            if (!MagicEffectsBehavior.Instance.BurningEffectStopwatch.ContainsKey(affectedAgent.Index))
                MagicEffectsBehavior.Instance.BurningEffectStopwatch.Add(affectedAgent.Index, fireHitTimer);
            else
                MagicEffectsBehavior.Instance.BurningEffectStopwatch[affectedAgent.Index] = fireHitTimer;

            if (affectorAgent.IsMainAgent)
            {
                TextObject fireTextObject = new("{=magic_fire_report}Enemy set on fire!");

                InformationManager.DisplayMessage(new InformationMessage(fireTextObject.ToString(), Color.FromUint(11936259)));
            }


        }
        public static void GreenSpark(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];

            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow, agent => !affectorAgent.IsEnemyOf(agent));



            int amount = 0;
            for (; amount < agentsInRadius.Count; amount++)
            {
                ApplyParticleOnSingleAgent(agentsInRadius[amount], weaponEffectData, agentsInRadius[amount] == affectedAgent ? gameEntity : null);

                agentsInRadius[amount].AgentDrivenProperties.CombatMaxSpeedMultiplier +=
                    agentsInRadius[amount].AgentDrivenProperties.CombatMaxSpeedMultiplier * 0.25f;

                agentsInRadius[amount].AgentDrivenProperties.MaxSpeedMultiplier +=
                    agentsInRadius[amount].AgentDrivenProperties.MaxSpeedMultiplier * 0.25f;

                agentsInRadius[amount].UpdateCustomDrivenProperties();

                InformationManager.DisplayMessage(new InformationMessage());
            }

            if (affectorAgent.IsMainAgent && amount > 0)
            {
                TextObject greensparkTextObject = new("{=greenspark_heal_report}{AMOUNT} troops were speeded!");
                greensparkTextObject.SetTextVariable("AMOUNT", amount);

                InformationManager.DisplayMessage(new InformationMessage(greensparkTextObject.ToString(), Color.FromUint(3265601)));
            }

        }
        public static void PurpleSpark(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];


            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow, agent => affectorAgent.IsEnemyOf(agent));


            int amount = 0;
            for (; amount < agentsInRadius.Count; amount++)
            {
                ApplyParticleOnSingleAgent(agentsInRadius[amount], weaponEffectData, agentsInRadius[amount] == affectedAgent ? gameEntity : null);

                if (RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.ContainsKey(agentsInRadius[amount].Index))
                    RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.Remove(agentsInRadius[amount].Index);

                RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.Add(agentsInRadius[amount].Index, -0.25f);

                agentsInRadius[amount].UpdateCustomDrivenProperties();
            }

            if (affectorAgent.IsMainAgent && amount > 0)
            {
                TextObject purplesparkTextObject = new("{=purplespark_heal_report}{AMOUNT} enemies were weakened!");
                purplesparkTextObject.SetTextVariable("AMOUNT", amount);

                InformationManager.DisplayMessage(new InformationMessage(purplesparkTextObject.ToString(), Color.FromUint(5190338)));
            }


        }

        public static void Terror(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            affectedAgent.ChangeMorale(-25);

            Timer timer = new(Time.ApplicationTime, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Duration, false);
            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(affectedAgent, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Effect, timer, gameEntity));



            if (affectorAgent.IsMainAgent)
            {
                TextObject terrorTextObject;
                if (affectedAgent.GetMorale() <= 0)
                    terrorTextObject = new("{=terror_heal_report}Enemy terrified!");
                else
                    terrorTextObject = new("{=terror_heal_report}Enemy morale decreased, a bit more a he will flee off the combat!");

                InformationManager.DisplayMessage(new InformationMessage(terrorTextObject.ToString(), Color.Black));
            }
        }
        public static void Force(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];

            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow, agent => !affectorAgent.IsEnemyOf(agent));

            int amount = 0;
            for (; amount < agentsInRadius.Count; amount++)
            {
                ApplyParticleOnSingleAgent(agentsInRadius[amount], weaponEffectData, agentsInRadius[amount] == affectedAgent ? gameEntity : null);


                if (RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.ContainsKey(agentsInRadius[amount].Index))
                    RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.Remove(agentsInRadius[amount].Index);

                RFAgentApplyDamageModel.Instance.ModifiedDamageAgents.Add(agentsInRadius[amount].Index, 0.25f);

                agentsInRadius[amount].UpdateCustomDrivenProperties();
            }

            if (affectorAgent.IsMainAgent && amount > 0)
            {
                TextObject healTextObject = new("{=force_heal_report}{AMOUNT} troops were strengthened!");
                healTextObject.SetTextVariable("AMOUNT", amount);
                InformationManager.DisplayMessage(new InformationMessage(healTextObject.ToString(), Color.FromUint(12389648)));
            }



        }
        
        public static void Ice(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            affectedAgent.AgentDrivenProperties.MaxSpeedMultiplier = 0.01f;
            affectedAgent.AgentDrivenProperties.CombatMaxSpeedMultiplier = 0.01f;
            affectedAgent.AgentDrivenProperties.MountSpeed = 0.01f;

            affectedAgent.UpdateCustomDrivenProperties();

            Timer timer = new(Time.ApplicationTime, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Duration, false);


            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(affectedAgent, RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId].Effect, timer, gameEntity));

            if (affectorAgent.IsMainAgent)
            {
                TextObject iceTextObject = new("{=ice_heal_report}Enemy freezed!");
                InformationManager.DisplayMessage(new InformationMessage(iceTextObject.ToString(), Color.FromUint(2147280)));
            }
        }

        public static void Heal(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData, GameEntity gameEntity = null)
        {
            WeaponEffectData weaponEffectData = RFEffectsLibrary.CurrentWeaponEffects[affectorWeapon.Item.StringId];

            List<Agent> agentsInRadius = GetRadiusAgentsOrSingle(affectedAgent, affectorAgent, weaponEffectData, blow, agent => !affectorAgent.IsEnemyOf(agent));

            int amount = 0;

            for (; amount < agentsInRadius.Count; amount++)
            {
                ApplyParticleOnSingleAgent(agentsInRadius[amount], weaponEffectData, agentsInRadius[amount] == affectedAgent ? gameEntity : null);

                float HealedAgentHealth = agentsInRadius[amount].Health + 20f;
                agentsInRadius[amount].Health = HealedAgentHealth > 100 ? 100 : HealedAgentHealth;
                amount++;
            }

            if (affectorAgent.IsMainAgent && amount > 0)
            {
                TextObject healTextObject = new("{=magic_heal_report}{AMOUNT} troops were healed!");
                healTextObject.SetTextVariable("AMOUNT", amount);
                InformationManager.DisplayMessage(new InformationMessage(healTextObject.ToString(), Color.FromUint(9690633)));
            }
        }
        private static void ApplyParticleOnSingleAgent(Agent agent, WeaponEffectData weaponEffectData, GameEntity firstAffectedGameEntity = null)
        {
            bool isFirstAffected = firstAffectedGameEntity != null;
            if (!isFirstAffected)
                TOWParticleSystem.ApplyParticleToAgent(agent, weaponEffectData.VictimParticle, out firstAffectedGameEntity, TOWParticleSystem.ParticleIntensity.Low, false);

            Timer timer = new(Time.ApplicationTime, weaponEffectData.Duration, false);

            MagicEffectsBehavior.Instance.AgentsUnderEffect.Add(new AgentEffectData(agent, weaponEffectData.Effect, timer, firstAffectedGameEntity));
        }
        private static List<Agent> GetRadiusAgentsOrSingle(Agent affectedAgent, Agent affectorAgent, WeaponEffectData weaponEffectData, Blow blow, Func<Agent, bool> condition)
        {
            float areaOfEffect = weaponEffectData.AreaOfEffect;

            if (areaOfEffect <= 0)
                return new List<Agent>() { affectedAgent };

            return RFUtility.GetAgentsInRadius(blow.GlobalPosition.AsVec2, areaOfEffect).Where(condition).ToList();
        }
    }
}
