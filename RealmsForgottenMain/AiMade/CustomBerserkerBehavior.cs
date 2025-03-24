using RealmsForgotten.AiMade.Models;
using RealmsForgotten.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace RealmsForgotten.AiMade
{
    public class CustomBerserkerBehavior : MissionBehavior
    {
        private readonly HashSet<Agent> berserkerAgents = new();
        private readonly Dictionary<Agent, Timer> agentTimers = new();
        private const float BerserkerDuration = 45f;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public bool berserkerModeActive => berserkerAgents.Count > 0; // True if any agent is in Berserker mode

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            foreach (Agent agent in Mission.Current.Agents)
            {
                if (IsBerserkerCandidate(agent) && !berserkerAgents.Contains(agent))
                {
                    if (agent.Health < agent.HealthLimit * 0.66f) // Condition to trigger Berserker
                    {
                        ActivateBerserkerMode(agent);
                    }
                }
            }

            foreach (var agent in berserkerAgents.ToList())
            {
                if (agentTimers.TryGetValue(agent, out var timer) && timer.Check(Time.ApplicationTime))
                {
                    DeactivateBerserkerMode(agent);
                }
            }
        }

        private void ActivateBerserkerMode(Agent agent)
        {
            berserkerAgents.Add(agent);
            agentTimers[agent] = new Timer(Time.ApplicationTime, BerserkerDuration);

            agent.HealthLimit += 20;
            agent.Health = Math.Min(agent.Health + 10, agent.HealthLimit);
            agent.SetMaximumSpeedLimit(agent.MaximumForwardUnlimitedSpeed * 1.2f, true);

            InformationManager.DisplayMessage(new InformationMessage($"{agent.Name} enters Berserker mode!", Colors.Red));
        }

        private void DeactivateBerserkerMode(Agent agent)
        {
            berserkerAgents.Remove(agent);
            agentTimers.Remove(agent);

            agent.HealthLimit -= 20;
            agent.SetMaximumSpeedLimit(agent.MaximumForwardUnlimitedSpeed / 1.2f, true);

            InformationManager.DisplayMessage(new InformationMessage($"{agent.Name} exits Berserker mode!", Colors.Red));
        }

        private bool IsBerserkerCandidate(Agent agent)
        {
            return agent.Character?.StringId == "dwarf_berzerker"; // Replace with your troop ID
        }

        public HashSet<Agent> GetBerserkerAgents() => berserkerAgents;
    }
}