using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Quest.AI_Quest
{
    public class KnightOfferCampaignBehavior : CampaignBehaviorBase
    {
        private const float KnightOfferCreationChance = 0.02f;
        private Tuple<Kingdom, CampaignTime> _currentKnightOffer;
        private bool _stopOffers;

        public override void RegisterEvents()
        {
            if (!_stopOffers)
            {
                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
                CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
                CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_currentKnightOffer", ref _currentKnightOffer);
            dataStore.SyncData("_stopOffers", ref _stopOffers);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddKnightDialogues(campaignGameStarter);
        }

        private void DailyTick()
        {
            if (_stopOffers || Clan.PlayerClan.Tier <= Campaign.Current.Models.ClanTierModel.MinClanTier)
            {
                return;
            }
            if (_currentKnightOffer != null)
            {
                if (_currentKnightOffer.Item2.ElapsedHoursUntilNow >= 48f)
                {
                    CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferCanceled(_currentKnightOffer.Item1);
                }
                return;
            }
            float randomFloat = MBRandom.RandomFloat;
            if (randomFloat <= KnightOfferCreationChance && CanPlayerReceiveKnightOffer())
            {
                Kingdom targetKingdom = Kingdom.All.GetRandomElementWithPredicate(KnightKingdomSelectionConditionsHold);
                if (targetKingdom != null)
                {
                    CreateKnightOffer(targetKingdom);
                }
            }
        }

        private bool KnightKingdomSelectionConditionsHold(Kingdom kingdom)
        {
            return !kingdom.IsAtWarWith(Clan.PlayerClan.Kingdom) && kingdom.Leader != Hero.MainHero;
        }

        private bool CanPlayerReceiveKnightOffer()
        {
            return Clan.PlayerClan.Kingdom == null && Clan.PlayerClan.Tier >= 3;
        }

        private void CreateKnightOffer(Kingdom kingdom)
        {
            _currentKnightOffer = new Tuple<Kingdom, CampaignTime>(kingdom, CampaignTime.Now);
            ShowKnightOfferNotification(kingdom);
        }

        private void ShowKnightOfferNotification(Kingdom kingdom)
        {
            TextObject notificationText = new TextObject("{=KnightOffer}You have been offered a position as a knight by {KINGDOM_NAME}.")
                .SetTextVariable("KINGDOM_NAME", kingdom.Name);

            KnightOfferMapNotification knightOfferNotification = new KnightOfferMapNotification(kingdom, notificationText);
            Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(knightOfferNotification);
        }

        private class KnightOfferMapNotification : InformationData
        {
            private Kingdom _kingdom;

            public KnightOfferMapNotification(Kingdom kingdom, TextObject description) : base(description)
            {
                _kingdom = kingdom;
            }

            public override string SoundEventPath => "";
            public override TextObject TitleText => new TextObject("Knight Offer");
        }

        
        private void ApplyJoinAsKnight(Clan clan, Kingdom kingdom)
        {
            if (clan.Kingdom != null)
            {
                
                ChangeKingdomAction.ApplyByLeaveKingdom(clan, false);
            }

           
            ChangeKingdomAction.ApplyByJoinToKingdom(clan, kingdom);

           
            clan.Influence = 50;

            
            InformationManager.DisplayMessage(new InformationMessage($"{clan.Leader.Name} has joined {kingdom.Name} as a knight."));
        }

        private void OnHeroPrisonerTaken(PartyBase captor, Hero prisoner)
        {
            if (prisoner == Hero.MainHero && _currentKnightOffer != null)
            {
                CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferCanceled(_currentKnightOffer.Item1);
            }
        }

        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
        {
            if (clan == Clan.PlayerClan && newKingdom != null)
            {
                _stopOffers = true;
                if (_currentKnightOffer != null)
                {
                    CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferCanceled(_currentKnightOffer.Item1);
                }
            }
        }

        private void AddKnightDialogues(CampaignGameStarter campaignGameStarter)
        {
            // Add knight-specific dialogues here.
        }
    }
}