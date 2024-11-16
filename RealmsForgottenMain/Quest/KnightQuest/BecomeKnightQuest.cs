using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.GameMenus;
using System.Collections.Generic;

namespace RealmsForgotten.Quest.KnightQuest
{

    public class BecomeKnightQuest : QuestBase
    {
        private Hero _questGiver; // IMPORTANT, KNIGHT MAESTER IS NOT A HERO, SO QUEST GIVER POINTS TO THE PLAYER, AS A "CHEAT"
        [SaveableField(1)]
        private string _questId;


        [SaveableField(2)]
        private JournalLog? _retrieveInsigniaLog;

        [SaveableField(3)]
        private JournalLog? _defeatBanditsLog;

        [SaveableField(4)]
        private int _banditsDefeated = 0;

        [SaveableField(5)]
        private JournalLog? _defeatHideoutLog;

        [SaveableField(6)]
        private int _hideoutsCleared = 0;

        [SaveableField(7)]
        private JournalLog? _beMercenaryLog;
        [SaveableField(8)]
        private JournalLog? _duelLog;
        [SaveableField(9)]
        private int _daysAsMercenary = 0;
        [SaveableField(10)]
        private bool _shouldDuelBeOnHorse;

        private const int BanditsToDefeatTarget = 10;
        private const int HideoutsToDefeatTarget = 5;
        private const int DaysToBeMercenary = 100;

        private static readonly string QuestGiverId = "south_realm_knight_maester";
        private static readonly string QuestItemId = "rfmisc_western_2hsword_t3_fire";

        public static CharacterObject KnightMaester
        {
            get
            {
                return MBObjectManager.Instance.GetObject<CharacterObject>(QuestGiverId);
            }
        }
        public BecomeKnightQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold)
        {
            _questId = questId;
            _questGiver = questGiver;
            InitializeQuestOnCreation();
            InitializeLogs();
            SetDialogs();
        }

        public bool BecameMercenary
        {
            get
            {
                return _defeatHideoutLog != null && _defeatHideoutLog.HasBeenCompleted() && _beMercenaryLog?.CurrentProgress == 0;
            }
        }
        public void IncrementDaysAsMercenary()
        {
            if (_beMercenaryLog == null || _beMercenaryLog.CurrentProgress == 1) return;
            _daysAsMercenary += 1;
            if (_daysAsMercenary >= DaysToBeMercenary)
            {
                MBInformationManager.AddQuickInformation(new TextObject("You have served as mercenary for enough days. Return to the knight maester!"));
                _beMercenaryLog?.UpdateCurrentProgress(1);
            }
        }
        public override bool IsSpecialQuest => true;

        public override TextObject Title => new("Become a Knight");

        public override bool IsRemainingTimeHidden => false;

        private void InitializeLogs()
        {
            _retrieveInsigniaLog = AddDiscreteLog(
                new TextObject("Retrieve the Knight's Sword from the monastery."),
                new TextObject("Find and bring back the Knight's Sword."), 0, 1);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }

