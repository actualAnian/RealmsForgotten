using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    public class MercenaryOfferBehavior : CampaignBehaviorBase
    {
        private static readonly TextObject JoinWarDecisionTitleText =
            new TextObject("{=JoinWarDecisionTitle}Join the War");

        private static readonly TextObject JoinWarDecisionText =
            new TextObject("{=JoinWarDecisionText}Khalik, the Dragon, has invited you to join the war against his enemies as a mercenary. Do you accept?");

        private static readonly TextObject AcceptText = new TextObject("{=Accept}Accept");
        private static readonly TextObject DeclineText = new TextObject("{=Decline}Decline");

        // --- CONFIG ---
        private const float OfferEarliestDay = 30f;

        private Hero _lord3_1;
        private Kingdom _lordKingdom;

        private bool _hasAcceptedOffer;   // permanent: accepted -> never show again
        private bool _hasDeclinedOffer;   // permanent: declined -> never show again
        private bool _wasAtPeace;         // tracks the peace->war transition

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
            dataStore.SyncData("_hasDeclinedOffer", ref _hasDeclinedOffer);
            dataStore.SyncData("_wasAtPeace", ref _wasAtPeace);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter) => Initialize();
        private void OnGameLoaded(CampaignGameStarter campaignGameStarter) => Initialize();

        private void Initialize()
        {
            _lord3_1 = Hero.FindFirst(hero => hero.StringId == "lord_3_1");
            _lordKingdom = _lord3_1?.Clan?.Kingdom;

            if (_lord3_1 == null || _lordKingdom == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Initialization failed: lord_3_1 or his kingdom is null"));
                return;
            }

            // Establish baseline state so you don't instantly trigger on load/new game
            _wasAtPeace = !IsKingdomAtWar(_lordKingdom);
        }

        private void DailyTick()
        {
            // Hard gate: don’t even evaluate before day 30
            if (CampaignTime.Now.ToDays < OfferEarliestDay)
                return;

            // If anything is missing, or offer is already resolved (accepted/declined), never show again
            if (_lord3_1 == null || _lordKingdom == null || _hasAcceptedOffer || _hasDeclinedOffer)
                return;

            bool isAtWar = IsKingdomAtWar(_lordKingdom);

            // Trigger only on peace -> war transition (after day 30)
            if (_wasAtPeace && isAtWar)
            {
                CreateJoinWarDecisionPopUp();
            }

            _wasAtPeace = !isAtWar;
        }

        private bool IsKingdomAtWar(Kingdom kingdom)
        {
            return kingdom.FactionsAtWarWith.Count > 0;
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
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Clan.PlayerClan, _lordKingdom, default);
            _hasAcceptedOffer = true;

            InformationManager.DisplayMessage(new InformationMessage(
                "You have joined the war as a mercenary."));
        }

        private void OnDecline()
        {
            _hasDeclinedOffer = true;

            InformationManager.DisplayMessage(new InformationMessage(
                "You have declined the offer to join the war."));
        }
    }
}
