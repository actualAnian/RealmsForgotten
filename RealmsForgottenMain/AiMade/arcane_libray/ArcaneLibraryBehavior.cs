
using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Encounters;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Source.Missions;
using SandBox.Conversation.MissionLogics;
using RealmsForgotten.AiMade.ArcaneLibrary;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace RealmsForgotten.AiMade.arcane_library
{
    public class ArcaneLibraryCampaignBehavior : CampaignBehaviorBase
    {
        private const string TargetSettlementId = "town_EM1";  // The ID of the settlement where the menu should be added

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            Initialize(campaignGameStarter);
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            Initialize(campaignGameStarter);
        }

        private void Initialize(CampaignGameStarter campaignGameStarter)
        {
            AddGameMenus(campaignGameStarter);
            InformationManager.DisplayMessage(new InformationMessage("Arcane Library behavior initialized successfully.", Colors.Green));
        }

        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("town", "visit_arcane_library", "Visit the Arcane Library",
                args => IsAtTargetSettlement(), VisitArcaneLibraryConsequence, false, 4, false);
        }

        private bool IsAtTargetSettlement()
        {
            return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.StringId == TargetSettlementId;
        }

        private void VisitArcaneLibraryConsequence(MenuCallbackArgs args)
        {
            EnterArcaneLibraryScene();
        }

        private void EnterArcaneLibraryScene()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.StringId == TargetSettlementId)
            {
                MissionInitializerRecord missionInitializerRecord = new MissionInitializerRecord("arcane_keep_a")
                {
                    DoNotUseLoadingScreen = false,
                    SceneLevels = "sp_player"
                };

                MissionState.OpenNew("ArcaneLibraryMission", missionInitializerRecord, mission =>
                {
                    return new MissionBehavior[]
                    {
                        new CampaignMissionComponent(),
                        new MissionBasicTeamLogic(),
                        new MissionSettlementPrepareLogic(),
                        new MissionBoundaryPlacer(),
                        new MissionAgentLookHandler(),
                        new AgentHumanAILogic(),
                        new MissionBoundaryCrossingHandler(),
                        new MissionFacialAnimationHandler(),
                        new MissionFightHandler(),
                        new VisualTrackerMissionBehavior(),
                        new EquipmentControllerLeaveLogic(),
                        new MissionOptionsComponent(),
                        new ArcaneLibraryMissionBehavior()  // Custom behavior to handle player and NPC spawning and dialogs
                    };
                }, true, true);

                InformationManager.DisplayMessage(new InformationMessage("Entering the Arcane Library."));
            }
        }
    }
}