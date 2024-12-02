using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.AIQuest
{
    internal class SimpleNpcQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog talkToNpcLog;
        [SaveableField(1)]
        private JournalLog returnToQuestGiverLog;

        public override TextObject Title => new TextObject("Simple NPC Quest");

        public override bool IsRemainingTimeHidden => true;

        // Constructor for the quest
        public SimpleNpcQuest(string questId, CampaignTime duration, int rewardGold)
            : base(questId, null, duration, rewardGold)  // Notice no Hero needed for quest giver
        {
            InitializeQuest();
        }

        // Initialization: Add logs for the quest
        private void InitializeQuest()
        {
            talkToNpcLog = AddLog(GameTexts.FindText("simple_npc_quest_log_talk_to_npc"));
            returnToQuestGiverLog = AddLog(GameTexts.FindText("simple_npc_quest_log_return_to_npc"));
        }

        // Set dialogs for the NPC interactions
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(InitialDialogFlow(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(QuestCompletionDialogFlow(), this);
        }

        // Define the flow of the conversation for the initial interaction
        private DialogFlow InitialDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("I have a task for you.")  // NPC dialogue
                .PlayerLine("What task do you have?")  // Player response
                .NpcLine("Go to the arcane keep and talk to the guardian.")  // NPC dialogue
                .Consequence(() =>
                {
                    talkToNpcLog = AddLog(GameTexts.FindText("simple_npc_quest_log_talk_to_npc"));
                })
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "arcane_library_maester_b")  // Check the NPC StringId
                .CloseDialog();
        }

        // Define the flow of the conversation for quest completion
        private DialogFlow QuestCompletionDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .Condition(() => talkToNpcLog?.CurrentProgress == 1 && CharacterObject.OneToOneConversationCharacter?.StringId == "arcane_library_maester_b")  // Check if we are talking to the right NPC
                .PlayerLine("I have completed your task.")  // Player's response
                .NpcLine("Thank you for helping me. Here is your reward.")  // NPC's response
                .Consequence(() =>
                {
                    returnToQuestGiverLog.UpdateCurrentProgress(1);
                    CompleteQuestSuccessfully();
                })
                .CloseDialog();
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();  // Re-initialize dialogs on load
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStart);
        }

        private void OnMissionStart(IMission imission)
        {
            if (imission is Mission mission && Settlement.CurrentSettlement != null)
            {
                if (mission.Scene?.GetName() == "arcane_keep_interior" && talkToNpcLog?.CurrentProgress == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage("You have entered the arcane keep!"));
                    talkToNpcLog.UpdateCurrentProgress(1);
                }
            }
        }

        private void CompleteQuestSuccessfully()
        {
            CompleteQuestWithSuccess();
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000);  // Reward example
            InformationManager.DisplayMessage(new InformationMessage("Quest completed! You received 1000 gold."));
        }

        protected override void HourlyTick() { }

        public void StartSimpleNpcQuest()
        {
            StartQuest();
        }
    }

    // This class starts the quest
    public class QuestStarter
    {
        public void StartNewSimpleNpcQuest()
        {
            // Create and start the quest (no Hero needed for quest giver)
            SimpleNpcQuest newQuest = new SimpleNpcQuest("simple_npc_quest", CampaignTime.DaysFromNow(10), 1000);
            newQuest.StartSimpleNpcQuest();

            InformationManager.DisplayMessage(new InformationMessage($"Quest '{newQuest.Title}' started with a simple NPC."));
        }
    }
}
