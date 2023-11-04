using HarmonyLib;
using RFCustomSettlements;
using RFCustomSettlements.Dialogues;
using RFCustomSettlements.Patches;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

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
            DialogueParser.Deserialize();
        }
        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
        }
        public override void OnAfterGameInitializationFinished(Game game, object obj)
        {
            foreach (Settlement settlement in CustomSettlementsCampaignBehavior.customSettlements)
            {
                RFCustomSettlement settlementComponent = (RFCustomSettlement)settlement.SettlementComponent;
                if (settlementComponent.StateHandler is ArenaSettlementStateHandler handler)
                {
                    ArenaBuildData buildData = ArenaBuildData.BuildArenaData();
                    ArenaSettlementStateHandler arenaHandler = handler;
                    arenaHandler.BuildData = buildData;
                    arenaHandler.SyncData(ArenaCampaignBehavior.currentArenaState, ArenaCampaignBehavior.currentChallengeToSync, ArenaCampaignBehavior.isWaiting);
                }

            }
            base.OnAfterGameInitializationFinished(game, obj);
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