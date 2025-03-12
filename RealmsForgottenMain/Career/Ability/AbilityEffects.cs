using RealmsForgotten.ObjectExtensions;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Ability
{
    public static class AbilityEffects
    {
        public static List<Agent> agentsWithProperties = new();
        private static readonly int battleCrySwingSpeedMult = 2;
        public static void GiveBerserkerEffects()
        {
            IEnumerable<Agent> agents = Mission.Current.Agents.Where(a => a.BelongsToMainParty());
            foreach (Agent agent in agents)
            {
                agentsWithProperties.Add(agent);
                agent.AgentDrivenProperties.SwingSpeedMultiplier *= battleCrySwingSpeedMult;
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
            foreach (Agent agent in agentsWithProperties)
            {
                agentsWithProperties.Add(agent);
                agent.AgentDrivenProperties.SwingSpeedMultiplier /= battleCrySwingSpeedMult;
            }
        }
    }
}