using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Quest.AI_Quest
{
    internal class CustomQuestTemplate : QuestBase
    {
        [SaveableField(0)]
        private JournalLog talkToNpcLog;
        [SaveableField(1)]
        private JournalLog completeObjectiveLog;
        [SaveableField(2)]
        private bool isObjectiveCompleted;

        public override TextObject Title => new TextObject("{=custom_quest_title}A Custom Quest");
        public override bool IsRemainingTimeHidden => false;
        public static CustomQuestTemplate Instance { get; private set; }

        public CustomQuestTemplate(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold)
        {
            Instance = this;
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            talkToNpcLog = AddLog(new TextObject("{=custom_quest_log_start}Talk to the NPC."));
            RegisterEvents();
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoad);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            RegisterEvents();
        }

        protected override void HourlyTick()
        {
            // Insert any logic here or leave empty if none is needed
        }

        private void OnConversationEnded(CharacterObject character, ConversationSentence sentence)
        {
            if (character == QuestGiver.CharacterObject && talkToNpcLog?.CurrentProgress == 0)
            {
                talkToNpcLog.UpdateCurrentProgress(1);
                completeObjectiveLog = AddLog(new TextObject("{=custom_quest_log_complete}Complete the quest objective."));
            }
        }

        private void OnMissionStarted(IMission mission)
        {
            // Example: Add specific mission behaviors if needed
        }

        private void OnGameLoad()
        {
            // Example: Reinitialize quest state after game load
            SetDialogs();
        }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(
                DialogFlow.CreateDialogFlow("start", 100)
                    .NpcLine(new TextObject("{=custom_quest_giver_dialog}Greetings, traveler! I have a task that needs your skills."))
                    .Condition(() => CharacterObject.OneToOneConversationCharacter == QuestGiver.CharacterObject)
                    .Consequence(() => StartQuest())
                    .PlayerLine(new TextObject("{=custom_quest_accept}I am willing to help."))
                    .CloseDialog()
            );

            Campaign.Current.ConversationManager.AddDialogFlow(
                DialogFlow.CreateDialogFlow("npc_talk", 100)
                    .NpcLine(new TextObject("{=custom_quest_objective_dialog}Thank you for helping!"))
                    .Condition(() => talkToNpcLog?.CurrentProgress == 1 && CharacterObject.OneToOneConversationCharacter == QuestGiver.CharacterObject)
                    .Consequence(() => CompleteObjective())
                    .CloseDialog()
            );
        }

        private void CompleteObjective()
        {
            completeObjectiveLog?.UpdateCurrentProgress(1);

            // Reward the player with gold
            if (QuestGiver != null && RewardGold > 0)
            {
                // Grant gold to the player
                Hero.MainHero.ChangeHeroGold(RewardGold);

                // Optionally, show a message to the player
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=reward_gold_message}You have been rewarded with {GOLD_AMOUNT} denars.")
                    .SetTextVariable("GOLD_AMOUNT", RewardGold).ToString()));
            }

            CompleteQuestWithSuccess();
        }
    }

    internal class CustomQuestTemplateSaveDefiner : SaveableTypeDefiner
    {
        public CustomQuestTemplateSaveDefiner() : base(12345678) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(CustomQuestTemplate), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(JournalLog));
        }
    }
}
