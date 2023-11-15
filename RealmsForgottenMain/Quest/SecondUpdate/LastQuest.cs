﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RealmsForgotten.Quest.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static RealmsForgotten.Quest.QuestLibrary;

namespace RealmsForgotten.Quest.SecondUpdate
{
    internal class HellBoundAmbushLogic : MissionLogic
    {
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow,
            in AttackCollisionData attackCollisionData)
        {
            if(affectedAgent.Character?.Culture.StringId == "hellbound_outlaw" && affectedAgent.Team == Mission.AttackerTeam)
                affectedAgent.Health += blow.InflictedDamage + 10;
        }
    }
    internal class LastQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog takeBossToLordLog;

        [SaveableField(1)] 
        private float initialDistanceFromAnoritLord;
        [SaveableField(2)]
        private JournalLog goToMonasteryLog;

        [SaveableField(3)]
        private Settlement questMonastery;

        public static bool DisableSendTroops => Instance?.takeBossToLordLog?.CurrentProgress == 1;

        public static LastQuest Instance;
        public LastQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            Instance = this;
        }

        public override TextObject Title => GameTexts.FindText("rf_quest_title_part_four");

        public override bool IsRemainingTimeHidden => true;
        public override bool IsSpecialQuest => true;

        


        protected override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, OnCanHeroBecomePrisoner);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        private void OnTick(float dt)
        {
            if (takeBossToLordLog?.CurrentProgress == 2)
            {
                if(Hero.MainHero.IsPrisoner)
                    EndCaptivityAction.ApplyByReleasedAfterBattle(Hero.MainHero);
                
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
                takeBossToLordLog?.UpdateCurrentProgress(3);
            }
        }

        private void OnCanHeroBecomePrisoner(Hero hero, ref bool canHeroBecome)
        {
            if (takeBossToLordLog?.CurrentProgress == 1 && (hero == Hero.MainHero || hero == TheOwl))
                canHeroBecome = false;
        }

        private void OnMissionStarted(IMission imission)
        {
            if (imission is Mission mission && takeBossToLordLog?.CurrentProgress == 1)
            {
                mission.AddMissionBehavior(new HellBoundAmbushLogic());
                takeBossToLordLog.UpdateCurrentProgress(2);
            }
        }

        protected override void HourlyTick()
        {
            if (MobileParty.MainParty.Position2D.DistanceSquared(AnoritLord.PartyBelongedTo.Position2D) <=
                initialDistanceFromAnoritLord && takeBossToLordLog?.CurrentProgress == 0)
            {
                takeBossToLordLog?.UpdateCurrentProgress(1);
                Clan hellboundClan =
                    Clan.FindFirst(x => x.StringId == "hellbound_outlaw");

                MobileParty hellboundParty = BanditPartyComponent.CreateBanditParty("quest_hellbound_party", hellboundClan, Settlement.Find("hideout_forest_13").Hideout,
                    true);
                TroopRoster hellBoundTroopRoster = TroopRoster.CreateDummyTroopRoster();
                PartyTemplateObject partyTemplate = hellboundClan.DefaultPartyTemplate;

                for (int i = 0; i < MobileParty.MainParty.MemberRoster.TotalManCount; i++)
                {
                    hellBoundTroopRoster.AddToCounts(partyTemplate.Stacks[MBRandom.RandomInt(0, partyTemplate.Stacks.Count-1)].Character, 5);
                }

                hellboundParty.InitializeMobilePartyAtPosition(hellBoundTroopRoster, TroopRoster.CreateDummyTroopRoster(), MobileParty.MainParty.Position2D);
                hellboundParty.Ai.SetMoveEngageParty(MobileParty.MainParty);
                hellboundParty.Aggressiveness = 10f;
            }
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            TextObject textObject = GameTexts.FindText("rf_last_quest_first_log");
            textObject.SetCharacterProperties("LORD", AnoritLord.CharacterObject);
            takeBossToLordLog = AddLog(textObject);

            initialDistanceFromAnoritLord =
                MobileParty.MainParty.Position2D.DistanceSquared(AnoritLord.PartyBelongedTo.Position2D);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            QuestLibrary.InitializeVariables();
            Instance = this;
        }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(FirstDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(MonkDialogFlow, this);
        }

        private void FirstDialogConsequence()
        {
            takeBossToLordLog.UpdateCurrentProgress(3);
            AddHeroToPartyAction.Apply(TheOwl, MobileParty.MainParty);
            questMonastery = Settlement.Find("retreat_monastery");
            AddTrackedObject(questMonastery);
            goToMonasteryLog = AddLog(GameTexts.FindText("rf_last_quest_second_log"));
            questMonastery.IsVisible = true;
        }

        private void ShowWaitScreen()
        {
            QuestUIManager.ShowNotification("After a while...", ()=>{}, false);
        }

        private void MonkDialogConsequence()
        {
            PartyBase.MainParty.AddMember(CharacterObject.Find("anorite_monastery_militia"), 20);
            PartyBase.MainParty.ItemRoster.AddToCounts(new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("anorit_fire_stone_t3_rfthrowing50")), 10);
            goToMonasteryLog.UpdateCurrentProgress(1);
        }

        private TextObject LineWithPlayerLink()
        {
            TextObject text = GameTexts.FindText("rf_last_quest_monk_dialog_11");
            text.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
            return text;
        }
        private DialogFlow FirstDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_1"))
            .Condition(() => takeBossToLordLog?.CurrentProgress == 2 && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_5"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_6"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_7"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_8"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_9"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_10"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_11"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_13"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_15"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_first_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_last_quest_first_dialog_17"))
            .Consequence(FirstDialogConsequence).CloseDialog();

        private DialogFlow MonkDialogFlow => DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_1"))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "quest_monastery_priest" && goToMonasteryLog?.CurrentProgress == 0 && Settlement.CurrentSettlement == questMonastery)
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_6"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_7"))
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_8"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_9")).Consequence(ShowWaitScreen)
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_10"))
            .PlayerLine(LineWithPlayerLink())
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_13"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_15"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_17"))
            .PlayerLine(GameTexts.FindText("rf_last_quest_monk_dialog_18"))
            .NpcLine(GameTexts.FindText("rf_last_quest_monk_dialog_19")).Consequence(MonkDialogConsequence).CloseDialog();
    }
}
