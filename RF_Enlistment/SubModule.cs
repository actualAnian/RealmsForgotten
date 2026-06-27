using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_Enlistment;

public sealed class SubModule : MBSubModuleBase
{
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();

        RFEnlistmentBootstrap.Initialize();
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);

        if (gameStarterObject is CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddBehavior(new RFEnlistmentCampaignBehavior());
        }
    }

    public override void OnMissionBehaviorInitialize(Mission mission)
    {
        base.OnMissionBehaviorInitialize(mission);

        if (Campaign.Current == null || mission == null)
        {
            return;
        }

        if (mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.Stealth)
        {
            mission.AddMissionBehavior(new RFEnlistmentMissionBehavior());
        }
    }
}
