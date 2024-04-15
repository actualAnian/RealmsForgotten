using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using RealmsForgotten.Quest.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static RealmsForgotten.Quest.QuestLibrary;

namespace RealmsForgotten.Quest.SecondUpdate
{
    internal class FifthQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog talkToMonkLog;
        public FifthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
        }
        protected override void OnStartQuest()
        {
            SetDialogs();
        }
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(FirstDialogFlow, this);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }

        protected override void HourlyTick()
        {

        }

        public override TextObject Title { get; }
        public override bool IsRemainingTimeHidden { get; }
        
        private DialogFlow FirstDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_first_dialog_1"))
            .Condition(() => talkToMonkLog == null && Hero.OneToOneConversationHero == TheOwl)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_first_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_first_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_first_dialog_4"))
            .Consequence(() =>
            {
                talkToMonkLog = AddLog(GameTexts.FindText("rf_fifth_quest_first_objective"));
                talkToMonkLog.UpdateCurrentProgress(1);
            }).CloseDialog();
        
        private DialogFlow SecondDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_1"))
            .Condition(() => talkToMonkLog?.CurrentProgress == 1 && CharacterObject.OneToOneConversationCharacter?.StringId == "quest_monastery_priest")
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_5"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_6"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_7"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_8"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_9"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_10"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_11"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_13")) // monk
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_15"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_17"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_18"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_19"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_20"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_21"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_22"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_23"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_24"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_25"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_second_dialog_26"))
            .CloseDialog();
        
        
        
        private DialogFlow ThirdDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_third_dialog_1"))
            .Condition(() => Hero.OneToOneConversationHero == TheOwl)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_third_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_third_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_third_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_third_dialog_5"));
        
        
        private DialogFlow NasorianKingDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_1"))
            .Condition(() => Hero.OneToOneConversationHero == TheOwl)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_6"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_7"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_8"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_9"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_10"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_11"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_13"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_15"));
        
        
        
        private DialogFlow ElveanKingDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_1"))
            .Condition(() => Hero.OneToOneConversationHero == TheOwl)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_6"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_7"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_8"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_9"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_10"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_11"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_13"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_15"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_17"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_18"));
        
    }
}
