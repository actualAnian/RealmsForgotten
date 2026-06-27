using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    public class HelpPeregrineBehavior : CampaignBehaviorBase
    {
        private CampaignTime _lastEventTime;
        private const int DaysBetweenEvents = 90;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("HelpPeregrine_LastEventTime", ref _lastEventTime);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            _lastEventTime = CampaignTime.Now - CampaignTime.Days(60) + CampaignTime.Days(MBRandom.RandomInt(5, 15));
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            // Nothing special needed here
        }

        private void DailyTick()
        {
            if (CampaignTime.Now - _lastEventTime >= CampaignTime.Days(DaysBetweenEvents))
            {
                CreateHelpPeregrinePopup();
                _lastEventTime = CampaignTime.Now;
            }
        }

        private Hero CreateMonkHero()
        {
            CharacterObject monkTemplate = CharacterObject.Find("quest_monastery_monk");
            if (monkTemplate == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Monk template not found!", Colors.Red));
                return null;
            }

            Hero newMonk = HeroCreator.CreateSpecialHero(
                monkTemplate,
                Settlement.CurrentSettlement,
                null,
                null,
                MBRandom.RandomInt(25, 40)
            );

            TextObject firstName = new TextObject("Peregrine");
            TextObject fullName = new TextObject("Pilgrim Monk");
            newMonk.SetName(firstName, fullName);

            newMonk.ChangeState(Hero.CharacterStates.Active);
            newMonk.SetHasMet();
            newMonk.ChangeHeroGold(100);
            newMonk.Clan = Clan.PlayerClan;

            newMonk.PartyBelongedTo?.SetMoveGoToSettlement(
                Settlement.CurrentSettlement,
                TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default,
                false);

            return newMonk;
        }

        private void CreateHelpPeregrinePopup()
        {
            TextObject title = new TextObject("{=HelpPeregrineTitle}HELP A PEREGRINE");
            TextObject description = new TextObject("{=HelpPeregrineText}AS YOU WENT PAST A CROSSROADS, A LONELY PEREGRINE CALLED FOR YOUR HELP. HE HAS BEEN ROBBED AND LOOKS SCARED. THE THIEVES STOLE AN OFFERING HE WAS BRINGING TO A SHRINE. WILL YOU HELP HIM?");
            TextObject accept = new TextObject("{=Accept}ACCEPT AND ESCORT THE PEREGRINE");
            TextObject decline = new TextObject("{=Decline}DECLINE AND RESUME YOUR PATH");

            InquiryData inquiry = new InquiryData(
                title.ToString(),
                description.ToString(),
                true,
                true,
                accept.ToString(),
                decline.ToString(),
                OnAcceptEscort,
                OnDeclineEscort
            );

            InformationManager.ShowInquiry(inquiry, true, false);
        }

        private void OnAcceptEscort()
        {
            Settlement? randomTown = ChooseEscortDestination();
            if (randomTown == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ No suitable nearby town found.", Colors.Red));
                return;
            }

            Hero monkHero = CreateMonkHero();
            if (monkHero == null)
            {
                return;
            }

            monkHero.StringId = "peregrine_monk_" + Guid.NewGuid().ToString();

            if (!Clan.PlayerClan.Heroes.Contains(monkHero))
            {
                Clan.PlayerClan.Heroes.Add(monkHero);
            }

            string questId = "help_peregrine_escort_" + MBRandom.RandomInt(100000, 999999);
            var quest = new HelpPeregrineQuest(questId, monkHero, CampaignTime.Days(7), randomTown);
            quest.StartQuest();

            InformationManager.DisplayMessage(
                new InformationMessage($"✅ The peregrine monk has joined you. Escort him to {randomTown.Name}.", Colors.Yellow));
        }

        private static Settlement? ChooseEscortDestination()
        {
            Settlement? currentSettlement = Settlement.CurrentSettlement;
            Vec2 anchorPosition = currentSettlement?.GetPosition2D ?? MobileParty.MainParty?.GetPosition2D ?? Vec2.Zero;

            var nearbyTowns = Settlement.All
                .Where(settlement =>
                    settlement != null
                    && settlement.IsTown
                    && settlement != currentSettlement
                    && !settlement.IsUnderSiege)
                .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(anchorPosition))
                .Take(8)
                .ToList();

            if (nearbyTowns.Count > 0)
            {
                int nearbyCount = Math.Min(4, nearbyTowns.Count);
                return nearbyTowns[MBRandom.RandomInt(nearbyCount)];
            }

            return Settlement.All
                .Where(settlement =>
                    settlement != null
                    && settlement.IsTown
                    && settlement != currentSettlement
                    && !settlement.IsUnderSiege)
                .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(anchorPosition))
                .FirstOrDefault();
        }

        private void OnDeclineEscort()
        {
            InformationManager.DisplayMessage(new InformationMessage("❌ You declined to help the peregrine monk.", Colors.Red));
        }
    }
}
