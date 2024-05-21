using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using RealmsForgotten.Quest.MissionBehaviors;
using RealmsForgotten.Quest.UI;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
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
using FaceGen = TaleWorlds.Core.FaceGen;

namespace RealmsForgotten.Quest.SecondUpdate
{
    internal class FifthQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog talkToMonkLog;
        [SaveableField(1)]
        private JournalLog takeMysticalWeaponLog;
        [SaveableField(2)]
        private JournalLog talkToElveanKingLog;
        [SaveableField(3)]
        private JournalLog talkToHumanKingLog;
        [SaveableField(4)]
        private JournalLog talkToNasorianKingLog;
        [SaveableField(5)]
        private bool elveanKingPersuasionFailed;
        [SaveableField(6)]
        private JournalLog talkToMagicSellerLog;
        [SaveableField(7)] 
        private JournalLog requireTreasureLog;
        [SaveableField(8)] 
        private JournalLog deliverNelrogToNasorianLog;
        [SaveableField(9)] 
        private JournalLog deliverLordToAlKhuurLog;

        private Agent _treasureFightWinner;



        private bool _persuasionFailed;
        private const string MysticWeaponId = "rfmisc_mistic_polearm";
        private const string ShieldTreasureId = "ulvor_dec_shield";
        private const string TreasureFightCharacter = "allkhuur_goddess";
        private const string DevilsPartyTemplateId = "cs_devils_raiders_template";
        
