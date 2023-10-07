﻿using RFCustomSettlements;
using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Tournaments.MissionLogics;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.View.Missions.Sound.Components;
using SandBox.ViewModelCollection;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;

namespace RealmsForgotten.RFCustomSettlements
{
    public static class RFMissions
    {
            [MissionMethod]
            public static Mission StartExploreMission(string sceneName, CustomSettlementBuildData currentBuildData)
                {
                return MissionState.OpenNew(sceneName,
                    SandBoxMissions.CreateSandBoxMissionInitializerRecord(sceneName, "", false, DecalAtlasGroup.Battle),
                    (Mission mission) => new MissionBehavior[] {
                    new MissionOptionsComponent(),
                    new CampaignMissionComponent(),
                    new MissionBasicTeamLogic(),
                    new BasicLeaveMissionLogic(),
                    new MissionAgentLookHandler(),
                    new HeroSkillHandler(),
                    new MissionFacialAnimationHandler(),
                    new BattleAgentLogic(),
                    new MountAgentLogic(),
                    new AgentHumanAILogic(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new EquipmentControllerLeaveLogic(),
                    new HighlightsController(),

                    new MissionSingleplayerViewHandler(),
                    new MissionItemContourControllerView(),
                    new MissionAgentContourControllerView(),
                    new SandBoxMissionHandler(),
                    new MissionFightHandler(),

                    new RFConversationLogic(),
                    ViewCreator.CreateMissionLeaveView(),
                    ViewCreator.CreateMissionBoundaryCrossingView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreatePhotoModeView(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),

                    new CustomSettlementMissionLogic(currentBuildData),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    new MissionCampaignView(),
                    ViewCreator.CreateMissionAgentLockVisualizerView(),
                    new MissionBoundaryWallView(),

                    ViewCreator.CreateMissionMainAgentEquipmentController(),

                    new OrderTroopPlacer(),
                    ViewCreator.CreateMissionOrderUIHandler(null),
                    ViewCreator.CreateMissionFormationMarkerUIHandler(mission),

                    SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
                    SandBoxViewCreator.CreateMissionConversationView(mission)
                    }, true, true);
            }
        [MissionMethod]
        public static Mission OpenArenaMission(string scene)
        {
            return MissionState.OpenNew("ArenaFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town),
                    (Mission mission) => new MissionBehavior[]
            {
                    new ArenaFightMissionController(),
                
                    ViewCreator.CreateMissionLeaveView(),
                    new BasicLeaveMissionLogic(),
                    new CampaignMissionComponent(),
                    new EquipmentControllerLeaveLogic(),
                    new AgentVictoryLogic(),
                    new MissionAgentPanicHandler(),
                    new AgentHumanAILogic(),
                    new ArenaAgentStateDeciderLogic(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionOptionsComponent(),
                    new HighlightsController(),
                    new SandboxHighlightsController(),


                    ViewCreator.CreateMissionAgentLabelUIHandler(mission),

                    ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreatePhotoModeView(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(),
                    ViewCreator.CreateMissionMainAgentEquipmentController(),

            }, true, true);
        }
    }

}
