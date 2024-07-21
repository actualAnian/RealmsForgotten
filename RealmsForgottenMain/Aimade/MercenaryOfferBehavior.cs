using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    public class MercenaryOfferBehavior : CampaignBehaviorBase
    {
        private static readonly TextObject JoinWarDecisionTitleText = new TextObject("{=JoinWarDecisionTitle}Join the War");
        private static readonly TextObject JoinWarDecisionText = new TextObject("{=JoinWarDecisionText}Khalik, the Dragon, has invited you to join the war against his enemies as a mercenary. Do you accept?");
        private static readonly TextObject AcceptText = new TextObject("{=Accept}Accept");
        private static readonly TextObject DeclineText = new TextObject("{=Decline}Decline");

        private Hero _lord3_1;
        private Kingdom _lordKingdom;
        private bool _hasAcceptedOffer; // Flag to track if the offer has been accepted
        private bool _wasAtPeace; // Flag to track if the kingdom was at peace after the last offer was declined

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lord3_1", ref _lord3_1);
            dataStore.SyncData("_lordKingdom", ref _lordKingdom);
            dataStore.SyncData("_hasAcceptedOffer", ref _hasAcceptedOffer);
            dataStore.SyncData("_wasAtPeace", ref _wasAtPeace);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
        }

        private void Initialize()
        {
            _lord3_1 = Hero.FindFirst(hero => hero.StringId == "lord_3_1");
            _lordKingdom = _lord3_1?.Clan?.Kingdom;
            if (_lord3_1 == null || _lordKingdom == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Initialization failed: lord_3_1 or his kingdom is null"));
            }
            else
            {
                _wasAtPeace = !IsKingdomAtWar(_lordKingdom);
            }
        }

        private void DailyTick()
        {
            if (_lord3_1 == null || _lordKingdom == null || _hasAcceptedOffer)
                return;

            if (_wasAtPeace && IsKingdomAtWar(_lordKingdom))
            {
                CreateJoinWarDecisionPopUp();
            }

            _wasAtPeace = !IsKingdomAtWar(_lordKingdom);
        }

        private bool IsKingdomAtWar(Kingdom kingdom)
        {
            return kingdom.Stances.Any(stance => stance.IsAtWar);
        }

        private void CreateJoinWarDecisionPopUp()
        {
            InformationManager.ShowInquiry(new InquiryData(
                JoinWarDecisionTitleText.ToString(),
                JoinWarDecisionText.ToString(),
                true,
                true,
                AcceptText.ToString(),
                DeclineText.ToString(),
                OnAccept,
                OnDecline
            ));
        }

        private void OnAccept()
        {
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Clan.PlayerClan, _lordKingdom, 0);
            _hasAcceptedOffer = true;
            InformationManager.DisplayMessage(new InformationMessage("You have joined the war as a mercenary."));
        }

        private void OnDecline()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have declined the offer to join the war."));
        }
    }
}
