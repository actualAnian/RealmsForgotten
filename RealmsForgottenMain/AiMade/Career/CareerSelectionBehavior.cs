using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class CareerSelectionBehavior : CampaignBehaviorBase
    {
        private bool isCareerOffered = false;
        private string selectedCareerType = null;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            isCareerOffered = false;
            selectedCareerType = null;
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            isCareerOffered = false;
            selectedCareerType = null;
        }

        public void ApplyKnightCareer()
        {
            try
            {
                var playerHero = Hero.MainHero;
                if (playerHero == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Error: Player hero is null."));
                    return;
                }

                selectedCareerType = "Knight";
                isCareerOffered = true;
                Campaign.Current.GetCampaignBehavior<CareerProgressionBehavior>().SetCurrentCareer(selectedCareerType);
                InformationManager.DisplayMessage(new InformationMessage("You have started your journey as a Knight!"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Exception in ApplyKnightCareer: {ex.Message}"));
            }
        }

        public void ApplyMercenaryCareer()
        {
            try
            {
                var playerHero = Hero.MainHero;
                if (playerHero == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Error: Player hero is null."));
                    return;
                }

                selectedCareerType = "Mercenary";
                isCareerOffered = true;
                Campaign.Current.GetCampaignBehavior<CareerProgressionBehavior>().SetCurrentCareer(selectedCareerType);
                InformationManager.DisplayMessage(new InformationMessage("You have started your journey as a Mercenary!"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Exception in ApplyMercenaryCareer: {ex.Message}"));
            }
        }

        public string GetSelectedCareerType()
        {
            return selectedCareerType;
        }

        public void ResetCareerSelection()
        {
            selectedCareerType = null;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("isCareerOffered", ref isCareerOffered);
            dataStore.SyncData("selectedCareerType", ref selectedCareerType);
        }
    }
}