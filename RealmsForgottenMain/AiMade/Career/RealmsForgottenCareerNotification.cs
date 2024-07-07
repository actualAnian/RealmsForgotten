using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Career
{
    public class RealmsForgottenCampaignBehavior : CampaignBehaviorBase
    {
        private bool hasDisplayedCareerNotification = false;

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("hasDisplayedCareerNotification", ref hasDisplayedCareerNotification);
        }

        private void OnGameLoadFinished()
        {
            if (Hero.MainHero != null && !hasDisplayedCareerNotification)
            {
                DisplayCareerNotification();
                hasDisplayedCareerNotification = true;
            }
        }

        private void DisplayCareerNotification()
        {
            var careerSelectionBehavior = Campaign.Current.GetCampaignBehavior<CareerSelectionBehavior>();
            if (careerSelectionBehavior != null)
            {
                string careerType = careerSelectionBehavior.GetSelectedCareerType();
                if (!string.IsNullOrEmpty(careerType))
                {
                    TextObject notificationText = new TextObject("Your adventure begins!");
                    switch (careerType)
                    {
                        case "Mercenary":
                            notificationText = new TextObject("You have started as a Mercenary. Forge your path through contracts and battles.");
                            break;
                        case "Knight":
                            notificationText = new TextObject("You have started as a Knight. Serve your lord and manage your fief.");
                            break;
                            // Add more cases for other careers as needed
                    }
                    MBInformationManager.AddQuickInformation(notificationText, 0, null, "event:/ui/notification/relation");
                }
            }
        }
    }
}


