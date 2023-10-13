using HarmonyLib;
using RFCustomSettlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static RFCustomSettlements.FocusStateCheckTickPatch;

namespace RealmsForgotten.RFCustomSettlements
{
    public class SubModule : MBSubModuleBase
    {
        private bool manualPatchesHaveFired = false;
        public static readonly Harmony harmony = new("RFCustomScenes");
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            harmony.PatchAll();
            CustomSettlementBuildData.BuildAll();
        }
        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
        }
        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            if (!manualPatchesHaveFired)
            {
                manualPatchesHaveFired = true;
                RunManualPatches();
            }
        }

        private void RunManualPatches()
        {
            var original = AccessTools.Method("MissionMainAgentInteractionComponent:FocusTick");
            harmony.Patch(original, transpiler: new HarmonyMethod(typeof(MissionMainAgentInteractionComponentFocusTickPatch), nameof(MissionMainAgentInteractionComponentFocusTickPatch.FocusTickPatch)));
        }

        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            if (starterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new CustomSettlementsCampaignBehavior());
                starter.AddBehavior(new ArenaCampaignBehavior());
            }
        }
    }
}