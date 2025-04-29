using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using RealmsForgotten.AiMade.AIQuest;


namespace RealmsForgotten.AiMade
{
    public class MerchantDeliveryBehavior : CampaignBehaviorBase
    {
        private CampaignTime _nextTriggerTime;
        private const int DaysBetweenEvents = 60;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("MerchantDelivery_NextTriggerTime", ref _nextTriggerTime);
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            // Start merchant event after ~90 days plus a small random variation
            _nextTriggerTime = CampaignTime.Now + CampaignTime.Days(DaysBetweenEvents + MBRandom.RandomInt(5, 15));
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            if (_nextTriggerTime == null || _nextTriggerTime == CampaignTime.Zero)
                _nextTriggerTime = CampaignTime.Now + CampaignTime.Days(DaysBetweenEvents);
        }

        private void DailyTick()
        {
            if (CampaignTime.Now >= _nextTriggerTime)
            {
                TryOfferMerchantQuest();
                _nextTriggerTime = CampaignTime.Now + CampaignTime.Days(DaysBetweenEvents);
            }
        }

        private void TryOfferMerchantQuest()
        {
            var towns = Settlement.All.Where(x => x.IsTown).ToList();
            if (towns.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ No towns found for merchant quest!", Colors.Red));
                return;
            }
            var town = towns[MBRandom.RandomInt(towns.Count)];

            TextObject title = new TextObject("{=MerchantTitle}A Distressed Merchant");
            TextObject description = new TextObject("{=MerchantText}You meet a distressed merchant on the road with a broken wagon. He says: \"I need these goods delivered to {TOWN_NAME}. Will you help me?\"");
            description.SetTextVariable("TOWN_NAME", town.Name);

            InquiryData inquiry = new InquiryData(
                title.ToString(),
                description.ToString(),
                true,
                true,
                new TextObject("{=Accept}Accept and help").ToString(),
                new TextObject("{=Decline}Decline and move on").ToString(),
                () => StartMerchantQuest(town),
                () => InformationManager.DisplayMessage(new InformationMessage("You declined to help the merchant.", Colors.Red))
            );

            InformationManager.ShowInquiry(inquiry, true, false);
        }

        private void StartMerchantQuest(Settlement destination)
        {
            string questId = "merchant_delivery_" + MBRandom.RandomInt(100000, 999999);
            var quest = new MerchantDeliveryQuest(questId, Hero.MainHero, CampaignTime.Days(7), destination);
            quest.StartQuest();
        }
    }
}