using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
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
                Settlement.CurrentSettlement, // where he spawns
                null, // no father
                null, // no mother
                MBRandom.RandomInt(25, 40) // age between 25 and 40
            );

            // Correctly setting name: needs BOTH first and full name
            TextObject firstName = new TextObject("Peregrine");
            TextObject fullName = new TextObject("Pilgrim Monk");
            newMonk.SetName(firstName, fullName);

            newMonk.ChangeState(Hero.CharacterStates.Active); // make him active and usable
            newMonk.SetHasMet();
            newMonk.ChangeHeroGold(100);

            // Optional: set him neutral (PlayerClan or create neutral clan if you want)
            newMonk.Clan = Clan.PlayerClan;

            // Clean up party AI if needed
            newMonk.PartyBelongedTo?.SetMoveGoToSettlement(Settlement.CurrentSettlement, TaleWorlds.CampaignSystem.Party.MobileParty.NavigationType.Default, false);

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
            var towns = Settlement.All.Where(s => s.IsTown).ToList();
            if (towns.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ No towns found.", Colors.Red));
                return;
            }

            var randomTown = towns[MBRandom.RandomInt(towns.Count)];
            if (randomTown == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ No town selected.", Colors.Red));
                return;
            }

            Hero monkHero = CreateMonkHero();
            if (monkHero == null)
            {
                return; // Monk creation failed
            }

            // Optional: give unique string ID to avoid name collisions in saves
            monkHero.StringId = "peregrine_monk_" + Guid.NewGuid().ToString();

            // Ensure hero is tracked by clan
            if (!Clan.PlayerClan.Heroes.Contains(monkHero))
                Clan.PlayerClan.Heroes.Add(monkHero);

            // Quest creation and startup
            string questId = "help_peregrine_escort_" + MBRandom.RandomInt(100000, 999999);
            var quest = new HelpPeregrineQuest(questId, monkHero, CampaignTime.Days(7), randomTown);
            quest.StartQuest(); // this internally registers the quest

            InformationManager.DisplayMessage(
                new InformationMessage($"✅ The peregrine monk has joined you. Escort him to {randomTown.Name}.", Colors.Yellow));
        }



        private void OnDeclineEscort()
        {
            InformationManager.DisplayMessage(new InformationMessage("❌ You declined to help the peregrine monk.", Colors.Red));
        }
    }
}
