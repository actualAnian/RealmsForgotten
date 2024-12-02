using HarmonyLib;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Marching
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad() => base.OnSubModuleLoad();

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            //base.OnGameStart(game, gameStarterObject);
            //if (GlobalSettings<MarchGlobalConfig>.Instance.ArtemisSupport)
            //    new Harmony("com.marching").PatchAll();
            //if (gameStarterObject is CampaignGameStarter campaignGameStarter)
            //    campaignGameStarter.AddModel((GameModel)new MarchingAgentStatCalculateModel(((IGameStarter)campaignGameStarter).GetExistingModel<AgentStatCalculateModel>()));
            //else
            //    gameStarterObject.AddModel((GameModel)new CustomMarchingAgentStatCalculateModel(gameStarterObject.GetExistingModel<AgentStatCalculateModel>()));
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            //base.OnMissionBehaviorInitialize(mission);
            //mission.AddMissionBehavior((MissionBehavior)new MarchMissionBehavior());s
        }
    }
}
