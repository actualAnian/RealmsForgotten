using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFLegendaryTroops
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }


        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            if (starterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new RFLegendaryTroopsPlayerVisitTownCampaignBehavior());
                starter.AddBehavior(new RFLegendaryTroopsNotableBehaviors());
                starter.AddBehavior(new RFLegendaryTroopsAIRecruitment());
            }
        }
    }

}