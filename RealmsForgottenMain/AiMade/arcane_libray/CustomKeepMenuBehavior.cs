using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Helpers;
using TaleWorlds.Engine;

namespace RealmsForgotten.AiMade.arcane_libray
{
    public class CustomKeepMenuBehavior : CampaignBehaviorBase
    {
        // Unique identifier for the town where the custom menu will appear.
        private const string TownId = "town_EM1";

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            AddCustomKeepMenu(campaignGameStarter);
        }

        private void AddCustomKeepMenu(CampaignGameStarter campaignGameStarter)
        {
            // Add a unique menu option for your secret keep
            campaignGameStarter.AddGameMenuOption(
                "town", // The main town menu
                "enter_secret_keep", // A unique identifier for your custom keep option
                "Enter the Arcane Library", // The text displayed in the menu
                condition: (args) =>
                {
                    // Ensure that the menu option only appears in town_EM1 and doesn't conflict with the existing keep
                    Settlement currentSettlement = Settlement.CurrentSettlement;
                    if (currentSettlement != null && currentSettlement.StringId == "town_EM1")
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    }
                    return false;
                },
                consequence: (args) =>
                {
                    // Load the custom keep scene when the player selects this option
                    LoadCustomKeepScene();
                },
                index: 4 // Ensure it doesn't conflict with existing options (set an appropriate index)
            );
        }

        private void LoadCustomKeepScene()
        {
            string sceneName = "arcane_keep_a"; // Your custom scene ID

            // Restart player encounter (helps when loading into a mission related to settlements)
            PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, Settlement.CurrentSettlement.Party, false);

            // Initialize the mission
            MissionInitializerRecord missionInitializerRecord = new MissionInitializerRecord(sceneName)
            {
                DoNotUseLoadingScreen = false,
                SceneUpgradeLevel = 0,
                PlayingInCampaignMode = true
            };

            // Open the custom mission with behaviors
            MissionState.OpenNew(sceneName, missionInitializerRecord, (Mission mission) =>
            {
                List<MissionBehavior> missionBehaviors = new List<MissionBehavior>
        {
            new CustomMissionLogic(), // Your custom mission logic goes here
        };

                // Now spawn the player agent in the custom scene
                SpawnPlayerAgent(mission);

                return missionBehaviors;
            }, false, true);
        }
        private void SpawnPlayerAgent(Mission mission)
        {
            // Find the spawn point in the scene tagged as "sp_player"
            GameEntity playerSpawnEntity = mission.Scene.FindEntityWithTag("sp_player");

            // If the spawn point is found, use its position and rotation
            if (playerSpawnEntity != null)
            {
                Vec3 spawnPosition = playerSpawnEntity.GetGlobalFrame().origin;
                Vec2 spawnDirection = playerSpawnEntity.GetGlobalFrame().rotation.f.AsVec2; // Get the forward direction as a Vec2

                // Ensure equipment is available, else assign default or none
                Equipment playerEquipment = Hero.MainHero?.BattleEquipment ?? new Equipment();

                // Set up the player's agent data using the player character
                AgentBuildData agentData = new AgentBuildData(CharacterObject.PlayerCharacter)
                    .InitialPosition(spawnPosition) // Set the spawn position
                    .InitialDirection(spawnDirection) // Set the spawn direction based on the spawn point
                    .NoHorses(true) // Ensure the player spawns without a horse
                    .Controller(Agent.ControllerType.Player) // Ensures the player controls this agent
                    .Equipment(playerEquipment); // Use the player's current equipment or fallback

                // Spawn the player agent in the mission
                Agent playerAgent = mission.SpawnAgent(agentData);

                // Make the player control this agent
                mission.MainAgent = playerAgent;
            }
            else
            {
                // Log or handle the case where no player spawn point was found
                InformationManager.DisplayMessage(new InformationMessage("No player spawn point found in the scene."));
            }
        }


        public class CustomMissionLogic : MissionLogic
        {
            // This method is called every mission tick (frame update) while the mission is active.
            public override void OnMissionTick(float dt)
            {
                // Custom logic that you want to run on every mission tick (e.g., updates, checks, etc.)
                base.OnMissionTick(dt); // Call the base method if necessary

                // Example: Checking if the player agent is still alive
                Agent playerAgent = Mission.MainAgent;
                if (playerAgent != null && playerAgent.IsActive())
                {
                    // Add custom behavior, for example:
                    // Check player status, update UI, or interact with the environment
                }
            }

            // This method is called when the mission starts
            public override void AfterStart()
            {
                base.AfterStart();

                // Custom initialization logic
                // For example, spawn custom troops or set up initial game state
                InitializeCustomMissionElements();
            }

            // This method can be used to add specific mission initialization logic
            private void InitializeCustomMissionElements()
            {
                // Example: Spawning some NPCs or setting up the environment
                Vec3 spawnPosition = new Vec3(59.43f, 57.38f, -0.50f); // Define where to spawn

                // Create the agent data without assigning a team
                AgentBuildData agentData = new AgentBuildData(CharacterObject.PlayerCharacter)
                    .InitialPosition(spawnPosition);

                // Spawn the agent
                Agent agent = Mission.SpawnAgent(agentData);

                // Additional custom mission initialization logic can go here
            }


            public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
            {
                base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

                if (affectedAgent.IsMainAgent)
                {
                   Mission.EndMission();
                }
            }
        }
    }
}
