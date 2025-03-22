using Newtonsoft.Json.Linq;
using RealmsForgotten.ObjectExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Ability
{
    public class AbilityData
    {
        public int Duration { get; private set; }
        public int Cooldown { get; private set; }
        public AbilityData(int duration, int cooldown, Dictionary<ActionTrigger, List<Delegate>> baseActions, Dictionary<ActionTrigger, List<Delegate>> upgradedActions)
        {
            Duration = duration;
            Cooldown = cooldown;
            BaseActions = baseActions;
            UpgradedActions = upgradedActions;
        }

        public enum ActionTrigger
        {
            OnActivate,
            OnDeactivate,
            OnTroopHit
        }
        public Dictionary<ActionTrigger, List<Delegate>> BaseActions { get; set; }
        public Dictionary<ActionTrigger, List<Delegate>> UpgradedActions { get; set; }
    }
    public static class AbilityEffects
    {
        public static List<Agent> agentsWithProperties = new();
        private static readonly float battleCrySwingSpeedMult = 0.2f;
        public static void GiveBerserkerEffects()
        {
            Agent.Main.AgentDrivenProperties.SwingSpeedMultiplier *= (1 + battleCrySwingSpeedMult);
            IEnumerable<Agent> agents = Mission.Current.Agents.Where(a => a.BelongsToMainParty());
            foreach (Agent agent in agents)
            {
                agentsWithProperties.Add(agent);
                agent.AgentDrivenProperties.SwingSpeedMultiplier *= (1 + battleCrySwingSpeedMult);
                agent.UpdateCustomDrivenProperties();
            }
        }
        public static void CheckAgents()
        {
            for (int i = agentsWithProperties.Count - 1; i >= 0; i--)
            {
                if (!agentsWithProperties[i].IsActive()) agentsWithProperties.RemoveAt(i);
            }
        }
        public static void RemoveBerserkerEffects()
        {
            CheckAgents();
            for (int i = agentsWithProperties.Count; i >= 0; i--)
            {
                Agent agent = agentsWithProperties[i];
                agent.AgentDrivenProperties.SwingSpeedMultiplier /= 1.2f;
                agent.UpdateCustomDrivenProperties();
                agentsWithProperties.RemoveAt(i);
            }
        }
        public static void GiveFiftyMeleeResistance(Agent attacker, Agent victim, float[] additionalDamagePercentages, float[] resistancePercentages)
        {
            if (victim.BelongsToMainParty()) resistancePercentages[1] += 0.5f;
        }
        public static void IncreaseMorale()
        {
            foreach (Agent? agent in Mission.Current.PlayerTeam.ActiveAgents)
            {
                if (agent.BelongsToMainParty())
                    agent.ChangeMorale(20);
            }
        }
    }
}