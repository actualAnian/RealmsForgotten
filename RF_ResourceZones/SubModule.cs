using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_ResourceZones
{
    /// <summary>
    /// Entry point of the resource-zones system (PLANO_RESOURCE_ZONES.md).
    /// Deliberately patch-free: the tent map icon and the menu encounter come
    /// from RF_Settlers' IRFStationaryCampParty patches (applied there, the
    /// MobilePartyVisual one LATE — folded-character discipline).
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (gameStarterObject is CampaignGameStarter campaignGameStarter)
            {
                campaignGameStarter.AddBehavior(new ResourceZonesCampaignBehavior());
                campaignGameStarter.AddBehavior(new ResourceZoneAmbitionBehavior());
            }
        }
    }
}
