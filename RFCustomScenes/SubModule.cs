using HarmonyLib;
using RFCustomSettlements;
using SandBox.CampaignBehaviors;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static RFCustomSettlements.FocusStateCheckTickPatch;

namespace RealmsForgotten.RFCustomSettlements
{
    public class SubModule : MBSubModuleBase
    {
        public static readonly Harmony harmony = new("RFCustomScenes");
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            //new Harmony("RFCustomScenes")
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
          //  if (!manualPatchesHaveFired)
            //{
             //   manualPatchesHaveFired = true;
                RunManualPatches();
           // }
        }

        private void RunManualPatches()
        {
            var original = AccessTools.Method("MissionMainAgentInteractionComponent:FocusTick");
            var aha = AccessTools.Method("HideoutConversationsCampaignBehavior:bandit_hideout_start_defender_on_condition");
           // harmony.Patch(aha, transpiler: new HarmonyMethod(typeof(HideoutConversationsCampaignBehaviorPatch), nameof(HideoutConversationsCampaignBehaviorPatch.StartOnConditionPatch)));
            harmony.Patch(original, transpiler: new HarmonyMethod(typeof(MissionMainAgentInteractionComponentFocusTickPatch), nameof(MissionMainAgentInteractionComponentFocusTickPatch.FocusTickPatch)));
        }

        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            if (starterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new CustomSettlementsCampaignBehavior());
                starter.AddBehavior(new ArenaCampaignBehavior());
                //                starter.AddBehavior(new RFLegendaryTroopsPlayerVisitTownCampaignBehavior());
            }
        }
    }
}