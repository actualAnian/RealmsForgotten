using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.HuntableHerds.Models
{
    public class HerdMapNotificationItemVM : MapNotificationItemBaseVM
    {
        /// <summary>Herd captured when the notice was created. Never read from the global static.</summary>
        private readonly HerdBuildData? _herd;

        private readonly string _title;
        private readonly string _body;

        public HerdMapNotificationItemVM(HerdMapNotification data) : base(data)
        {
            base.NotificationIdentifier = "ransom";

            _herd = data.Herd;
            // Title/body variants are rolled once, here, so the inquiry matches the notice text.
            _title = _herd != null ? _herd.MessageTitle : "Herd Spotted";
            _body = _herd != null ? _herd.Message : "Your scouts spotted the tracks of wild beasts. Do you pursue a hunt?";

            this._onInspect = () =>
            {
                OpenHuntingMessageBox();
                base.ExecuteRemove();
            };

            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, party =>
            {
                if (party.IsMainParty)
                    base.ExecuteRemove();
            });
        }

        public override void OnFinalize()
        {
            CampaignEventDispatcher.Instance.RemoveListeners(this);
        }

        private void OpenHuntingMessageBox()
        {
            try
            {
                HerdBuildData? herd = _herd ?? HerdBuildData.PickRandom(null);
                if (herd == null)
                {
                    SubModule.PrintDebugMessage("HuntableHerds: no herd data available, hunting_herds.xml may be missing.", 255, 0, 0);
                    return;
                }

                string sceneName = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(
                    Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position), false);
                bool isRandomScene = true;
                int numScenes = herd.SceneIds.Count;
                if (numScenes > 0)
                {
                    isRandomScene = false;
                    sceneName = herd.SceneIds[MBRandom.RandomInt(0, numScenes)];
                }

                InquiryData inquiry = new InquiryData(_title, _body, true, true, "Yes", "No", () =>
                {
                    try
                    {
                        // The static only ever becomes "the herd of the current hunt" HERE, at accept time.
                        HerdBuildData.CurrentHerdBuildData = herd;
                        HerdSpottingBehavior.NotifyHuntAccepted();
                        CustomMissions.StartHuntingMission(sceneName, isRandomScene, herd);
                    }
                    catch (Exception e)
                    {
                        SubModule.PrintDebugMessage($"HuntableHerds: failed to start the hunting mission ({e.Message})", 255, 0, 0);
                    }
                }, null);

                InformationManager.ShowInquiry(inquiry, true, true);
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: failed to open the hunt inquiry ({e.Message})", 255, 0, 0);
            }
        }
    }
}
