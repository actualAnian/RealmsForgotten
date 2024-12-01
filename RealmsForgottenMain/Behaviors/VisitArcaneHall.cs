using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Encounters;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace RealmsForgotten.Behaviors
{
    public class VisitArcaneHall : CampaignBehaviorBase
    {
        private const string TargetSettlementId = "town_EM1";  // The ID of the settlement where the menu should be added

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddGameMenus(starter);
        }

        private void AddGameMenus(CampaignGameStarter starter)
        {
            // Add a menu option only for the specific town with ID "town_EM1"
            starter.AddGameMenuOption("town", "visit_arcane_library", new TextObject("Visit the Arcane Library").ToString(),
                args => IsAtTargetSettlement(), // Show the option only if at the correct settlement
                args =>
                {
                    // When the player selects this option, switch to the Arcane Library scene
                    EnterArcaneLibraryScene();
                },
                false, 4);
        }

        // Check if the player is currently in the specified settlement
        private bool IsAtTargetSettlement()
        {
            return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.StringId == TargetSettlementId;
        }

        private void EnterArcaneLibraryScene()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.StringId == TargetSettlementId)
            {
                // Open the custom Arcane Library scene
                MissionInitializerRecord missionInitializerRecord = new MissionInitializerRecord("magic_hall_a")  // Use the correct scene name
                {
                    DoNotUseLoadingScreen = false
                };

                MissionState.OpenNew("MagicHallMission", missionInitializerRecord, (mission) =>
                {
                    return new MissionBehavior[]
                    {
                    new CampaignMissionComponent(),
                    new MissionOptionsComponent(),
                    new MissionBasicTeamLogic(),
                    new AgentHumanAILogic(),
                    new MissionBoundaryPlacer()
                    };
                }, true, true);

                InformationManager.DisplayMessage(new InformationMessage("Entering the Arcane Library."));
            }
        }

        private void SpawnNPC()
        {
            if (Mission.Current != null)
            {
                string npcId = "anorite_monastery_priest";
                CharacterObject npcCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(npcId);

                if (npcCharacter != null)
                {
                    Vec3 spawnPosition = new Vec3(117.86f, 87.88f, 4.43f);  // Adjust position as necessary
                    Vec2 spawnDirection = new Vec2(0f, 1f);  // Adjust direction as necessary

                    AgentBuildData agentBuildData = new AgentBuildData(npcCharacter)
                        .NoHorses(true)
                        .InitialPosition(spawnPosition)
                        .InitialDirection(spawnDirection);

                    Agent agent = Mission.Current.SpawnAgent(agentBuildData);

                    InformationManager.DisplayMessage(new InformationMessage("NPC " + npcId + " has been spawned in the Arcane Library."));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Failed to find NPC with ID: " + npcId));
                }
            }
        }
        public override void SyncData(IDataStore dataStore) { }
    }
}
