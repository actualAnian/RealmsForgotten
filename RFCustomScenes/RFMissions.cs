using RFCustomSettlements;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.View;
using SandBox.View.Missions;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using static RFCustomSettlements.ArenaBuildData;

namespace RealmsForgotten.RFCustomSettlements
{
    public static class RFMissions
    {
            [MissionMethod]
            public static Mission StartExploreMission(string sceneName, CustomSettlementBuildData currentBuildData, Action? onBattleEnd = null)
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
                    new MissionConversationCameraView(),
                    new RFConversationLogic(),
                    ViewCreator.CreateMissionLeaveView(),
                    ViewCreator.CreateMissionBoundaryCrossingView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreatePhotoModeView(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),

                    new CustomSettlementMissionLogic(currentBuildData, onBattleEnd),
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
        public static Mission OpenArenaMission(string scene, StageData stageData, Action<bool> onBattleEnd)
        {
            return MissionState.OpenNew("ArenaFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town),
                    (Mission mission) => new MissionBehavior[]
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
