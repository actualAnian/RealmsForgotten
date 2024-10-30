using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SandBox.Conversation.MissionLogics;
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
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.Quest.AI_Quest
{
    public class SpawnNpcInLordsHallBehavior : CampaignBehaviorBase
    {
        private static readonly string QuestGiverId = "south_realm_knight_maester";  // The NPC's StringId
        private static readonly string LordsHallLocationId = "lordshall";  // The location ID for the Lord's Hall
        private static readonly string QuestItemId = "rfmisc_poisoned_javelin";  // The item ID for the quest
        private RetrieveArtifactQuest quest;  // The quest instance

        private static SpawnNpcInLordsHallBehavior Instance { set; get; }

        public SpawnNpcInLordsHallBehavior()
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
                mission.AddMissionBehavior(new SpawnNpcInLordsHallMissionBehavior());
            }
        }

        private class SpawnNpcInLordsHallMissionBehavior : MissionBehavior
        {
            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

            public override void OnRenderingStarted()
            {
                base.OnRenderingStarted();

                if (CampaignMission.Current?.Location != null && CampaignMission.Current.Location.StringId == LordsHallLocationId)
                {
                    // Ensure the Maester is present, but don't start any conversation here.
                    if (Instance.quest != null && Instance.quest.PlayerHasArtifact())
                    {
                        Agent questGiverAgent = Mission.Current.Agents.Find(a => a.Character.StringId.Contains(QuestGiverId));
                        if (questGiverAgent != null)
                        {
                            // The Maester is spawned, but the conversation should not start automatically.
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
                    if (location.StringId == LordsHallLocationId && settlement != null && settlement.StringId == "town_EN1")
                    {
                        if (settlement.IsTown)
                        {
                            LocationCharacter locationCharacter = CreateQuestGiver(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                            location.AddCharacter(locationCharacter);

                            InformationManager.DisplayMessage(new InformationMessage("Quest Giver has been spawned in the Lord's Hall of town_EM1."));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LocationCharactersAreReadyToSpawn: {ex.Message}"));
            }
        }

        // Create the quest giver's LocationCharacter
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
            Campaign.Current.ConversationManager.AddDialogFlow(CreateQuestDialogFlow());
        }

        private DialogFlow CreateQuestDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Greetings, traveler. I have a task for you, should you be willing to help.", null)
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "south_realm_knight_maester"
                                && Settlement.CurrentSettlement != null
                                && Settlement.CurrentSettlement.StringId == "town_EN1"
                                && (quest == null || !quest.IsOngoing))  // Only show this dialog if the quest hasn't started
                .PlayerLine("What do you need help with?")
                .NpcLine("I need you to retrieve a rare artifact for me. Will you do it?")
                .BeginPlayerOptions()

                // Option 1: Ask about the kind of artifact
                .PlayerOption("What kind of artifact is it?", null)
                .NpcLine("It is an ancient relic, rumored to hold great power.")
                .BeginPlayerOptions()
                    .PlayerOption("What's in it for me?", null)
                    .NpcLine("You will be handsomely rewarded in gold, and knowledge of its secrets if you desire.")
                    .BeginPlayerOptions()
                        .PlayerOption("I will retrieve the artifact for you.")
                        .Consequence(() => OnStartQuest())  // Start the quest
                        .CloseDialog()
                        .PlayerOption("No, I cannot help at the moment.")
                        .CloseDialog()
                    .EndPlayerOptions()
                .EndPlayerOptions()

                // Option 2: Ask about the reward first
                .PlayerOption("What's in it for me?", null)
                .NpcLine("You will be handsomely rewarded in gold, and knowledge of its secrets if you desire.")
                .BeginPlayerOptions()
                    .PlayerOption("What kind of artifact is it?", null)
                    .NpcLine("It is an ancient relic, rumored to hold great power.")
                    .BeginPlayerOptions()
                        .PlayerOption("I will retrieve the artifact for you.")
                        .Consequence(() => OnStartQuest())  // Start the quest
                        .CloseDialog()
                        .PlayerOption("No, I cannot help at the moment.")
                        .CloseDialog()
                    .EndPlayerOptions()
                .EndPlayerOptions()

                .EndPlayerOptions();
        }

        // Starts the quest and initializes the quest object
        private void OnStartQuest()
        {
            quest = new RetrieveArtifactQuest("retrieve_artifact_quest", Hero.MainHero, CampaignTime.YearsFromNow(1), 500);
            quest.StartQuest();
            InformationManager.DisplayMessage(new InformationMessage("Quest started: Retrieve the rare artifact."));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("quest", ref quest);
        }

        // Inner class to handle the quest logic
        public class RetrieveArtifactQuest : QuestBase
        {
            [SaveableField(1)]
            private string _questId; // Removed readonly
            [SaveableField(2)]
            private Hero _questGiver; // Removed readonly
            [SaveableField(3)]
            private JournalLog _retrieveArtifactLog;
            [SaveableField(4)]
            private bool _isOngoing;

            private static readonly string QuestGiverId = "south_realm_knight_maester";
            private static readonly string QuestItemId = "rfmisc_western_2hsword_t3_fire";  // **Ensure this ID is correct**

            public override bool IsSpecialQuest => true;

            // Implementing the abstract properties from QuestBase
            public override TextObject Title => new TextObject("Retrieve the Rare Artifact");
            public override bool IsRemainingTimeHidden => false;

            public RetrieveArtifactQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
                : base(questId, questGiver, duration, rewardGold)
            {
                _questId = questId;
                _questGiver = questGiver;
                _isOngoing = true;
               
                InitializeLogs();
                SetDialogs();
                RegisterEvents();
            }

            private void InitializeLogs()
            {
                _retrieveArtifactLog = AddDiscreteLog(
                    new TextObject("Retrieve the Knight's Insignia from the distant temple."),
                    new TextObject("Find and bring back the Knight's Insignia."),
                    0,
                    1
                );
                InformationManager.DisplayMessage(new InformationMessage("Quest log initialized with one discrete log."));
            }

            protected override void RegisterEvents()
            {
                base.RegisterEvents();
                CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
                // Add other necessary event listeners here
                InformationManager.DisplayMessage(new InformationMessage("Quest events registered."));
            }

            protected override void InitializeQuestOnGameLoad()
            {
                SetDialogs();
            }

            protected override void HourlyTick()
            {
                // Add logic that should happen every in-game hour, or leave it empty if not needed
            }

            protected override void SetDialogs()
            {
                Campaign.Current.ConversationManager.AddDialogFlow(CreateRetrieveArtifactDialogFlow());
                InformationManager.DisplayMessage(new InformationMessage("Quest dialog flow set."));
            }

            private DialogFlow CreateRetrieveArtifactDialogFlow()
            {
                return DialogFlow.CreateDialogFlow("start", 125)
                    .NpcLine("Ah, have you retrieved the Knight's Insignia?")
                    .Condition(() => {
                        bool isCorrectNPC = CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId;
                        bool hasCompletedArtifact = _retrieveArtifactLog?.CurrentProgress == 1;
                        bool isQuestOngoing = _isOngoing;
                        bool isInCorrectSettlement = Settlement.CurrentSettlement?.StringId == _questGiver?.CurrentSettlement?.StringId;

                        // Debug Messages
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"Dialog Condition Check:\n" +
                            $"isCorrectNPC: {isCorrectNPC}\n" +
                            $"hasCompletedArtifact: {hasCompletedArtifact}\n" +
                            $"isQuestOngoing: {isQuestOngoing}\n" +
                            $"isInCorrectSettlement: {isInCorrectSettlement}"
                        ));

                        return isCorrectNPC && hasCompletedArtifact && isQuestOngoing && isInCorrectSettlement;
                    })
                    .PlayerLine("Yes, I have it.")
                    .NpcLine("Good work. Here is your reward.")
                    .Consequence(() => CompleteQuest())
                    .CloseDialog()
                    .PlayerLine("No, I do not have it yet.")
                    .CloseDialog();
            }

            private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
            {
                if (mobileParty == MobileParty.MainParty && settlement.StringId == _questGiver?.CurrentSettlement?.StringId)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Entered settlement '{settlement.Name}' for quest."));
                    CheckPlayerHasArtifact();
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Entered settlement '{settlement.Name}' does not match quest giver's settlement."));
                }
            }

            private void CheckPlayerHasArtifact()
            {
                if (PlayerHasArtifact() && _retrieveArtifactLog?.CurrentProgress == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage("You have obtained the Knight's Insignia. Return to the quest giver."));
                    _retrieveArtifactLog.UpdateCurrentProgress(1);
                    InformationManager.DisplayMessage(new InformationMessage("Quest log updated to progress 1."));
                }
            }

            public bool PlayerHasArtifact()
            {
                ItemObject artifact = MBObjectManager.Instance.GetObject<ItemObject>(QuestItemId);
                return artifact != null && Hero.MainHero.PartyBelongedTo.ItemRoster.GetItemNumber(artifact) > 0;
            }

                    

        public void StartQuest()
            {
                // Since logs are initialized in the constructor, simply mark the quest as ongoing
                _isOngoing = true;
                InformationManager.DisplayMessage(new InformationMessage("Quest started: Retrieve the rare artifact."));
                // No need to add logs here
            }

            public void CompleteQuest()
            {
                if (PlayerHasArtifact())
                {
                    _isOngoing = false;
                    _retrieveArtifactLog.UpdateCurrentProgress(2);  // Assuming 2 signifies completion
                    InformationManager.DisplayMessage(new InformationMessage("Quest marked as complete."));
                    GiveReward();
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Player does not have the artifact during quest completion."));
                }
            }

            private void GiveReward()
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 500);  // Adjust reward as needed
                InformationManager.DisplayMessage(new InformationMessage("You have received 500 gold as a reward."));
            }

            public void SyncData(IDataStore dataStore)
            {
                dataStore.SyncData("_questId", ref _questId);
                dataStore.SyncData("_questGiver", ref _questGiver);
                dataStore.SyncData("_retrieveArtifactLog", ref _retrieveArtifactLog);
                dataStore.SyncData("_isOngoing", ref _isOngoing);
            }
        }
    }
}