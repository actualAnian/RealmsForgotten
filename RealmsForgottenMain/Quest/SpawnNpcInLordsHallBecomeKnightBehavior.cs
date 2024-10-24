using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;

namespace RealmsForgotten.Quest
{
    public class SpawnNpcInLordsHallBecomeKnightBehavior : CampaignBehaviorBase
    {
        [SaveableField(1)]
        private BecomeKnightQuest quest;

        private static readonly string QuestGiverId = "south_realm_knight_maester";
        private static readonly string LordsHallLocationId = "lordshall";
        private static readonly string QuestItemId = "rfmisc_western_2hsword_t3_fire";

        private static SpawnNpcInLordsHallBecomeKnightBehavior Instance { set; get; }

        public SpawnNpcInLordsHallBecomeKnightBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        // Method triggered when a mission starts (e.g., entering the Lord's Hall)
        private void OnMissionStarted(IMission imission)
        {
            LocationCharactersAreReadyToSpawn();

            if (imission is Mission mission)
            {
                mission.AddMissionBehavior(new SpawnNpcInLordsHallBecomeKnightMissionBehavior());
            }
        }

        private class SpawnNpcInLordsHallBecomeKnightMissionBehavior : MissionBehavior
        {
            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

            public override void OnRenderingStarted()
            {
                base.OnRenderingStarted();

                if (CampaignMission.Current?.Location != null && CampaignMission.Current.Location.StringId == LordsHallLocationId)
                {
                    // Ensure the Knight Guild Master is present, but don't start any conversation here.
                    if (Instance.quest != null && Instance.quest.PlayerHasInsignia())
                    {
                        Agent questGiverAgent = Mission.Current.Agents.Find(a => a.Character.StringId.Contains(QuestGiverId));
                        if (questGiverAgent != null)
                        {
                            // The Knight Guild Master is spawned, but the conversation should not start automatically.
                        }
                    }
                }
            }

            public override void OnMissionStateActivated()
            {
                base.OnMissionStateActivated();
            }

            public override void AfterStart()
            {
                base.AfterStart();
            }
        }