        protected override void HourlyTick() { }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyedHandler);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
        }
        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero) //@TODO, how to fix this
        {
            if (party.LeaderHero?.CharacterObject != CharacterObject.PlayerCharacter) return;
            if (settlement.Culture.StringId == "neutral_culture")
                CheckInsigniaInInventory();
        }
        private void CheckInsigniaInInventory()
        {
            if (PlayerHasInsignia() && _retrieveInsigniaLog?.CurrentProgress == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("You have obtained the Knight's Insignia. Return to the quest giver."));
                _retrieveInsigniaLog.UpdateCurrentProgress(1);
            }
        }
        private void OnMobilePartyDestroyedHandler(MobileParty destroyedParty, PartyBase destroyer)
        {
            if (destroyer.LeaderHero == null || destroyer.LeaderHero != Hero.MainHero) return;

            if (destroyedParty.IsBandit)
            {
                _banditsDefeated++;

                if (_banditsDefeated == BanditsToDefeatTarget)
                {
                    CompleteBanditObjective();
                }
            }
        }

        private void CompleteBanditObjective()
        {
            _defeatBanditsLog?.UpdateCurrentProgress(BanditsToDefeatTarget);
            MBInformationManager.AddQuickInformation(new TextObject("You have defeated enough bandit parties. Return to complete the quest."));
        }

        public bool PlayerHasInsignia()
        {
            ItemObject knightInsignia = MBObjectManager.Instance.GetObject<ItemObject>(QuestItemId);

            if (knightInsignia == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error: Knight Sword item with ID '{QuestItemId}' not found."));
                return false;
            }

            int itemCount = Hero.MainHero.PartyBelongedTo.ItemRoster.GetItemNumber(knightInsignia);

            if (itemCount > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("Knight's Sword found in inventory."));
                return true;
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Knight's Sword not found in inventory."));
                return false;
            }
        }
        private void StartBanditObjective()
        {
            if (_defeatBanditsLog == null)
            {
                _defeatBanditsLog = AddDiscreteLog(
                    new TextObject("Defeat the roaming bandit parties in the area."),
                    new TextObject($"Defeat {BanditsToDefeatTarget} bandit party(ies)."),
                    0,
                    BanditsToDefeatTarget
                );
                InformationManager.DisplayMessage(new InformationMessage("New objective: Defeat bandit parties."));
            }
        }

        private void StartDefeatHideoutsObjective()
        {
            if (_defeatHideoutLog == null)
            {
                _defeatHideoutLog = AddDiscreteLog(
                    new TextObject("Defeat bandit hideouts."),
                    new TextObject($"Clear {HideoutsToDefeatTarget} bandit hideout(s)."), 0, HideoutsToDefeatTarget);

                InformationManager.DisplayMessage(new InformationMessage($"New objective: Clear {HideoutsToDefeatTarget} bandit hideout(s)."));
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (!mapEvent.IsHideoutBattle)
            {
                return;
            }

            if (mapEvent.InvolvedParties.Contains(PartyBase.MainParty))
            {
                if (mapEvent.BattleState == BattleState.AttackerVictory)
                {
                    _hideoutsCleared++;

                    MBInformationManager.AddQuickInformation(new TextObject($"You have cleared a bandit hideout! {_hideoutsCleared}/{HideoutsToDefeatTarget} completed."));

                    if (_hideoutsCleared >= HideoutsToDefeatTarget)
                    {
                        MBInformationManager.AddQuickInformation(new TextObject("You have cleared all required bandit hideouts! Return to the quest giver."));
                        _defeatHideoutLog?.UpdateCurrentProgress(1);
                    }
                }
                else if (mapEvent.BattleState == BattleState.DefenderVictory)
                {
                    InformationManager.DisplayMessage(new InformationMessage("You failed to clear the hideout."));
                }
            }
        }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(CreateRetrieveInsigniaTrueDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateRetrieveInsigniaFalseDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateBanditObjectiveDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateHideoutObjectiveDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateServeAsMercenaryNotFinishedDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateServeAsMercenaryFinishedDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateDoDuelDialogFlow());
        }

        private DialogFlow CreateRetrieveInsigniaTrueDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Ah, have you retrieved the Sword?")
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                 && _retrieveInsigniaLog?.CurrentProgress == 1)
                .PlayerLine("Yes, I have it.")
                .NpcLine("Good... That was your task of bravery, now you your task of honour begins. Hunt and defeat bandit parties to prove your worth.")
                .Consequence(() =>
                {
                    _retrieveInsigniaLog?.UpdateCurrentProgress(2);  // Update the quest to the next stage
                    StartBanditObjective();  // Proceed with the bandit objective
                })
                .CloseDialog();
        }

        private DialogFlow CreateRetrieveInsigniaFalseDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125).NpcLine("Do you have the Knight's Sword?")
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                             && _retrieveInsigniaLog?.CurrentProgress == 0)
            .PlayerLine("No, I do not have it yet.")
            .CloseDialog();
        }
        private DialogFlow CreateBanditObjectiveDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Have you defeated the bandits?")
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                 && _defeatHideoutLog == null
                                 && _retrieveInsigniaLog?.CurrentProgress == 2
                                 )
                .BeginPlayerOptions()
                    // Option when the bandits are not yet defeated
                    .PlayerOption("Not yet, I am still working on it.")
                    .Condition(() => _banditsDefeated < BanditsToDefeatTarget)
                    .NpcLine("Keep going, you need to prove your worth.")
                    .CloseDialog()

                    // Option when the bandits have been defeated
                    .PlayerOption("Yes, I have defeated the bandits.")
                    .Condition(() => _banditsDefeated >= BanditsToDefeatTarget)
                    .NpcLine("Excellent work! You managed to make on step further, an important one. You prove you are able to protect the weak. Now, you need to go into those scum´s nest. Find and destroy their hideouts, to purge the land from this evil.")
                    .Consequence(() =>
                    {
                        _defeatBanditsLog?.UpdateCurrentProgress(BanditsToDefeatTarget);
                        StartDefeatHideoutsObjective();
                    })
                    .CloseDialog()
                .EndPlayerOptions();
        }

        private DialogFlow CreateHideoutObjectiveDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Have you cleared the bandit hideout(s)?")
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                 && IsOngoing
                                 && _beMercenaryLog == null
                                 )
                .BeginPlayerOptions()
                    // Option: The player hasn't cleared the hideout(s) yet
                    .PlayerOption("Not yet, I am still working on it.")
                    .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                && _hideoutsCleared < HideoutsToDefeatTarget)
                    .NpcLine("Keep going, you need to prove your worth by clearing the hideout(s).")
                    .CloseDialog()

                    // Option: The player has defeated the required number of hideouts
                    .PlayerOption("Yes, I have defeated the bandit hideout(s).")
                    .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                 && _defeatHideoutLog?.CurrentProgress == 1
                                 )
                    .NpcLine("Well done. You have proven your bravery, your honour and your worth. Now the task of loyalty begins. A knight is nothing without true loyalty. You will serve our queen and wage war against her enemies. That is the moment you prove yourself worthy of the Queen´s trust. God bless you.")
                    .Consequence(() =>
                    {
                        _beMercenaryLog = AddDiscreteLog(
                            new TextObject("Serve as mercenary."),
                            new TextObject($"Serve as mercenary for {DaysToBeMercenary} days(s)."), 0, DaysToBeMercenary);
                    })
                    .CloseDialog()
                .EndPlayerOptions();
        }
        private DialogFlow CreateServeAsMercenaryNotFinishedDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("You have not served for enough days.", (IAgent agent) => agent.Character.StringId == QuestGiverId, (IAgent agent) => agent == Mission.Current.MainAgent)
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                 && IsOngoing
                                 && _beMercenaryLog?.CurrentProgress == 0
                                 && _duelLog == null).CloseDialog();
        }
        private DialogFlow CreateServeAsMercenaryFinishedDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("You did it!")
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                 && IsOngoing
                                 && _beMercenaryLog?.CurrentProgress == 1
                                 && _duelLog == null)
                .NpcLine("I am honoured to be taking you into the ranks of knights. " +
                "The Knighthood ceremony will be happening after a ceremonial duel against me. Don't worry, not many have bested me in combat, so people won't expect you to win, however it will be a great way to earn reputation in these lands. " +
                "Now, you can choose, whether you would like the duel to happen on foot, or on the horse.")
                .BeginPlayerOptions()
                    .PlayerOption("Lords and ladies alike are astounded by my horse riding skills.")
                    .Consequence(() => _shouldDuelBeOnHorse = true)
                    .NpcLine("I will meet you in an arena. Have your best armour and finest horse brought.")
                    .Consequence(() =>
                    {
                        _duelLog =
                            _duelLog = AddDiscreteLog(
                                new TextObject("Duel the maester."),
                                new TextObject($"Go to the arena and duel {KnightMaester.Name} on a horse."), 0, 1);
                    })
                    .CloseDialog()

                    .PlayerOption("I am unmatched in hand to hand combat.")
                    .Consequence(() => _shouldDuelBeOnHorse = false)
                    .NpcLine("I will meet you in an arena. Have your best armour brought.")
                    .Consequence(() =>
                    {
                        _duelLog =
                            _duelLog = AddDiscreteLog(
                                new TextObject("Duel the maester."),
                                new TextObject($"Go to the arena and duel {KnightMaester.Name} on foot."), 0, 1);
                    })
                    .CloseDialog()
                .EndPlayerOptions();
        }
        private DialogFlow CreateDoDuelDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Let us duel in the arena.", (IAgent agent) => agent.Character.StringId == QuestGiverId, (IAgent agent) => agent == Mission.Current.MainAgent)
                .Condition(() =>
                                 //CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                 //&& 
                                 IsOngoing
                                 && _duelLog?.CurrentProgress == 0)
                .CloseDialog();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddGameMenus(starter);
        }
        private void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town_arena", "rf_knight_quest_start_duel", "Start Ceremonial Duel for Knighthood", new GameMenuOption.OnConditionDelegate(this.game_menu_start_duel_on_condition), new GameMenuOption.OnConsequenceDelegate(this.game_menu_start_duel_on_consequence), false, 2, false, null);

        }
        private bool game_menu_start_duel_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;
            return IsOngoing && _duelLog?.CurrentProgress == 0;
        }
        private void game_menu_start_duel_on_consequence(MenuCallbackArgs args)
        {
            Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("arena");
            int upgradeLevel = Settlement.CurrentSettlement.Town.GetWallLevel();
            KnightQuestDuelMission.OpenDuelMission(locationWithId.GetSceneName(upgradeLevel), locationWithId, KnightMaester, _shouldDuelBeOnHorse, OnDuelEnded);
        }

        private void OnDuelEnded(bool playerWon)
        {
            _duelLog?.UpdateCurrentProgress(1);
            CompleteQuest(playerWon);
        }

        public void CompleteQuest(bool shouldGetFullReward)
        {
            IncreaseRelationWithSpecificClan("clan_empire_south_3", 10);
            SwitchPlayerToNewFaction();
            GiveReward(shouldGetFullReward);

            CompleteQuestWithSuccess();
        }

        private void IncreaseRelationWithSpecificClan(string clanId, int relationAmount)
        {
            Clan targetClan = Clan.All.FirstOrDefault(c => c.StringId == clanId);

            if (targetClan != null)
            {
                foreach (Hero clanHero in targetClan.Heroes)
                {
                    ChangeRelationAction.ApplyPlayerRelation(clanHero, relationAmount);
                }

                InformationManager.DisplayMessage(new InformationMessage($"Your relation with the {targetClan.Name} clan has increased by {relationAmount}."));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error: Clan with ID '{clanId}' not found."));
            }
        }

        private void GiveReward(bool fullReward)
        {
            int renown = fullReward ? 50 : 20;
            int gold = fullReward ? 10000 : 500;

            ChangeClanInfluenceAction.Apply(Clan.PlayerClan, renown);
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold);

            // Base items rewarded in both cases
            List<string> baseItemIds = new List<string>
    {
        "realm_plated_cav_helmet",
        "realm_knight_shoulder_plates",
        "realm_knight_armor",
        "realm_expert_gauntlets",
        "r_knight_chausses",
        "t2_empire_horse",
        "half_scale_barding"
    };

            // Additional item for full reward
            if (fullReward)
            {
                baseItemIds.Add("rfmisc_western_2hsword_t3_fire");
            }

            foreach (string itemId in baseItemIds)
            {
                ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>(itemId);
                if (item != null)
                {
                    Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(item, 1);
                }
            }

            string rewardMessage = fullReward
                ? $"You have received {gold} gold, earned the title of Knight, and been gifted an sacred fire sword."
                : $"You have received {gold} gold and earned the title of Knight.";

            InformationManager.DisplayMessage(new InformationMessage(rewardMessage));
        }



        private void SwitchPlayerToNewFaction()
        {
            Kingdom newKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "empire_s");
            Clan newClan = Clan.All.FirstOrDefault(c => c.StringId == "clan_empire_south_3");

            if (newKingdom != null && newClan != null)
            {
                if (Clan.PlayerClan.Kingdom != null)
                {
                    ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, false);
                }

                if (Clan.PlayerClan.IsUnderMercenaryService)
                {
                    ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, true);
                }

                ChangeKingdomAction.ApplyByJoinToKingdom(Clan.PlayerClan, newKingdom);
                InformationManager.DisplayMessage(new InformationMessage($"You are now a vassal of {newKingdom.Name} under the {newClan.Name} clan."));

                CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferCanceled(newKingdom);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Failed to switch factions. The kingdom or clan could not be found."));
            }
        }

        public void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_questId", ref _questId);
            dataStore.SyncData("_questGiver", ref _questGiver);
            dataStore.SyncData("_banditsDefeated", ref _banditsDefeated);
            dataStore.SyncData("_hideoutsCleared", ref _hideoutsCleared);
            dataStore.SyncData("_daysAsMercenary", ref _daysAsMercenary);
            dataStore.SyncData("_shouldDuelBeOnHorse", ref _shouldDuelBeOnHorse);
        }
    }
}







