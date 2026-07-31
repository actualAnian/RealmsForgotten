using RealmsForgotten.HuntableHerds.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.HuntableHerds
{
    public class HerdSpottingBehavior : CampaignBehaviorBase {
        public override void RegisterEvents() {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);

        }

        public override void SyncData(IDataStore dataStore) {
            //
        }

        // Water terrains the player crosses only by ship — no land herds there.
        private static bool IsWaterTerrain(TerrainType terrain) {
            return terrain == TerrainType.Water
                || terrain == TerrainType.Lake
                || terrain == TerrainType.River
                || terrain == TerrainType.CoastalSea
                || terrain == TerrainType.OpenSea;
        }

        private void OnDailyTickParty(MobileParty party) {
            if (!party.IsMainParty || party.CurrentSettlement != null)
                return;

            // Only spot herds while genuinely travelling the land campaign map:
            // not aboard a ship / at sea, not mid-encounter or battle, not parked
            // in a menu (settlement is already covered above).
            if (party.IsCurrentlyAtSea
                || party.MapEvent != null
                || party.BesiegerCamp != null
                || party.Army != null && party.Army.LeaderParty != party
                || PlayerEncounter.Current != null
                || Campaign.Current.CurrentMenuContext != null)
                return;

            TerrainType terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
            if (IsWaterTerrain(terrain))
                return;

            if (MBRandom.RandomFloat <= Settings.Instance.DailyChanceOfSpottingHerd)
                ShowHuntingHerdNotification(terrain);
        }

        private void ShowHuntingHerdNotification(TerrainType terrain) {
            // Pick a herd that fits this biome; suppress if nothing lives here.
            if (!HerdBuildData.RandomizeForTerrain(terrain) || HerdBuildData.CurrentHerdBuildData == null)
                return;
            Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new HerdMapNotification(new TextObject(HerdBuildData.CurrentHerdBuildData.NotifMessage)));
        }
    }

    public class CustomSaveDefiner : SaveableTypeDefiner {
        public CustomSaveDefiner() : base(877885323) { }

        protected override void DefineClassTypes() {
            AddClassDefinition(typeof(HerdMapNotification), 1);
            AddClassDefinition(typeof(HerdMapNotificationItemVM), 2);
        }

        protected override void DefineContainerDefinitions() {
            //
        }
    }
}
