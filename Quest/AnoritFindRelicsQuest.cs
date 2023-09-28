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

namespace Quest
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

        protected override void HourlyTick()
        {
            if (!escapedPrison && anoritLordConversationTime != CampaignTime.Never && anoritLordConversationTime.ElapsedHoursUntilNow >= 40 && !PlayerEncounter.InsideSettlement && CampaignTime.Now.IsNightTime)
            {
                GameStateManager.Current.PushState(GameStateManager.Current.CreateState<RFNotificationState>(GameTexts.FindText("rf_kidnapped_text").ToString(), 40, () => { QueenQuest.OpenPrisonBreak(); }));
                anoritLordConversationTime = CampaignTime.Never;
                escapedPrison = true;

            }
        }

        protected override void InitializeQuestOnGameLoad()
        {

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


            InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("quest_continuation_box_title").ToString(), GameTexts.FindText("quest_continuation_box_desc").ToString(), true, false, GameTexts.FindText("rf_ok").ToString(), "", null, null));
            escapedPrison = true;
                
        }

        public override void RegisterEvents()
        {

        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("questStoppedAt", ref questStoppedAt);
            dataStore.SyncData("escapedPrison", ref escapedPrison);
        }
    }
}
