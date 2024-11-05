using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using SandBox;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using SandBox.View.Missions;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using RealmsForgotten.Quest.KnightQuest;
using System;

namespace RealmsForgotten.Quest
{
    public static class KnightQuestDuelMission
    {
        [MissionMethod]
        public static Mission OpenDuelMission(string scene, Location location, CharacterObject knightMaester, bool duelOnHorse, Action<bool> onEnd)
        {
            return MissionState.OpenNew("RFKnightDuel", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town), (Mission mission) => new MissionBehavior[]
            {
                new KnightQuestDuelMissionController(knightMaester, duelOnHorse, onEnd),
                new MissionOptionsComponent(),
                new CampaignMissionComponent(),
                new ArenaDuelMissionBehavior(),
                new BasicLeaveMissionLogic(),
                new MissionAgentHandler(location, null, null),
                new HeroSkillHandler(),
                new MissionFacialAnimationHandler(),
                new MissionAgentPanicHandler(),
                new AgentHumanAILogic(),
                new EquipmentControllerLeaveLogic(),
                new ArenaAgentStateDeciderLogic(),

                new MissionCampaignView(),
                ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionLeaveView(),
                ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                new MissionSingleplayerViewHandler(),
                new MusicSilencedMissionView(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.3f),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                new MissionCampaignBattleSpectatorView(),
                ViewCreator.CreatePhotoModeView()

            }, true, true);
        }
    }
}







