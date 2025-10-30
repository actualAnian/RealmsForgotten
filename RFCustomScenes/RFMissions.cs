using BehaviorTreeWrapper;
using RealmsForgotten.RFCustomSettlements;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.ViewModelCollection;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;
using static RFCustomSettlements.ArenaBuildData;

namespace RFCustomSettlements
{
    public static class RFMissions
    {
        [MissionMethod]
        public static Mission StartExploreMission(string sceneName, CustomSettlementBuildData currentBuildData, Action? onBattleEnd = null)
        {
            return MissionState.OpenNew(sceneName,
                SandBoxMissions.CreateSandBoxMissionInitializerRecord(sceneName, "", false, DecalAtlasGroup.Battle),
                (mission) => new MissionBehavior[] {
                new MissionOptionsComponent(),
                new CampaignMissionComponent(),
                new MissionBasicTeamLogic(),
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
                new MissionConversationCameraView(),
                new MissionBoundaryWallView(),
                new MissionCampaignView(),
                new OrderTroopPlacer(null),

                new CustomSettlementMissionLogic(currentBuildData, sceneName, onBattleEnd),
                new RFConversationLogic(),        
                new BehaviorTreeMissionLogic(),

                ViewCreator.CreateMissionLeaveView(),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreatePhotoModeView(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionAgentLockVisualizerView(),
                ViewCreator.CreateMissionMainAgentEquipmentController(),
                ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
                ViewCreator.CreateMissionOrderUIHandler(),
                SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
                SandBoxViewCreator.CreateMissionConversationView(mission),
    			SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
                // to check
                // new StealthAreaMissionLogic()
                // VisualTrackerMissionBehavior
            }, true, true);
        }
        [MissionMethod]
        public static Mission OpenArenaMission(string scene, StageData stageData, Action<bool> onBattleEnd)
        {
            return MissionState.OpenNew("ArenaFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town),
                    (mission) => new MissionBehavior[]
             {
                    new ArenaFightMissionController(stageData, onBattleEnd), // @TODO temporary
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


                    new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.6f),


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
