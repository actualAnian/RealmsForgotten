using HuntableHerds.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace HuntableHerds {
    public class HerdSpottingBehavior : CampaignBehaviorBase {
        public override void RegisterEvents() {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);

        }

        public override void SyncData(IDataStore dataStore) {
            //
        }

        private void OnDailyTickParty(MobileParty party) {
            if (!party.IsMainParty || party.CurrentSettlement != null)
                return;

            if(MBRandom.RandomFloat <= Settings.Instance.DailyChanceOfSpottingHerd)
                ShowHuntingHerdNotification();
        }

        private void ShowHuntingHerdNotification() {
            HerdBuildData.Randomize();
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