        private void LocationCharactersAreReadyToSpawn()
        {
            try
            {
                if (CampaignMission.Current != null && CampaignMission.Current.Location != null)
                {
                    Location location = CampaignMission.Current.Location;

                    // Check if the player is in the Lord's Hall of the specific town with ID "town_EM1"
                    Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement;
                    if (location.StringId == LordsHallLocationId && settlement != null && settlement.StringId == "town_ES1")
                    {
                        if (settlement.IsTown)
                        {
                            LocationCharacter locationCharacter = CreateQuestGiver(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                            location.AddCharacter(locationCharacter);

                            InformationManager.DisplayMessage(new InformationMessage("Knight Guild Master has been spawned in the Lord's Hall."));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LocationCharactersAreReadyToSpawn: {ex.Message}"));
            }
        }

        // Create the quest giver's LocationCharacter for the "Become Knight" quest
        private static LocationCharacter CreateQuestGiver(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            try
            {
                CharacterObject questGiver = MBObjectManager.Instance.GetObject<CharacterObject>(QuestGiverId);

                int minValue, maxValue;
                Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(questGiver, out minValue, out maxValue, "");
                Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(questGiver.Race, "_settlement");

                AgentData agentData = new AgentData(new SimpleAgentOrigin(questGiver, -1, null, default(UniqueTroopDescriptor)))
                                      .Monster(monsterWithSuffix)
                                      .Age(MBRandom.RandomInt(minValue, maxValue));

                LocationCharacter locationCharacter = new LocationCharacter(
                    agentData,
                    new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors),  // Fixed behavior for the NPC
                    "sp_lordshall_hero",  // Spawn point tag
                    true,
                    relation,
                    null,
                    true,
                    false,
                    null,
                    false,
                    false,
                    true);

                return locationCharacter;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in CreateQuestGiver: {ex.Message}"));
                return null;
            }
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            SetDialogs();
        }

        // Set up dialog for quest interactions
        private void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(CreateStartQuestDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateAfterQuestDialogFlow());

        }

        private DialogFlow CreateStartQuestDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Greetings, aspiring knight. Do you wish to prove yourself worthy?", null)
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "south_realm_knight_maester"
                                && Settlement.CurrentSettlement != null
                                && Settlement.CurrentSettlement.StringId == "town_ES1"
                                && (quest == null || !quest.IsOngoing && !quest.IsFinalized))  // Ensure quest hasn't started yet
                .PlayerLine("What must I do to become a knight?")
                .NpcLine("You must retrieve the Knight's Insignia from a distant temple. Will you accept the challenge?")
                .BeginPlayerOptions()

                // Option 1: Ask about the Knight's Insignia
                .PlayerOption("What is the Knight's Insignia?", null)
                .NpcLine("It is a sacred relic, symbolizing honor and courage.")
                .BeginPlayerOptions()
                    .PlayerOption("What do I gain from this?", null)
                    .NpcLine("You will be granted the title of knight and receive great rewards.")
                    .BeginPlayerOptions()
                        .PlayerOption("I will retrieve the Knight's Insignia.")
                        .Consequence(() => OnStartQuest())  // Start the quest
                        .CloseDialog()
                        .PlayerOption("No, I cannot accept this task.")
                        .CloseDialog()
                    .EndPlayerOptions()
                .EndPlayerOptions()

                // Option 2: Ask about the rewards first
                .PlayerOption("What do I gain from this?", null)
                .NpcLine("You will be granted the title of knight and receive great rewards.")
                .BeginPlayerOptions()
                    .PlayerOption("What is the Knight's Insignia?", null)
                    .NpcLine("It is a sacred relic, symbolizing honor and courage.")
                    .BeginPlayerOptions()
                        .PlayerOption("I will retrieve the Knight's Insignia.")
                        .Consequence(() => OnStartQuest())  // Start the quest
                        .CloseDialog()
                        .PlayerOption("No, I cannot accept this task.")
                        .CloseDialog()
                    .EndPlayerOptions()
                .EndPlayerOptions()

                .EndPlayerOptions();
        }
        private DialogFlow CreateAfterQuestDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Congratulations!")
                .Condition(() => quest.IsFinalized 
                                 && CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                && Settlement.CurrentSettlement != null
                                && Settlement.CurrentSettlement.StringId == "town_ES1")
                .CloseDialog();
        }

        // Starts the quest and initializes the quest object
        private void OnStartQuest()
        {
            quest = new BecomeKnightQuest("become_knight_quest", Hero.MainHero, CampaignTime.YearsFromNow(1), 500);
            quest.StartQuest();
            InformationManager.DisplayMessage(new InformationMessage("Quest started: Become a Knight."));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("quest", ref this.quest);
            if (quest != null)
            {
                quest.SyncData(dataStore);  // Sync inner quest data manually
            }
        }
        public class BecomeKnightQuest : QuestBase
        {
            [SaveableField(1)]
            private string _questId;

            [SaveableField(2)]
            private Hero _questGiver;

            [SaveableField(3)]
            private JournalLog? _retrieveInsigniaLog;

            [SaveableField(4)]
            private JournalLog? _defeatBanditsLog;

            [SaveableField(6)]
            private int _banditsDefeated = 0;

            [SaveableField(7)]
            private JournalLog? _defeatHideoutLog;

            [SaveableField(8)]
            private int _hideoutsCleared = 0;

            private const int BanditsToDefeatTarget = 1;
            private const int HideoutsToDefeatTarget = 1;

            private static readonly string QuestGiverId = "south_realm_knight_maester";
            private static readonly string QuestItemId = "rfmisc_western_2hsword_t3_fire";