        public override TextObject Title => GameTexts.FindText("rf_quest_title_part_five");
        public override bool IsRemainingTimeHidden => true;
        public override bool IsSpecialQuest => true;
        public static FifthQuest Instance { get; private set; }
        public FifthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            Instance = this;
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeave);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStart);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        private void OnWeeklyTick()
        {
            if (deliverNelrogToNasorianLog?.CurrentProgress == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    Hideout hideout = Hideout.All.GetRandomElement();
                    MobileParty party = BanditPartyComponent.CreateBanditParty("nelrogs", Clan.FindFirst(x => x.StringId == "cs_nelrog_raiders"),
                        null, true);
                    party.InitializeMobilePartyAroundPosition(
                        party.ActualClan.DefaultPartyTemplate,
                        hideout.Settlement.Position2D,
                        200f, 10f);
                }
            }
        }

        private void OnDailyTick()
        {
            Hideout hideout = Hideout.All.GetRandomElement();
            MobileParty party = BanditPartyComponent.CreateBanditParty("devils", Clan.FindFirst(x => x.StringId == "cs_devils_raiders"),
                null, true);
            party.InitializeMobilePartyAroundPosition(
                MBObjectManager.Instance.GetObject<PartyTemplateObject>(DevilsPartyTemplateId),
                hideout.Settlement.Position2D,
                200f, 10f);
        }


        private void OnMissionStart(IMission imission)
        {
            if (imission is Mission mission && Settlement.CurrentSettlement != null)
            {
                if (requireTreasureLog?.CurrentProgress == 0 && mission.Scene?.GetName() == "allkhuur_temple_inside")
                {
                    mission.AddMissionBehavior(new RecordDamageMissionLogic((victim, attacker, damage) =>
                    {
                        if ((victim.IsMainAgent || victim.Character?.StringId == TreasureFightCharacter) && damage >= victim.Health)
                            _treasureFightWinner = attacker;
                    }));
                    mission.AddMissionBehavior(new MissionConversationLogic());
                }
                mission.AddMissionBehavior(new FifthQuestMissionLogic());
            }
        }

        private bool talkedToElveanKing => talkToElveanKingLog?.CurrentProgress == 1 ||
                                           (talkToElveanKingLog?.CurrentProgress == 0 && elveanKingPersuasionFailed);
        private void OnTick(float dt)
        {
            if (talkToMonkLog == null || talkToHumanKingLog?.CurrentProgress == 1 || talkToMagicSellerLog?.CurrentProgress == 1 || talkedToElveanKing || deliverNelrogToNasorianLog?.CurrentProgress == 1)
            {
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
                if (talkedToElveanKing)
                {
                    talkToElveanKingLog.UpdateCurrentProgress(2);
                    talkToHumanKingLog = AddLog(GameTexts.FindText("rf_fifth_quest_fourth_objective"));
                }
            }
        }

        private void OnSettlementLeave(MobileParty mobileParty, Settlement settlement)
        {
            if (mobileParty.IsMainParty && takeMysticalWeaponLog?.CurrentProgress == 1)
            {
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
            }
        }
        
        protected override void OnStartQuest()
        {
            SetDialogs();
            Instance = this;
        }
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(OwlPersuasionFailedDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(ElveanKingFailedDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(FirstDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(SecondDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(ThirdDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(ElveanKingSucccessDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TreasureDialog_1, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TreasureDialog_2, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TreasureDialog_3, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TreasureFightDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(AfterConvinceThirdKingDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(DeliverRelicToHumanKingDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(DeliverNelrogToNasorianKingDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(ElveanKingPersuasionDialogFlow(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(NasorianKingPersuasionDialogFlow(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(HumanKingPersuasionDialogFlow(), this);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            Instance = this;
            Instance = this;
        }

        protected override void HourlyTick()
        {

        }
        
        
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
            .NpcLine(new TextObject("Wait here.")).Consequence(() => QuestUIManager.ShowNotification("After a while...", ()=>{}, false))
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
            .NpcLine(GameTexts.FindText("rf_fifth_quest_second_dialog_13"))
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
            .Consequence(() =>
            {
                talkToMonkLog.UpdateCurrentProgress(2);
                takeMysticalWeaponLog = AddLog(GameTexts.FindText("rf_fifth_quest_second_objective"));
            }).CloseDialog();
        
        
        
        private DialogFlow ThirdDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_third_dialog_1"))
            .Condition(() => takeMysticalWeaponLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_third_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_third_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_third_dialog_4"))
            .Consequence(() =>
            {   
                takeMysticalWeaponLog.UpdateCurrentProgress(2);
                talkToElveanKingLog = AddLog(GameTexts.FindText("rf_fifth_quest_third_objective"));
            }).CloseDialog();


        private DialogFlow ElveanKingSucccessDialog => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_fourth_dialog_1"))
            .Condition(() => talkToElveanKingLog?.CurrentProgress == 1 && !elveanKingPersuasionFailed && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_fourth_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_fourth_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_fourth_dialog_4"))
            .Consequence(() => talkToElveanKingLog.UpdateCurrentProgress(2)).CloseDialog();
        
        
        private DialogFlow OwlPersuasionFailedDialog => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(new TextObject("{OWL_ASSUMPTION}"))
            .Condition(() => _persuasionFailed)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_fourth_dialog_3"))
            .Consequence(() => _persuasionFailed = false).CloseDialog();

        private DialogFlow ElveanKingFailedDialog => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_fail_dialog_1")).Condition(() =>
                Hero.OneToOneConversationHero == TheOwl && talkToElveanKingLog?.CurrentProgress == 0 &&
                elveanKingPersuasionFailed)
            .PlayerLine(GameTexts.FindText("rf_i_agree"))
            .Consequence(() => talkToElveanKingLog.UpdateCurrentProgress(2)).CloseDialog();

        private DialogFlow TreasureDialog_1 => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_1"))
            .Condition(() => talkToHumanKingLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_2"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_3"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_4"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_5"))
            .Consequence(() =>
            {
                talkToHumanKingLog.UpdateCurrentProgress(2);
                talkToMagicSellerLog = AddLog(GameTexts.FindText("rf_fifth_quest_fifth_objective"));
            }).CloseDialog();
        
        private DialogFlow TreasureDialog_2 => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_vendor_treasure_1"))
            .Condition(() => talkToMagicSellerLog?.CurrentProgress == 0 && CharacterObject.OneToOneConversationCharacter?.StringId == "enchanted_vendor")
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_vendor_treasure_2"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_vendor_treasure_3"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_vendor_treasure_4"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_vendor_treasure_5"))
            .Consequence(() =>
            {
                talkToMagicSellerLog.UpdateCurrentProgress(1);
            }).CloseDialog();
        
        private DialogFlow TreasureDialog_3 => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_1_1"))
            .Condition(() => talkToMagicSellerLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_1_2"))
            .NpcLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_1_3"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_owl_treasure_1_4"))
            .Consequence(() =>
            {
                talkToMagicSellerLog.UpdateCurrentProgress(2);
                TextObject textObject = GameTexts.FindText("rf_fifth_quest_sixth_objective");
                Settlement settlement = Settlement.Find("castle_village_K2_2");
                textObject.SetTextVariable("CLOSEST_SETTLEMENT", settlement.EncyclopediaLinkWithName);
;                requireTreasureLog = AddLog(textObject);
            }).CloseDialog();

        
        private DialogFlow TreasureFightDialog => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_treasure_fight_1"))
            .Condition(() => requireTreasureLog?.CurrentProgress == 0 && Mission.Current != null && MissionConversationLogic.Current?.ConversationManager?.ConversationAgents[0] == FifthQuestMissionLogic.Instance?.TreasureFightAgent)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_treasure_fight_2"))
            .Consequence(StartTreasureFight).CloseDialog();
        
        
        private DialogFlow AfterConvinceThirdKingDialog => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_fifth_dialog_1"))
            .Condition(() => deliverNelrogToNasorianLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == TheOwl)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_fifth_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_fifth_dialog_3"))
            .Consequence(() =>
            {
                deliverNelrogToNasorianLog.UpdateCurrentProgress(2);
                deliverLordToAlKhuurLog = AddLog(GameTexts.FindText("rf_fifth_quest_ninth_objective"));
            }).CloseDialog();

        private bool CanDeliverNelrogsToNasorian()
        {
            bool haveNelrogInParty = MobileParty.MainParty?.PrisonRoster?.GetTroopRoster().Any(x => !x.Character.IsHero && x.Character?.Culture?.StringId == "giant_demons") == true;
            return deliverNelrogToNasorianLog?.CurrentProgress == 0 && haveNelrogInParty && Hero.OneToOneConversationHero.IsKingdomLeader &&
                   (Hero.OneToOneConversationHero.MapFaction as Kingdom)?.StringId == "vlandia";
        }
        private DialogFlow DeliverNelrogToNasorianKingDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_deliver_nelrog_1"))
            .Condition(CanDeliverNelrogsToNasorian)
            .NpcLine(GameTexts.FindText("rf_fifth_quest_deliver_nelrog_2"))
            .Consequence(() =>
            {
                deliverNelrogToNasorianLog.UpdateCurrentProgress(1);
            }).CloseDialog();
        
        private DialogFlow DeliverRelicToHumanKingDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fifth_quest_deliver_treasure_1"))
            .Condition(() => requireTreasureLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero?.IsKingdomLeader == true && (Hero.OneToOneConversationHero.MapFaction as Kingdom)?.StringId == "empire")
            .NpcLine(GameTexts.FindText("rf_fifth_quest_deliver_treasure_2"))
            .Consequence(() =>
            {
                CultureObject culture = Hero.OneToOneConversationHero.Culture;
                (CharacterObject, int)[] characterObjects = new[]
                {
                    (CharacterObject.All.GetRandomElementWithPredicate(x => !x.IsHero && x.IsSoldier && x.Culture == culture && x.DefaultFormationClass == FormationClass.Infantry && x.Tier == 3), 30),
                    (CharacterObject.All.GetRandomElementWithPredicate(x => !x.IsHero && x.IsSoldier && x.Culture == culture && x.DefaultFormationClass == FormationClass.Ranged && x.Tier == 2), 15),
                    (CharacterObject.All.GetRandomElementWithPredicate(x => !x.IsHero && x.IsSoldier && x.Culture == culture && (x.DefaultFormationClass == FormationClass.Cavalry || 
                        x.DefaultFormationClass == FormationClass.HeavyCavalry || x.DefaultFormationClass == FormationClass.LightCavalry) && x.Tier == 3), 10),
                };
                for (int i = 0; i < characterObjects.Length; i++)
                {
                    PartyBase.MainParty?.MemberRoster.AddToCounts(characterObjects[i].Item1, characterObjects[i].Item2);
                }
                requireTreasureLog.UpdateCurrentProgress(2);
                talkToNasorianKingLog = AddLog(GameTexts.FindText("rf_fifth_quest_seventh_objective"));
            }).CloseDialog();
        private DialogFlow ElveanKingPersuasionDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_1"))
                .Condition(()=> talkToElveanKingLog?.CurrentProgress == 0 && Hero.OneToOneConversationHero?.IsKingdomLeader == true && (Hero.OneToOneConversationHero.MapFaction as Kingdom)?.StringId == "battania")
                .GotoDialogState("quest_elvean_dialog_start");

            dialogFlow.AddPlayerLine("quest_elvean_id_1", "quest_elvean_dialog_start", "quest_elvean_dialog_1",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_2").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_2", "quest_elvean_dialog_1", "quest_elvean_dialog_2", 
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_3").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_3", "quest_elvean_dialog_2", "quest_elvean_dialog_3",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_4").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_4", "quest_elvean_dialog_3", "quest_elvean_dialog_4",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_5").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_5", "quest_elvean_dialog_4", "quest_elvean_dialog_5",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_6").ToString(), null, null, this);

            dialogFlow.AddDialogLine("quest_elvean_id_6", "quest_elvean_dialog_5", "quest_elvean_dialog_6",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_7").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_7", "quest_elvean_dialog_6", "quest_elvean_dialog_7_1",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_answer_1").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_8", "quest_elvean_dialog_6", "quest_elvean_dialog_7_2",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_answer_2").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_9", "quest_elvean_dialog_7_1", "quest_elvean_dialog_8",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_reply_1").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_10", "quest_elvean_dialog_7_2", "quest_elvean_dialog_8",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_reply_2").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_11", "quest_elvean_dialog_8", "quest_elvean_dialog_9_1",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_answer_3_wrong").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_12", "quest_elvean_dialog_8", "quest_elvean_dialog_9_2",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_answer_4").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_13", "quest_elvean_dialog_9_1", "quest_elvean_dialog_9_wrong",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_reply_3").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_14", "quest_elvean_dialog_9_2", "quest_elvean_dialog_10",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_reply_4").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_15", "quest_elvean_dialog_9_wrong", "close_window",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_answer_5_wrong").ToString(), null, () =>
                {
                    elveanKingPersuasionFailed = true;
                }, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_16", "quest_elvean_dialog_10", "quest_elvean_dialog_11",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_answer_6").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_17", "quest_elvean_dialog_11", "quest_elvean_dialog_12",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_8").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_elvean_id_18", "quest_elvean_dialog_12", "quest_elvean_dialog_13",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_9").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_elvean_id_19", "quest_elvean_dialog_13", "close_window",
                GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_10").ToString(), null, () =>
                {
                    talkToElveanKingLog.UpdateCurrentProgress(1);
                    if(PlayerEncounter.EncounteredMobileParty != null)
                        PlayerEncounter.Finish();
                }, this);

            return dialogFlow;
        }
        private DialogFlow HumanKingPersuasionDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_1"))
                .Condition(()=> talkToHumanKingLog?.CurrentProgress == 0 && Hero.OneToOneConversationHero?.IsKingdomLeader == true && (Hero.OneToOneConversationHero.MapFaction as Kingdom)?.StringId == "empire")
                .GotoDialogState("quest_belvor_dialog_start");

            dialogFlow.AddPlayerLine("quest_belvor_id_1", "quest_belvor_dialog_start", "quest_belvor_dialog_1",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_2").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_belvor_id_2", "quest_belvor_dialog_1", "quest_belvor_dialog_2", 
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_3").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_belvor_id_3_1", "quest_belvor_dialog_2", "quest_belvor_dialog_4",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_4").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_belvor_id_3_2", "quest_belvor_dialog_4", "quest_belvor_dialog_5",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_5").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_belvor_id_4", "quest_belvor_dialog_5", "quest_belvor_dialog_6",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_6").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_belvor_id_5", "quest_belvor_dialog_6", "quest_belvor_dialog_7",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_7").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("quest_belvor_id_6", "quest_belvor_dialog_7", "quest_belvor_dialog_7_1",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_7_1").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_belvor_id_7", "quest_belvor_dialog_7", "quest_belvor_dialog_7_2",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_7_2").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_belvor_id_8", "quest_belvor_dialog_7_1", "quest_belvor_dialog_8",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_8_1").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_belvor_id_9", "quest_belvor_dialog_7_2", "quest_belvor_dialog_8",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_8_2").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_belvor_id_10", "quest_belvor_dialog_8", "quest_belvor_dialog_9_1",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_9_1").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_belvor_id_11", "quest_belvor_dialog_8", "quest_belvor_dialog_9_2",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_9_2").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_belvor_id_12", "quest_belvor_dialog_9_1", "quest_belvor_dialog_10",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_10_1").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_belvor_id_13", "quest_belvor_dialog_9_2", "quest_belvor_dialog_10",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_10_2").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_belvor_id_14", "quest_belvor_dialog_10", "quest_belvor_dialog_11",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_11").ToString(), null, null, this);

            dialogFlow.AddDialogLine("quest_belvor_id_15", "quest_belvor_dialog_11", "close_window",
                GameTexts.FindText("rf_fifth_quest_belvor_king_dialog_12").ToString(), null, () =>
                { 
                    talkToHumanKingLog.UpdateCurrentProgress(1);
                    if(PlayerEncounter.EncounteredMobileParty != null)
                        PlayerEncounter.Finish();
                }, this);

            return dialogFlow;
        }
        
        private DialogFlow NasorianKingPersuasionDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(GameTexts.FindText("rf_fifth_quest_elvean_king_dialog_1"))
                .Condition(()=> talkToNasorianKingLog?.CurrentProgress == 0 && Hero.OneToOneConversationHero?.IsKingdomLeader == true && (Hero.OneToOneConversationHero.MapFaction as Kingdom)?.StringId == "vlandia")
                .GotoDialogState("quest_nasorian_dialog_start");

            dialogFlow.AddPlayerLine("quest_nasorian_id_1", "quest_nasorian_dialog_start", "quest_nasorian_dialog_1",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_2").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_2", "quest_nasorian_dialog_1", "quest_nasorian_dialog_2", 
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_3").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_nasorian_id_3_1", "quest_nasorian_dialog_2", "quest_nasorian_dialog_3",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_4_1").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_nasorian_id_3_2", "quest_nasorian_dialog_3", "quest_nasorian_dialog_4",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_4_2").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_4", "quest_nasorian_dialog_4", "quest_nasorian_dialog_5",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_5").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_5", "quest_nasorian_dialog_5", "quest_nasorian_dialog_6",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_6").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("quest_nasorian_id_6", "quest_nasorian_dialog_6", "quest_nasorian_dialog_7",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_7").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_7", "quest_nasorian_dialog_7", "quest_nasorian_dialog_7_1",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_answer_1").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_nasorian_id_8", "quest_nasorian_dialog_7", "quest_nasorian_dialog_7_2",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_answer_2_wrong").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_9", "quest_nasorian_dialog_7_1", "quest_nasorian_dialog_8",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_reply_1").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_10", "quest_nasorian_dialog_7_2", "quest_nasorian_dialog_8",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_reply_2").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_nasorian_id_11", "quest_nasorian_dialog_8", "quest_nasorian_dialog_9_1",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_answer_3").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_nasorian_id_12", "quest_nasorian_dialog_8", "quest_nasorian_dialog_9_2",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_answer_4").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_13", "quest_nasorian_dialog_9_1", "quest_nasorian_dialog_10",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_reply_3").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_14", "quest_nasorian_dialog_9_2", "quest_nasorian_dialog_10",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_reply_4").ToString(), null, null, this);
            
            dialogFlow.AddPlayerLine("quest_nasorian_id_15", "quest_nasorian_dialog_10", "quest_nasorian_dialog_11",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_9").ToString(), null, null, this);
            
            dialogFlow.AddDialogLine("quest_nasorian_id_16", "quest_nasorian_dialog_11", "close_window",
                GameTexts.FindText("rf_fifth_quest_nasorian_king_dialog_10").ToString(), null, () =>
                {
                    talkToNasorianKingLog?.UpdateCurrentProgress(1);
                    deliverNelrogToNasorianLog = AddLog(GameTexts.FindText("rf_fifth_quest_eighth_objective"));
                }, this);

            return dialogFlow;
        }
        private void StartTreasureFight()
        {
            Instance?.requireTreasureLog?.UpdateCurrentProgress(1);    

            List<Agent> enemyAgents = new();

            Agent enemyAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0];
            enemyAgents.Add(enemyAgent);

            Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(new (){Agent.Main}, enemyAgents, false, false, delegate (bool isPlayerSideWon)
            {
                if (_treasureFightWinner == Agent.Main)
                {
                    OnTreasureFightWin();
                }
            });

        }

        private void OnTreasureFightWin()
        {
            requireTreasureLog.UpdateCurrentProgress(1);
            PartyBase.MainParty.ItemRoster.AddToCounts(
                MBObjectManager.Instance.GetObject<ItemObject>(ShieldTreasureId), 1);
        }

        private class FifthQuestMissionLogic : MissionLogic
        {
            public Agent TreasureFightAgent;
            public static FifthQuestMissionLogic Instance { get; private set; }
            public FifthQuestMissionLogic()
            {
                Instance = this;
            }
            public override void AfterStart()
            {
                base.AfterStart();
                if (FifthQuest.Instance?.takeMysticalWeaponLog?.CurrentProgress == 0 && Mission.Scene?.GetName() == "ice_tower_inside")
                {
                    Vec3 position = new(122.42f, 192.97f, 4.03f),
                        rotation = new(0.00f, 0.00f, -30.00f);
                    MissionWeapon missionWeapon = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>(MysticWeaponId), new ItemModifier(), Banner.CreateOneColoredEmptyBanner(1));
                    Mission.SpawnWeaponWithNewEntityAux(missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics, new MatrixFrame(Mat3.CreateMat3WithForward(rotation), position), 0, null, false);
                }
                else if (FifthQuest.Instance?.requireTreasureLog?.CurrentProgress == 0 && Mission.Scene?.GetName() == "allkhuur_temple_inside")
                {
                    Vec3 position = new(138.62f, 161.10f, 23.00f, -1f),
                        rotation = new(0.00f, 0.00f, 177.79f);
                    
                    MissionWeapon missionWeapon = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>(ShieldTreasureId), new ItemModifier(), Banner.CreateOneColoredEmptyBanner(1));
                    Mission.SpawnWeaponWithNewEntityAux(missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics, new MatrixFrame(Mat3.CreateMat3WithForward(rotation), position), 0, null, false);
                }
                Mission.Current.OnItemPickUp += OnItemPickup;
            }

            private void OnItemPickup(Agent agent, SpawnedItemEntity spawnedItemEntity)
            {
                if (agent.IsMainAgent)
                {
                    if(FifthQuest.Instance?.takeMysticalWeaponLog?.CurrentProgress == 0 && spawnedItemEntity.WeaponCopy.Item?.StringId == MysticWeaponId)
                        FifthQuest.Instance?.takeMysticalWeaponLog?.UpdateCurrentProgress(1);
                    if (FifthQuest.Instance?.requireTreasureLog?.CurrentProgress == 0 && spawnedItemEntity.WeaponCopy.Item?.StringId == ShieldTreasureId)
                    {
                        CharacterObject characterObject = CharacterObject.Find(TreasureFightCharacter);
                        Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(characterObject.Race, FaceGen.MonsterSuffixSettlement);
                        Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(characterObject, true);
                        
                        AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(characterObject)).Equipment(randomEquipmentElements)
                            .Monster(monsterWithSuffix);
                        Vec3 initialPosition = new Vec3(138.69f, 157.79f, 23.82f);
                        agentBuildData.InitialPosition(in initialPosition).InitialDirection(agent.Position.AsVec2);
                        
                        TreasureFightAgent = Mission.Current.SpawnAgent(agentBuildData, true);
                        TreasureFightAgent.TeleportToPosition(initialPosition);
                        Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(TreasureFightAgent, false);
                    }
                }
            }
        }
    }
}
