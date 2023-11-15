using RealmsForgotten.Quest.SecondUpdate;
using RealmsForgotten.Quest.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Quest
{
    public class AnoritFindRelicsQuest : QuestBase
    {
        [SaveableField(1)]
        private CampaignTime anoritLordConversationTime;
        [SaveableField(2)]
        private bool escapedPrison;
        public AnoritFindRelicsQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            TextObject questObjectiveText = GameTexts.FindText("rf_anorit_objective_1");
            questObjectiveText.SetCharacterProperties("ANORIT", QuestGiver.CharacterObject, false);
            this.AddLog(questObjectiveText);
            escapedPrison = false;
            anoritLordConversationTime = CampaignTime.Now;
        }

        public override TextObject Title => GameTexts.FindText("rf_anorit_quest_title");
        protected override void RegisterEvents()
        {
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this,
                (MobileParty mobileParty, Settlement settlement) =>
                {
                    if (mobileParty.LeaderHero == Hero.MainHero && settlement.StringId == "town_B4" && escapedPrison)
                        SaveCurrentQuestCampaignBehavior.Instance.SaveQuestState("anorit");
                });
        }
        public override bool IsRemainingTimeHidden => true;
        public override bool IsSpecialQuest => true;

        protected override void HourlyTick()
        {
            if (!escapedPrison && anoritLordConversationTime != CampaignTime.Never && anoritLordConversationTime.ElapsedHoursUntilNow >= 40 && !PlayerEncounter.InsideSettlement && CampaignTime.Now.IsNightTime)
            {
                QuestUIManager.ShowNotification(GameTexts.FindText("rf_kidnapped_text").ToString(), QueenQuest.OpenPrisonBreak, true, "prisoner_image");
                anoritLordConversationTime = CampaignTime.Never;
                escapedPrison = true;

            }
        }

        protected override void InitializeQuestOnGameLoad()
        {
            QuestLibrary.InitializeVariables();
        }

        protected override void SetDialogs()
        {

        }
    }

    internal class SaveCurrentQuestCampaignBehavior : CampaignBehaviorBase
    {
        public string questStoppedAt;
        public bool escapedPrison;
        public static SaveCurrentQuestCampaignBehavior Instance;
        public SaveCurrentQuestCampaignBehavior()
        {
            Instance = this;
            escapedPrison = false;
        }

        public void SaveQuestState(string _questStoppedAt)
        {
            questStoppedAt = _questStoppedAt;
            if (questStoppedAt == "anorit")
            {
                AnoritFindRelicsQuest qb = (AnoritFindRelicsQuest)Campaign.Current.QuestManager.Quests.FirstOrDefault(x => x.GetType() == typeof(AnoritFindRelicsQuest));

                if (qb != null)
                    qb.CompleteQuestWithSuccess();
            }
            else
            {
                QueenQuest qb = (QueenQuest)Campaign.Current.QuestManager.Quests.FirstOrDefault(x => x.GetType() == typeof(QueenQuest));

                if (qb != null)
                    qb.CompleteQuestWithSuccess();
            }
            escapedPrison = true;
                
        }
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckIfFirstQuestHasEnded);
        }

        private void CheckIfFirstQuestHasEnded()
        {
            SaveCurrentQuestCampaignBehavior currentQuestCampaignBehavior = SaveCurrentQuestCampaignBehavior.Instance;

            if (currentQuestCampaignBehavior?.questStoppedAt != null && !Campaign.Current.QuestManager.Quests.Any(x => x is PersuadeAthasNpcQuest))
            {
                Hero hero = null;
                if (currentQuestCampaignBehavior.questStoppedAt == "anorit")

                    hero = Hero.FindFirst(x => x.StringId == "lord_WE9_l");

                else if (currentQuestCampaignBehavior.questStoppedAt == "queen")

                    hero = Kingdom.All.First(x => x.StringId == "empire").Leader.Spouse;



                new PersuadeAthasNpcQuest("athas_quest", hero, CampaignTime.Never, 0).StartQuest();

                questStoppedAt = null;
            }
        }
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("questStoppedAt", ref questStoppedAt);
            dataStore.SyncData("escapedPrison", ref escapedPrison);
        }
    }
}
