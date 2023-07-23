using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.HuntableHerds.Models {
    public class HerdMapNotificationItemVM : MapNotificationItemBaseVM {
        public HerdMapNotificationItemVM(HerdMapNotification data) : base(data) {
            base.NotificationIdentifier = "ransom";
            this._onInspect = () => {
                OpenHuntingMessageBox();
                base.ExecuteRemove();
            };

            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, party => {
                if (party.IsMainParty)
                    base.ExecuteRemove();
            });
        }

        public override void OnFinalize() {
            CampaignEventDispatcher.Instance.RemoveListeners(this);
        }

        private void OpenHuntingMessageBox() {
            string sceneName = PlayerEncounter.GetBattleSceneForMapPatch(Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position2D));
            bool isRandomScene = true;
            int numScenes = HerdBuildData.CurrentHerdBuildData.SceneIds.Count;
            if (numScenes > 0) {
                isRandomScene = false;
                int randomIndex = MBRandom.RandomInt(0, numScenes);
                sceneName = HerdBuildData.CurrentHerdBuildData.SceneIds[randomIndex];
            }

            InquiryData inquiry = new InquiryData(HerdBuildData.CurrentHerdBuildData.MessageTitle, HerdBuildData.CurrentHerdBuildData.Message, true, true, "Yes", "No", () => {
                CustomMissions.StartHuntingMission(sceneName, isRandomScene);
            }, null);

            InformationManager.ShowInquiry(inquiry, true, true);

        }
    }
}