            public BecomeKnightQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
                : base(questId, questGiver, duration, rewardGold)
            {
                _questId = questId;
                _questGiver = questGiver;

                InitializeQuestOnCreation();
                InitializeLogs();
                SetDialogs();  // Initialize dialogs on creation
            }

            public override bool IsSpecialQuest => true;

            public override TextObject Title => new("Become a Knight");

            public override bool IsRemainingTimeHidden => false;

            private void InitializeLogs()
            {
                _retrieveInsigniaLog = AddDiscreteLog(
                    new TextObject("Retrieve the Knight's Insignia from the distant temple."),
                    new TextObject("Find and bring back the Knight's Insignia."), 0, 1);
            }

            protected override void InitializeQuestOnGameLoad()
            {
                SetDialogs();  // Ensure dialogs are re-initialized after loading
            }

            protected override void HourlyTick()
            {
                // Add logic that should happen every in-game hour, or leave it empty if not needed
            }

            protected override void RegisterEvents()
            {
                base.RegisterEvents();
                CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyedHandler);
                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
                CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
                CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            }

            private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
            {
                // Additional logic if needed
            }

            private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
            {
                if (mobileParty == MobileParty.MainParty && settlement == _questGiver.CurrentSettlement)
                {
                    CheckInsigniaInInventory();
                }
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
                if (destroyer.LeaderHero != null && destroyer.LeaderHero != Hero.MainHero) return;

                if (destroyedParty.IsBandit)
                {
                    _banditsDefeated++;

                    InformationManager.DisplayMessage(new InformationMessage($"You have defeated {_banditsDefeated}/{BanditsToDefeatTarget} bandit parties."));

                    if (_banditsDefeated >= BanditsToDefeatTarget)
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
                    InformationManager.DisplayMessage(new InformationMessage($"Error: Knight Insignia item with ID '{QuestItemId}' not found."));
                    return false;
                }

                int itemCount = Hero.MainHero.PartyBelongedTo.ItemRoster.GetItemNumber(knightInsignia);

                if (itemCount > 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Knight's Insignia found in inventory."));
                    return true;
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Knight's Insignia not found in inventory."));
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
                        _defeatHideoutLog.UpdateCurrentProgress(1);

                        InformationManager.DisplayMessage(new InformationMessage($"You have cleared a bandit hideout! {_hideoutsCleared}/{HideoutsToDefeatTarget} completed."));

                        if (_hideoutsCleared >= HideoutsToDefeatTarget)
                        {
                            InformationManager.DisplayMessage(new InformationMessage("You have cleared all required bandit hideouts! Return to the quest giver."));
                        }
                    }
                    else if (mapEvent.BattleState == BattleState.DefenderVictory)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("You failed to clear the hideout."));
                        // Handle quest failure if needed
                    }
                }
            }

            protected override void SetDialogs()
            {
                Campaign.Current.ConversationManager.AddDialogFlow(CreateRetrieveInsigniaTrueDialogFlow());
                Campaign.Current.ConversationManager.AddDialogFlow(CreateRetrieveInsigniaFalseDialogFlow());
                Campaign.Current.ConversationManager.AddDialogFlow(CreateBanditObjectiveDialogFlow());
                Campaign.Current.ConversationManager.AddDialogFlow(CreateHideoutObjectiveDialogFlow());
            }

            private DialogFlow CreateRetrieveInsigniaTrueDialogFlow()
            {
                return DialogFlow.CreateDialogFlow("start", 125)
                    .NpcLine("Ah, have you retrieved the Knight's Insignia?")
                    .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                     && _retrieveInsigniaLog?.CurrentProgress == 1
                                     && Settlement.CurrentSettlement?.StringId == _questGiver.CurrentSettlement.StringId)
                    .PlayerLine("Yes, I have it.")
                    .NpcLine("Good. Now, defeat the bandit parties to prove your worth.")
                    .Consequence(() =>
                    {
                        _retrieveInsigniaLog?.UpdateCurrentProgress(2);  // Update the quest to the next stage
                        StartBanditObjective();  // Proceed with the bandit objective
                    })
                    .CloseDialog();
            }

            private DialogFlow CreateRetrieveInsigniaFalseDialogFlow()
            {
                    return DialogFlow.CreateDialogFlow("start", 125).NpcLine("Do you have the Knight's Insignia?")
                    .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                     && _retrieveInsigniaLog?.CurrentProgress == 0
                                     && Settlement.CurrentSettlement?.StringId == _questGiver.CurrentSettlement.StringId)
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
                                     && Settlement.CurrentSettlement?.StringId == _questGiver.CurrentSettlement.StringId)
                    .BeginPlayerOptions()
                        // Option when the bandits are not yet defeated
                        .PlayerOption("Not yet, I am still working on it.")
                        .Condition(() => _banditsDefeated < BanditsToDefeatTarget)
                        .NpcLine("Keep going, you need to prove your worth.")
                        .CloseDialog()

                        // Option when the bandits have been defeated
                        .PlayerOption("Yes, I have defeated the bandits.")
                        .Condition(() => _banditsDefeated >= BanditsToDefeatTarget)
                        .NpcLine("Excellent work! Now, clear the bandit hideout(s) to complete your quest.")
                        .Consequence(() =>
                        {
                            _defeatBanditsLog.UpdateCurrentProgress(BanditsToDefeatTarget);
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
                                     && _defeatHideoutLog != null // Ensure hideout objective has started
                                     && Settlement.CurrentSettlement?.StringId == _questGiver.CurrentSettlement.StringId)
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
                                     && Settlement.CurrentSettlement?.StringId == _questGiver.CurrentSettlement.StringId)
                        .NpcLine("Well done. You have proven your worth.")
                        .Consequence(() =>
                        {
                            CompleteQuest();  // Complete the quest
                        })
                        .CloseDialog()
                    .EndPlayerOptions();
            }
            public void CompleteQuest()
            {
                // Check if all objectives are completed
                if (_retrieveInsigniaLog?.CurrentProgress >= 2
                    && _banditsDefeated >= BanditsToDefeatTarget
                    && _hideoutsCleared >= HideoutsToDefeatTarget)
                {
                    // Update the logs to mark the objectives as fully completed
                    _retrieveInsigniaLog.UpdateCurrentProgress(2);
                    _defeatBanditsLog?.UpdateCurrentProgress(BanditsToDefeatTarget);
                    _defeatHideoutLog?.UpdateCurrentProgress(HideoutsToDefeatTarget);

                    // Reward the player and finalize the quest
                    IncreaseRelationWithSpecificClan("clan_empire_south_3", 10);
                    SwitchPlayerToNewFaction();
                    GiveReward();

                    CompleteQuestWithSuccess();  // Mark the quest as completed successfully
                }
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

            private void GiveReward()
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 2000);  // Reward the player with gold
                InformationManager.DisplayMessage(new InformationMessage("You have received 2000 gold and earned the title of Knight."));
            }

            private void SwitchPlayerToNewFaction()
            {
                Kingdom newKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "empire_s");
                Clan newClan = Clan.All.FirstOrDefault(c => c.StringId == "clan_empire_south_3");

                if (newKingdom != null && newClan != null)
                {
                    if (Clan.PlayerClan.Kingdom != null)
                    {
                        ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, false); // Leaves the player's current kingdom
                    }

                    if (Clan.PlayerClan.IsUnderMercenaryService)
                    {
                        ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, true); // Leave mercenary service if applicable
                    }

                    ChangeKingdomAction.ApplyByJoinToKingdom(Clan.PlayerClan, newKingdom); // Joins the player to the new kingdom as a vassal
                    InformationManager.DisplayMessage(new InformationMessage($"You are now a vassal of {newKingdom.Name} under the {newClan.Name} clan."));

                    // Clear any active mercenary offers to avoid conflicts
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
            }
        }
    }
}







