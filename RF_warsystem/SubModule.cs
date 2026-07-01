using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_warsystem;

public sealed class SubModule : MBSubModuleBase
{
    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);

        if (gameStarterObject is not CampaignGameStarter campaignGameStarter)
        {
            return;
        }

        RFWarSystemRegistrar.RegisterBehaviors(campaignGameStarter);
        RFWarSystemRegistrar.Register(campaignGameStarter);
    }
}
