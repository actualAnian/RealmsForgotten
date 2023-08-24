using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFCustomSettlements
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            new Harmony("RFCustomScenes").PatchAll();
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            SceneBuildData.BuildAll();
        }
        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            if (starterObject is CampaignGameStarter starter)
            {
                //starter.AddBehavior(new RuinCampaignBehavior());
                starter.AddBehavior(new CustomSettlementsCampaignBehavior());
                //                starter.AddBehavior(new RFLegendaryTroopsPlayerVisitTownCampaignBehavior());
            }
        }
    }
}