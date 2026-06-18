//using SandBox;
//using SandBox.Missions.MissionLogics;
//using SandBox.View.Missions;
//using SandBox.ViewModelCollection;
//using System;
//using TaleWorlds.CampaignSystem;
//using TaleWorlds.CampaignSystem.MapEvents;
//using TaleWorlds.CampaignSystem.Party;
//using TaleWorlds.CampaignSystem.TroopSuppliers;
//using TaleWorlds.Core;
//using TaleWorlds.Engine;
//using TaleWorlds.MountAndBlade;
//using TaleWorlds.MountAndBlade.Source.Missions;
//using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
//using TaleWorlds.MountAndBlade.View;
//using TaleWorlds.MountAndBlade.View.MissionViews;
//using TaleWorlds.MountAndBlade.View.MissionViews.Order;
//using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
//using TaleWorlds.MountAndBlade.View.MissionViews.Sound;
//using static TaleWorlds.MountAndBlade.Mission;

//namespace RealmsForgotten
//{
//    public static class PlayerSoloMission
//    {
//        [MissionMethod]
//        public static Mission OpenPlayerSoloMission(string scene, bool isPlayerAttacker)
//        {
//            var rec = SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Battle);
//            Mission mission2 = MissionState.OpenNew("Battle", rec, (mission) =>
//            {
//                MissionView missionView = ViewCreator.CreateMissionOrderUIHandler(null);
//                ISiegeDeploymentView @object = missionView as ISiegeDeploymentView;

//                return new MissionBehavior[]
//                {
//                new MissionAgentSpawnLogic(new IMissionTroopSupplier[]
//                {
//                    new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, null, null),
//                    new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker, null, null)
//                }, PartyBase.MainParty.Side, BattleSizeType.Battle),
//                new BattlePowerCalculationLogic(),
//                new BattleSpawnLogic("battle_set"),
//                new SandBoxBattleMissionSpawnHandler(),
//                new CampaignMissionComponent(),
//                new BattleAgentLogic(),
//                new MountAgentLogic(),
//                new BannerBearerLogic(),
//                new MissionOptionsComponent(),
//                new BattleEndLogic(),
//                new BattleReinforcementsSpawnController(),
//                new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.FieldBattle, false),
//                new BattleObserverMissionLogic(),
//                new AgentHumanAILogic(),
//                new AgentVictoryLogic(),
//                new BattleSurgeonLogic(),
//                new MissionAgentPanicHandler(),
//                new BattleMissionAgentInteractionLogic(),
//                new AgentMoraleInteractionLogic(),
//                new AssignPlayerRoleInTeamMissionController(true, false, false),
//                new EquipmentControllerLeaveLogic(),
//                new MissionHardBorderPlacer(),
//                new MissionBoundaryPlacer(),
//                new MissionBoundaryCrossingHandler(10f),
//                new HighlightsController(),
//                new BattleHighlightsController(),
//                new BattleDeploymentMissionController(isPlayerAttacker),
//                //new BattleDeploymentHandler(isPlayerAttacker),
//                new MissionCampaignView(),
//                missionView,
//                ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
//                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
//                ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
//                ViewCreator.CreateOptionsUIHandler(),
//                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
//                new OrderTroopPlacer(null),
//                new MissionSingleplayerViewHandler(),
//                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
//                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
//                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
//                ViewCreator.CreateMissionAgentLockVisualizerView(mission),
//                new MusicBattleMissionView(false),
//                new DeploymentMissionView(),
//                new MissionDeploymentBoundaryMarker("swallowtail_banner", 2f),
//                ViewCreator.CreateMissionBoundaryCrossingView(),
//                new MissionBoundaryWallView(),
//                ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
//                new MissionFormationTargetSelectionHandler(),
//                ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
//                ViewCreator.CreateMissionSpectatorControlView(mission),
//                new MissionItemContourControllerView(),
//                new MissionAgentContourControllerView(),
//                new MissionPreloadView(),
//                new MissionCampaignBattleSpectatorView(),
//                ViewCreator.CreatePhotoModeView(),
//                new MissionFaceCacheView(),
//                new MissionEntitySelectionUIHandler(new Action<WeakGameEntity>(@object.OnEntitySelection), new Action<WeakGameEntity>(@object.OnEntityHover)),
//                ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, new SPOrderOfBattleVM())
//                    };
//            }, true, true);
//            return mission2;
//        }
//    }
//}
