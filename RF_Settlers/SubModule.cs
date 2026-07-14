using HarmonyLib;
using SandBox.View.Map.Visuals;
using System;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RF_Settlers
{
    public class SubModule : MBSubModuleBase
    {
        private bool _harmonyApplied;
        static Harmony harmony = new("rf.settlers");
        bool _manualPatchApplied = false;
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            if (!_harmonyApplied)
            {
                _harmonyApplied = true;
                harmony.PatchAll(typeof(SubModule).Assembly);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (gameStarterObject is CampaignGameStarter campaignGameStarter)
            {
                campaignGameStarter.AddBehavior(new SettlersCampaignBehavior());
            }
        }

        /// <summary>
        /// Runs BEFORE campaign deserialization. Settler villages are not part
        /// of any module's settlements.xml, so their definitions must be
        /// re-registered here on every load — otherwise save references to them
        /// fail to resolve. The XML for each village travels inside the save
        /// (behavior SyncData), read early through the store dictionary.
        /// </summary>
        public override void RegisterSubModuleObjects(bool isSavedCampaign)
        {
            if (!isSavedCampaign || MBObjectManager.Instance == null || Campaign.Current == null)
            {
                return;
            }

            SettlersCampaignBehavior behavior = SettlersCampaignBehavior.Instance;
            if (behavior == null)
            {
                return;
            }

            behavior.LoadEarlySync(CampaignStoreAccess.GetStore(Campaign.Current, behavior));
            foreach (SettlerVillageRecord record in behavior.VillageRecords)
            {
                try
                {
                    InjectVillageXml(record);
                }
                catch (Exception exception)
                {
                    Debug.Print($"[RF_Settlers] Village XML injection failed for {record?.StringId}: {exception}");
                }
            }
        }

        private static void InjectVillageXml(SettlerVillageRecord record)
        {
            if (string.IsNullOrEmpty(record?.SettlementXml))
            {
                return;
            }

            XmlDocument document = new();
            document.LoadXml(record.SettlementXml);
            MBObjectManager.Instance.LoadXml(document);

            // A stale, unready object from a previous session blocks the fresh
            // registration — unregister and load again (Player Settlement dance).
            Settlement settlement = MBObjectManager.Instance.GetObject<Settlement>(record.StringId);
            if (settlement != null && !settlement.IsReady)
            {
                MBObjectManager.Instance.UnregisterObject(settlement);
                MBObjectManager.Instance.LoadXml(document);
            }

            Patches.SettlersMapScenePatch.RegisterVillagePrefab(record.StringId, record.PrefabId);
        }
        public override void OnGameInitializationFinished(Game game)
        {
            if (!_manualPatchApplied)
            {
                _manualPatchApplied = true;
                RunManualPatches();
            }
        }
        private static void RunManualPatches()
        {
            var original = AccessTools.Method(typeof(MobilePartyVisual), "MobilePartyVisual.AddMobileIconComponents");
            var prefix = AccessTools.Method(typeof(Patches.SettlerCampVisualPatch), nameof(Patches.SettlerCampVisualPatch.Prefix));
            harmony.Patch(original, prefix);
        }
    }
}