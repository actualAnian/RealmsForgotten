using System;
using System.Linq; // Add this directive for LINQ
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.Module1.Religions
{
    public class CeremonyQuestBehavior : CampaignBehaviorBase
    {
        private static readonly TextObject CeremonyTitleText = new TextObject("Religious Ceremony");
        private static readonly TextObject CeremonyText = new TextObject("A religious ceremony is taking place in {TARGET_TOWN}. You have 5 days to arrive and participate. Do you want to go?");
        private static readonly TextObject AcceptText = new TextObject("Attend Ceremony");
        private static readonly TextObject DeclineText = new TextObject("Ignore");

        private Settlement targetSettlement;
        private bool ceremonyQuestAccepted = false;
        private CampaignTime questDeadline;

        private bool _ceremonyCompleted = false;
        public bool CeremonyCompleted
        {
            get => _ceremonyCompleted;
            private set => _ceremonyCompleted = value;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("CeremonyQuest_targetSettlement", ref targetSettlement);
            dataStore.SyncData("CeremonyQuest_ceremonyQuestAccepted", ref ceremonyQuestAccepted);
            dataStore.SyncData("CeremonyQuest_questDeadline", ref questDeadline);

            bool tempCeremonyCompleted = CeremonyCompleted;
            dataStore.SyncData("CeremonyQuest_CeremonyCompleted", ref tempCeremonyCompleted);
            CeremonyCompleted = tempCeremonyCompleted;

            // Add logging
            InformationManager.DisplayMessage(new InformationMessage($"SyncData called for {this.GetType().Name}"));
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            // Add any initialization logic here
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            // Add any loading logic here
        }

        public void TriggerReligiousCeremonyFromDialogue()
        {
            var playerHero = Hero.MainHero;
            var playerCulture = playerHero.Culture;
            var targetTown = Town.AllTowns.Where(t => t.Culture == playerCulture).OrderBy(t => MBRandom.RandomFloat).FirstOrDefault();

            if (targetTown != null)
            {
                string eventDescription = CeremonyText.ToString().Replace("{TARGET_TOWN}", targetTown.Name.ToString());
                InformationManager.ShowInquiry(new InquiryData(
                    CeremonyTitleText.ToString(),
                    eventDescription,
                    true,
                    true,
                    AcceptText.ToString(),
                    DeclineText.ToString(),
                    () => AcceptCeremonyQuest(targetTown.Settlement),
                    null
                ));
            }
        }

        private void AcceptCeremonyQuest(Settlement settlement)
        {
            targetSettlement = settlement;
            ceremonyQuestAccepted = true;
            questDeadline = CampaignTime.DaysFromNow(7);
            InformationManager.DisplayMessage(new InformationMessage($"You need to travel to {settlement.Name} within 5 days to attend the ceremony.", Colors.Green));
        }

        private void CompleteCeremony()
        {
            ceremonyQuestAccepted = false;
            CeremonyCompleted = true;
            PietyManager.AddPiety(Hero.MainHero, 20, true);
            InformationManager.ShowInquiry(new InquiryData(
                "Ceremony Attended",
                $"{Hero.MainHero.Name} successfully attended the ceremony and gained 20 piety.",
                true,
                false,
                "OK",
                null,
                null,
                null
            ));
        }

        private void HourlyTick()
        {
            if (!ceremonyQuestAccepted || targetSettlement == null)
                return;

            if (CampaignTime.Now > questDeadline)
            {
                ceremonyQuestAccepted = false;
                InformationManager.DisplayMessage(new InformationMessage("You failed to attend the event in time.", Colors.Red));
                return;
            }

            if (MobileParty.MainParty.CurrentSettlement == targetSettlement)
            {
                CompleteCeremony();
            }
        }
    }
}