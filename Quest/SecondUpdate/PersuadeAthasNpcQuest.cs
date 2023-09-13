using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Quest.SecondUpdate
{
    internal class PersuadeAthasNpcBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckIfFirstQuestHasEnded);
        }

        private void CheckIfFirstQuestHasEnded()
        {
            SaveCurrentQuestCampaignBehavior currentQuestCampaignBehavior = SaveCurrentQuestCampaignBehavior.Instance;
            if (currentQuestCampaignBehavior != null && currentQuestCampaignBehavior.questStoppedAt != null)
            {
                Hero hero = null;
                if (currentQuestCampaignBehavior.questStoppedAt == "anorit")
                    hero = Hero.FindFirst(x => x.StringId == "lord_WE9_l");
                else if(currentQuestCampaignBehavior.questStoppedAt == "queen")
                    hero = Kingdom.All.First(x => x.StringId == "empire").Leader.Spouse;

                new PersuadeAthasNpcQuest("athas_quest", hero, CampaignTime.Never, 0).StartQuest();
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }


    internal class PersuadeAthasNpcQuest : QuestBase
    {
        [SaveableField(1)] private JournalLog goToAnoritLordLog;
        [SaveableField(2)] private JournalLog takeAthasScholarLog;

        [SaveableField(3)] private Hero athasScholarHero;
        public PersuadeAthasNpcQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {

        }
        Settlement Ityr => Settlement.Find("town_A1");
        public override TextObject Title => GameTexts.FindText("rf_quest_title_part_three");
        protected override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this,
                (CampaignGameStarter campaignGameStarter) =>
                {
                    campaignGameStarter.AddGameMenuOption("town", "town_athas_quest_option", GameTexts.FindText("town_athas_quest_option").ToString(), x=>Settlement.CurrentSettlement == Ityr,
                        args =>
                        {

                        });
                });
        }
        public override bool IsRemainingTimeHidden => true;
        protected override void OnStartQuest()
        {
            base.OnStartQuest();
            SetDialogs();
            athasScholarHero = HeroCreator.CreateSpecialHero(CharacterObject.Find("rf_athas_scholar"), Ityr, Ityr.OwnerClan);

            InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_anorit_message").ToString(), true, false, GameTexts.FindText("str_done").ToString(), "",
                () =>
                {
                    TextObject objective = GameTexts.FindText("rf_third_quest_anorit_objective_1");
                    objective.SetCharacterProperties("ANORIT", this.QuestGiver.CharacterObject);
                    goToAnoritLordLog = this.AddLog(objective);
                }, null), true);
        }

        protected override void HourlyTick()
        {

        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }

        private void AnoritFirstDialogConsequence()
        {
            //Primeiro fazer um diálogo com sistema pra convencer, se o player falhar, ativa o objetivo de captura
            takeAthasScholarLog = this.AddLog(new TextObject());
            this.AddTrackedObject(Ityr);

        }
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(AnoritFirstDialogFlow);
        }
        private DialogFlow AnoritFirstDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_1")).Condition(() => Hero.OneToOneConversationHero == this.QuestGiver && goToAnoritLordLog.CurrentProgress == 0)
            .NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_2")).PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_3")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_5")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_6")).Consequence(AnoritFirstDialogConsequence).CloseDialog();
    }
}
