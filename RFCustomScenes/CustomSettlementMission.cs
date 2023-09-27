using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.View;
using SandBox.View.Missions;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;

namespace RealmsForgotten.RFCustomSettlements
{
    public static class CustomSettlementMission
    {
            [MissionMethod]
            public static Mission StartCustomSettlementMission(string sceneName, CustomSettlementBuildData currentBuildData)
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
            //  new MissionSettlementPrepareLogic(),
                    new SandBoxMissionHandler(),
                    new MissionFightHandler(),
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

                    SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission)
                    }, true, true);
            }
        }

}
