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
        public static bool berserkerModeActive = false;
        private float initialHealth;
        private Timer berserkerTimer;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public CustomBerserkerBehavior()
        {
            berserkerTimer = new Timer(Time.ApplicationTime, 0f, false);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            Agent mainAgent = Agent.Main; // Change to the agent you want to monitor

            // Debugging: Print agent health status
            InformationManager.DisplayMessage(new InformationMessage($"Agent Health: {mainAgent?.Health}"));

            if (mainAgent == null || !IsCustomTroop(mainAgent))
                return;

            // Initialize initial health if not already done
            if (!berserkerModeActive && initialHealth == 0)
            {
                initialHealth = mainAgent.Health;
                InformationManager.DisplayMessage(new InformationMessage("Initial health recorded."));
            }

            // Check if the agent has lost 1/3 of its HP
            if (!berserkerModeActive && mainAgent.Health < initialHealth * (2f / 3f))
            {
                InformationManager.DisplayMessage(new InformationMessage("Berserker mode condition met!"));
                ActivateBerserkerMode(mainAgent);
            }

            // Disable berserker mode after some time
            if (berserkerModeActive && berserkerTimer.Check(Time.ApplicationTime))
            {
                DeactivateBerserkerMode(mainAgent);
            }
        }

        private void ActivateBerserkerMode(Agent agent)
        {
            berserkerModeActive = true;
            berserkerTimer.Reset(Time.ApplicationTime, 45f); // Berserker effect lasts for 45 seconds

            // Print message when berserker mode is activated
            var msg = new TextObject("{=berserker_activated}Berserker mode activated!");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Color.FromUint(0xFFFF0000)));

            // Debugging: Confirm agent health when berserker mode is triggered
            InformationManager.DisplayMessage(new InformationMessage($"Berserker mode triggered at health: {agent.Health}"));
        }

        private void DeactivateBerserkerMode(Agent agent)
        {
            berserkerModeActive = false;

            // Print message when berserker mode is deactivated
            var msg = new TextObject("{=berserker_deactivated}Berserker mode deactivated!");
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Color.FromUint(0xFFFF0000)));
        }

        private bool IsCustomTroop(Agent agent)
        {
            // Debugging: Check if the agent is the custom troop
            bool isCustom = agent.Character?.StringId == "dwarf_berzerker"; // Replace with your troop ID
            InformationManager.DisplayMessage(new InformationMessage($"Is Custom Troop: {isCustom}"));
            return isCustom;
        }
    }
}