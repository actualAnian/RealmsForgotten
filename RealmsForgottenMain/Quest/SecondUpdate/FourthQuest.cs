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
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
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
    
    internal class FourthQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog takeBossToLordLog;
        [SaveableField(1)] 
        private float initialDistanceFromAnoritLord;
        [SaveableField(2)]
        private JournalLog goToMonasteryLog;
        [SaveableField(3)]
        private Settlement questMonastery;
        [SaveableField(4)]
        private float initialDistanceToMonastery;
        [SaveableField(5)]
        private JournalLog captureHellboundLog;

        public static bool DisableSendTroops => Instance?.takeBossToLordLog?.CurrentProgress == 1;

        public static FourthQuest Instance;
        public FourthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
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
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, BattleEnd);
        }

        private void BattleEnd(MapEvent mapEvent)
        {
            MapEventSide? defeatedSide = mapEvent.DefenderSide.MissionSide == mapEvent.DefeatedSide
                ? mapEvent.DefenderSide
                : mapEvent.AttackerSide.MissionSide == mapEvent.DefeatedSide ? mapEvent.AttackerSide : null;

            if (captureHellboundLog?.CurrentProgress == 0 && defeatedSide?.LeaderParty.Culture.StringId == "hellbound_outlaw")
            {
                captureHellboundLog.UpdateCurrentProgress(1);
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(CharacterObject.Find("hellbound_chief")));
            }
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
                initialDistanceFromAnoritLord * 0.7 && takeBossToLordLog?.CurrentProgress == 0)
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

            if (MobileParty.MainParty.Position2D.DistanceSquared(questMonastery.GatePosition) <= initialDistanceToMonastery * 0.7 && takeBossToLordLog?.CurrentProgress == 3)
            {
                takeBossToLordLog.UpdateCurrentProgress(4);
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
            }
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            TextObject textObject = GameTexts.FindText("rf_fourth_quest_first_log");
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
            Campaign.Current.ConversationManager.AddDialogFlow(UliahTableDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(MonkDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(HellboudPersuasionDialogFlow(), this);

        }

       
        private void FirstDialogConsequence()
        {
            takeBossToLordLog.UpdateCurrentProgress(3);
            AddHeroToPartyAction.Apply(TheOwl, MobileParty.MainParty);
            questMonastery = Settlement.Find("retreat_monastery");
            AddTrackedObject(questMonastery);
            goToMonasteryLog = AddLog(GameTexts.FindText("rf_fourth_quest_second_log"));
            questMonastery.IsVisible = true;
            initialDistanceToMonastery = MobileParty.MainParty.Position2D.DistanceSquared(questMonastery.GetPosition2D);
        }

        private void ShowWaitScreen()
        {
            QuestUIManager.ShowNotification("After a while...", ()=>{}, false);
        }

         
        private void MonkDialogConsequence()
        {
            PartyBase.MainParty.AddMember(CharacterObject.Find("monk_knight"), 20);
            PartyBase.MainParty.ItemRoster.AddToCounts(new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("anorit_fire_stone_t3_rfthrowing50")), 10);

            captureHellboundLog = AddLog(GameTexts.FindText("rf_fourth_quest_third_log"));

            goToMonasteryLog.UpdateCurrentProgress(1);
        }

        private TextObject LineWithPlayerLink()
        {
            TextObject text = GameTexts.FindText("rf_fourth_quest_monk_dialog_11");
            text.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
            return text;
        }

        private void OwlGivesTableConsequence()
        {
            PartyBase.MainParty.ItemRoster.Add(new ItemRosterElement(MBObjectManager.Instance.GetObject<ItemObject>("vortiak_stone_tablet")));
            takeBossToLordLog.UpdateCurrentProgress(5);
        }

        private DialogFlow HellboudPersuasionDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_fourth_quest_hellbound_dialog_1")).Condition(()=>captureHellboundLog?.CurrentProgress == 1 && CharacterObject.OneToOneConversationCharacter?.StringId == "hellbound_chief")
                .GotoDialogState("quest_hellbound_dialog_start");

            dialogFlow.AddDialogLine("quest_hellbound_id_1", "quest_hellbound_dialog_start", "quest_hellbound_dialog_1",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_2").ToString(), null, ()=>StartPersuasion(true), this);

            dialogFlow.AddDialogLine("quest_hellbound_id_2", "quest_hellbound_dialog_1", "quest_hellbound_dialog_options_1",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_2").ToString(), () => !ConversationManager.GetPersuasionProgressSatisfied(), null, this);




            dialogFlow.AddPlayerLine("quest_hellbound_id_3", "quest_hellbound_dialog_options_1", "quest_hellbound_dialog_outcome_1",
                "{=!}{PERSUADE_ATTEMPT_1}", PersuasionOptionCondition_1, PersuasionOptionConsequence_1, this, 100, null, () => _persuasionTask.Options.ElementAt(0));

            dialogFlow.AddPlayerLine("quest_hellbound_id_4", "quest_hellbound_dialog_options_1", "quest_hellbound_dialog_outcome_1",
                "{=!}{PERSUADE_ATTEMPT_2}", PersuasionOptionCondition_2, PersuasionOptionConsequence_2, this, 100, null, () => _persuasionTask.Options.ElementAt(1));



            dialogFlow.AddDialogLine("quest_hellbound_id_5", "quest_hellbound_dialog_outcome_1", "quest_hellbound_dialog_options_2",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_1").ToString(), ConversationManager.GetPersuasionProgressSatisfied, RestartPersuasionTask, this);

            dialogFlow.AddDialogLine("quest_hellbound_id_6", "quest_hellbound_dialog_outcome_1", "quest_hellbound_dialog_options_2",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_1_wrong").ToString(), () => !ConversationManager.GetPersuasionProgressSatisfied(), null, this);



            dialogFlow.AddPlayerLine("quest_hellbound_id_7", "quest_hellbound_dialog_options_2", "quest_hellbound_dialog_outcome_2",
                "{=!}{PERSUADE_ATTEMPT_1}", PersuasionOptionCondition_1, PersuasionOptionConsequence_1, this, 100, null, () => _persuasionTask.Options.ElementAt(0));

            dialogFlow.AddPlayerLine("quest_hellbound_id_8", "quest_hellbound_dialog_options_2", "quest_hellbound_dialog_outcome_2",
                "{=!}{PERSUADE_ATTEMPT_2}", PersuasionOptionCondition_2, PersuasionOptionConsequence_2, this, 100, null, () => _persuasionTask.Options.ElementAt(1));


            dialogFlow.AddDialogLine("quest_hellbound_id_9", "quest_hellbound_dialog_outcome_2", "close_window",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_1").ToString(), ConversationManager.GetPersuasionProgressSatisfied, OnPersuasionComplete, this);

            dialogFlow.AddDialogLine("quest_hellbound_id_10", "quest_hellbound_dialog_outcome_2", "close_window",
                GameTexts.FindText("rf_fourth_quest_hellbound_dialog_npc_1_wrong").ToString(), () => !ConversationManager.GetPersuasionProgressSatisfied(), OnPersuasionComplete, this);


            return dialogFlow;

        }

        private void OnPersuasionComplete()
        {
            captureHellboundLog.UpdateCurrentProgress(2);
        }
        private void RestartPersuasionTask()
        {
            _persuasionTask = null;
            StartPersuasion(false);
        }
        private bool PersuasionOptionCondition_1()
        {
            if (this._persuasionTask.Options.Count > 0)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(this._persuasionTask.Options.ElementAt(0), false));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", this._persuasionTask.Options.ElementAt(0).Line);
                MBTextManager.SetTextVariable("PERSUADE_ATTEMPT_1", textObject, false);
                return true;
            }
            return false;
        }
        private bool PersuasionOptionCondition_2()
        {
            if (_persuasionTask.Options.Count > 1)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}");
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(_persuasionTask.Options.ElementAt(1), false));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", _persuasionTask.Options.ElementAt(1).Line);
                MBTextManager.SetTextVariable("PERSUADE_ATTEMPT_2", textObject);
                return true;
            }
            return false;
        }
        private void PersuasionOptionConsequence_1()
        {
            if (_persuasionTask.Options.Count > 0)
            {
                _persuasionTask.Options[0].BlockTheOption(true);
            }
        }
        private void PersuasionOptionConsequence_2()
        {
            if (_persuasionTask.Options.Count > 1)
            {
                _persuasionTask.Options[1].BlockTheOption(true);
            }
        }

        private PersuasionTask _persuasionTask;

        private void StartPersuasion(bool isFirstPersuasion)
        {
            PersuasionTask persuasionTask = new PersuasionTask(0);

            persuasionTask.FinalFailLine = GameTexts.FindText(isFirstPersuasion ? "rf_fourth_quest_hellbound_dialog_npc_1_wrong" : "rf_fourth_quest_hellbound_dialog_npc_2_wrong");
            persuasionTask.TryLaterLine = null;
            persuasionTask.SpokenLine = GameTexts.FindText(isFirstPersuasion ? "rf_fourth_quest_hellbound_dialog_2" : "rf_fourth_quest_hellbound_dialog_npc_1");

            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyEasy, true,
                GameTexts.FindText(isFirstPersuasion ? "rf_fourth_quest_hellbound_dialog_player_1" : "rf_fourth_quest_hellbound_dialog_player_2"), null, false, false, false);
            persuasionTask.AddOptionToTask(option);
            PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Negative, PersuasionArgumentStrength.ExtremelyHard, false,
                GameTexts.FindText(isFirstPersuasion ?"rf_fourth_quest_hellbound_dialog_player_1_wrong" : "rf_fourth_quest_hellbound_dialog_player_2_wrong"), null, false, false, false);
            persuasionTask.AddOptionToTask(option2);

            _persuasionTask = persuasionTask;

            ConversationManager.StartPersuasion(2f, 1f, 1f, 1f, 1f, 0f, PersuasionDifficulty.MediumHard);
        }
        private DialogFlow FirstDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_1"))
            .Condition(() => takeBossToLordLog?.CurrentProgress == 2 && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_5"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_6"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_7"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_8"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_9"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_10"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_11"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_13"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_15"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_first_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_first_dialog_17"))
            .Consequence(FirstDialogConsequence).CloseDialog();

        private DialogFlow UliahTableDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_1")).Condition(()=>Hero.OneToOneConversationHero == TheOwl && takeBossToLordLog?.CurrentProgress == 4)
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_owl_dialog_5")).Consequence(OwlGivesTableConsequence);

        private DialogFlow MonkDialogFlow => DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_1"))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "quest_monastery_priest" && goToMonasteryLog?.CurrentProgress == 0 && Settlement.CurrentSettlement == questMonastery)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_6"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_7"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_8"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_9")).Consequence(ShowWaitScreen)
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_10"))
            .PlayerLine(LineWithPlayerLink())
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_12"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_13"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_14"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_15"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_16"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_17"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_18"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_19")).Consequence(MonkDialogConsequence).CloseDialog();
    }
}
