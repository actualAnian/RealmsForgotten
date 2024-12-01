using SandBox.Conversation.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Quest.AI_Quest
{
    public class MagicItemQuestBehavior : CampaignBehaviorBase
    {
        private static readonly string QuestGiverId = "arcane_library_maester";  // The NPC's StringId
        private static readonly string LordsHallLocationId = "lordshall";        // The location ID for the Lord's Hall
        private static readonly string MagicItemId = "poisoned_knife";           // The item ID for the quest

        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        private void OnMissionStarted(IMission mission)
        {
            if (CampaignMission.Current?.Location?.StringId == LordsHallLocationId)
            {
                // Spawn the quest giver in the Lord's Hall
                SpawnQuestGiver();
            }
        }

        private void SpawnQuestGiver()
        {
            try
            {
                CharacterObject questGiver = MBObjectManager.Instance.GetObject<CharacterObject>(QuestGiverId);
                if (questGiver != null && Mission.Current != null)
                {
                    AgentBuildData agentData = new AgentBuildData(new SimpleAgentOrigin(questGiver));
                    MatrixFrame spawnFrame = new MatrixFrame(Mat3.Identity, new Vec3(100f, 100f, 0f));  // Example spawn position
                    Agent agent = Mission.Current.SpawnAgent(agentData.InitialPosition(spawnFrame.origin).Team(Mission.Current.PlayerTeam));

                    InformationManager.DisplayMessage(new InformationMessage("The Arcane Maester has appeared in the Lord's Hall."));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in SpawnQuestGiver: {ex.Message}"));
            }
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (mobileParty.LeaderHero == Hero.MainHero && settlement.StringId == "town_EM1")
            {
                // Check if the player has the magic item and complete the quest if conditions are met
                var quest = Campaign.Current.QuestManager.Quests.FirstOrDefault(q => q is MagicItemQuest) as MagicItemQuest;
                quest?.CheckItemAndCompleteQuest();
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync data for save/load functionality
        }

        // The actual quest class, internal to MagicItemQuestBehavior
        internal class MagicItemQuest : QuestBase
        {
            [SaveableField(0)]
            public bool HasTalkedToMaester;

            [SaveableField(1)]
            private JournalLog retrieveItemJournalLog;  // Log for retrieving the item

            [SaveableField(2)]
            private JournalLog deliverItemJournalLog;   // Log for delivering the item

            [SaveableField(3)]
            public bool HasRetrievedItem;

            public MagicItemQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
                : base(questId, questGiver, duration, rewardGold)
            {
                SetDialogs();
                retrieveItemJournalLog = AddLog(new TextObject("Retrieve the Arcane Crystal for the Maester."));
            }

            public override TextObject Title => new TextObject("Arcane Maester's Request");

            public override bool IsRemainingTimeHidden => true;

            public override bool IsSpecialQuest => true;

            protected override void RegisterEvents()
            {
                // Register quest-specific events
            }

            public bool PlayerHasMagicItem()
            {
                ItemObject magicItem = MBObjectManager.Instance.GetObject<ItemObject>(MagicItemId);
                return magicItem != null && MobileParty.MainParty.ItemRoster.GetItemNumber(magicItem) > 0;
            }

            public void CheckItemAndCompleteQuest()
            {
                if (PlayerHasMagicItem())
                {
                    // Complete the quest by updating logs
                    HasRetrievedItem = true;
                    retrieveItemJournalLog.UpdateCurrentProgress(1);
                    deliverItemJournalLog = AddLog(new TextObject("Deliver the Arcane Crystal to the Arcane Maester."));
                    InformationManager.DisplayMessage(new InformationMessage("You have retrieved the Arcane Crystal. Deliver it to the Maester."));
                }
            }

            protected override void SetDialogs()
            {
                Campaign.Current.ConversationManager.AddDialogFlow(CreateQuestDialogFlow());
            }

            private DialogFlow CreateQuestDialogFlow()
            {
                return DialogFlow.CreateDialogFlow("start", 125)
                    .NpcLine(new TextObject("Greetings, traveler. I have a task for you, should you be willing to help."), null)
                    .Condition(() => !HasTalkedToMaester)
                    .PlayerLine(new TextObject("What do you need help with?"))
                    .NpcLine(new TextObject("I need you to retrieve a rare artifact for me. Will you do it?"))
                    .BeginPlayerOptions()
                        .PlayerOption(new TextObject("What kind of artifact is it?"), null)
                        .NpcLine(new TextObject("It is an ancient relic, rumored to hold great power."))
                        .PlayerOption(new TextObject("What's in it for me?"), null)
                        .NpcLine(new TextObject("You will be handsomely rewarded in gold, and knowledge of its secrets if you desire."))
                        .PlayerOption(new TextObject("I will retrieve the artifact for you."))
                        .Consequence(() => StartRetrieveItemQuest())
                        .CloseDialog()
                        .PlayerOption(new TextObject("No, I cannot help at the moment."))
                        .CloseDialog()
                    .EndPlayerOptions();
            }

            private void StartRetrieveItemQuest()
            {
                HasTalkedToMaester = true;
                retrieveItemJournalLog = AddDiscreteLog(new TextObject("Retrieve the Arcane Crystal."), new TextObject("Find and retrieve the Arcane Crystal for the Maester."), 0, 1);
            }

            protected override void HourlyTick()
            {
                // Handle hourly quest updates here
            }

            protected override void InitializeQuestOnGameLoad()
            {
                // Handle quest loading logic
            }
        }
    }
}


