﻿using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using HarmonyLib;
using TaleWorlds.TwoDimension;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;
using TaleWorlds.CampaignSystem.Roster;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.InputSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using System.Collections;
using System.Timers;

namespace Quest
{
    [HarmonyPatch(typeof(Hero), "CanHaveQuestsOrIssues")]
    static class CanHaveQuestsOrIssuesPatch
    {
        
        [HarmonyPostfix]
        static void Postfix(ref bool __result, Hero __instance)
        {
            if(Kingdom.All.First(x => x.StringId == "empire").Leader.Spouse.HomeSettlement.Notables[0].StringId == __instance.StringId)
                __result = false;
        }
    }

    public class RescueUliahBehavior : CampaignBehaviorBase
    {
        private static CampaignGameStarter gameStarter;
        private static readonly string uliahId = "questGiver";
        private static readonly int prisonersAmount = 5;
        private bool isNewGame;
        private static Hero Uliah
        {
            get { return Hero.AllAliveHeroes.Find(x=>x.StringId==uliahId); }
        }

        private static Hero Queen
        {
            get { return Kingdom.All.First(x => x.StringId == "empire").Leader.Spouse; }
        }
        private static bool hideoutDefeated = false;
        private static Vec2 hideoutPosition = Vec2.Invalid;
        public RescueUliahBehavior(CampaignGameStarter _gameStarter, bool isNewGame)
        {
            gameStarter = _gameStarter;
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, (CampaignGameStarter cgs) =>
            {
                RegisterEvents();
            });
            this.isNewGame = isNewGame;
        }
        private void HandleBandits()
        {
            new RescueUliahQuest("rescue_uliah_quest", Uliah, CampaignTime.Never, 0).StartQuest();
            Hideout nearestHideout = Hideout.All.Find(x => x.StringId == "hideout_mountain_7");
            if (!nearestHideout.IsInfested)
            {
          
                Clan clan = Clan.All.Find(x=>x.StringId== "mountain_bandits");
                for(int i = 0; i <= 2; i++)
                {
                    MobileParty bandits = BanditPartyComponent.CreateBanditParty("bandits_quest_" + i, clan, nearestHideout, i==2 ? true : false);
                    bandits.InitializeMobilePartyAtPosition(clan.DefaultPartyTemplate, nearestHideout.Settlement.Position2D);
                    bandits.Ai.SetMoveGoToSettlement(nearestHideout.Settlement);
                    bandits.Ai.RecalculateShortTermAi();
                    EnterSettlementAction.ApplyForParty(bandits, nearestHideout.Settlement);
                }
                AccessTools.Field(typeof(Hideout), "_nextPossibleAttackTime").SetValue(nearestHideout, CampaignTime.Now);
                
                nearestHideout.IsSpotted = true;


            }
            nearestHideout.Settlement.IsVisible = true;
            hideoutPosition = nearestHideout.Settlement.Position2D;
            
        }

        public void AfterLoad()
        {
            
            if (Uliah == null)
            {
                Queen.HomeSettlement.Notables[0].StringId = uliahId;
                Queen.HomeSettlement.Notables[0].StringId = uliahId;
                if (Queen.HomeSettlement.Notables[0].IsFemale)
                    Queen.HomeSettlement.Notables[0].UpdatePlayerGender(false);
                Queen.HomeSettlement.Notables[0].SetName(new TextObject("Uliah"), new TextObject("Uliah"));
                Queen.HomeSettlement.Notables[0].SetHasMet();
                StartQuest();
                
            }

        }

        public override void RegisterEvents()
        {
            //Check if this quest has already started
            if (!Campaign.Current.QuestManager.Quests.Any(x => x.GetType() == typeof(RescueUliahQuest)))
            {
                
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickForNewGame);
            }
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, AfterLoad);
            CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutDefeat);

        }
        private void HourlyTickForNewGame()
        {
            if (isNewGame)
            {
                Queen.HomeSettlement.Notables[0].StringId = uliahId;
                Queen.HomeSettlement.Notables[0].StringId = uliahId;
                if (Queen.HomeSettlement.Notables[0].IsFemale)
                    Queen.HomeSettlement.Notables[0].UpdatePlayerGender(false);
                Queen.HomeSettlement.Notables[0].SetName(new TextObject("Uliah"), new TextObject("Uliah"));
                Queen.HomeSettlement.Notables[0].SetHasMet();
                StartQuest();
                isNewGame = false;
                hideoutDefeated = false;
            }
        }
        private void OnHideoutDefeat(BattleSideEnum side, HideoutEventComponent hideout)
        {
            if (hideout.MapEvent != null && side == BattleSideEnum.Attacker && hideout.MapEvent.MapEventSettlement.StringId== "hideout_mountain_7" && !hideoutDefeated)
            {
                hideoutDefeated = true;
                AddHeroToPartyAction.Apply(Uliah, MobileParty.MainParty, true);
            }
        }
        private void StartQuest()
        {
            InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_main_quest_start_inquiry_1").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", () =>
            {
                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_main_quest_start_inquiry_2").ToString(), true, true, new TextObject("{=proceed}Proceed").ToString(), new TextObject("{=ignore}Ignore").ToString(), () =>
                {
                    InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_main_quest_start_inquiry_4").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", HandleBandits, null), true);
                }, () =>
                {
                    InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_main_quest_start_inquiry_3").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", null, null), true);
                }), true);

            }, null), true);
        }
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("hideoutDefeated", ref hideoutDefeated);
        }

                       
        internal class RescueUliahQuest : QuestBase
        {
            [SaveableField(1)]
            private bool _refusedQueenMapQuest = false;
            [SaveableField(2)]
            private bool _thirdTextBox = false;
            [SaveableField(3)]
            private bool _hiddenHandSpawned = false;
            [SaveableField(4)]
            private bool _smallPlayerArmyPendentQuest = false;
            [SaveableField(5)]
            private JournalLog? _bringZombiesJournalLog;
            [SaveableField(6)]
            private JournalLog? _smallPlayerArmyJournalLog;
            [SaveableField(7)]
            private CampaignTime _deliveredToAlchemistsTime;

            private readonly int minimumSoldiersAmountForQuest = 50;

            public RescueUliahQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
            {
                this.SetDialogs();
                base.InitializeQuestOnCreation();
                this.AddLog(GameTexts.FindText("rf_first_quest_objective_1"));
                Hideout hideout = Hideout.All.Find(x => x.StringId == "hideout_mountain_7");
                this.AddTrackedObject((ITrackableCampaignObject)hideout.Settlement);
                _deliveredToAlchemistsTime = CampaignTime.Zero;
            }
            public override bool IsSpecialQuest => true;


            public override TextObject Title => GameTexts.FindText("rf_first_quest_title");


            protected override void RegisterEvents()
            {
                CampaignEvents.MapEventStarted.AddNonSerializedListener(this, this.OnMapEventStarted);
                CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, MakePartyEngage);
                CampaignEvents.OnPartySizeChangedEvent.AddNonSerializedListener(this, (PartyBase party) =>
                {
                    if (_smallPlayerArmyPendentQuest && party == PartyBase.MainParty && _smallPlayerArmyJournalLog!= null && !_smallPlayerArmyJournalLog.HasBeenCompleted())
                    {
                        _smallPlayerArmyJournalLog.UpdateCurrentProgress(party.NumberOfAllMembers);
                    }
                });
            }

            private void MakePartyEngage(PartyBase party)
            {
                if (!_hiddenHandSpawned
                    && _bringZombiesJournalLog != null && _bringZombiesJournalLog.HasBeenCompleted())
                {
                    this.AddLog(GameTexts.FindText("rf_first_quest_objective_4"));
                    Clan clan = Clan.FindFirst(x => x.StringId == "hidden_hand");
                    Hero hero = clan.Heroes.GetRandomElement();
                    MobileParty hiddenHandParty = MobileParty.AllLordParties.First(x => x.ActualClan == clan) ?? LordPartyComponent.CreateLordParty("attacker_party_quest", hero, MobileParty.MainParty.Position2D, 1f, Queen.HomeSettlement, hero);
                    hiddenHandParty.StringId = "attacker_party_quest";


                    hiddenHandParty.InitializeMobilePartyAroundPosition(clan.DefaultPartyTemplate, MobileParty.MainParty.Position2D, 0.3f, 0.1f, 100);
                    
                        
                    SetPartyAiAction.GetActionForEngagingParty(hiddenHandParty, MobileParty.MainParty);
                    
                    hiddenHandParty.Ai.RecalculateShortTermAi();
                    hiddenHandParty.IgnoreByOtherPartiesTill(CampaignTime.Hours(1));
                    _hiddenHandSpawned = true;
                }
            }

            private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
            {
                if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
                {
                    QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
                }
            }

            public override bool IsRemainingTimeHidden => true;

            protected override void InitializeQuestOnGameLoad()
            {
                this.SetDialogs();
            }
            protected override void SetDialogs()
            {

                this.QuestCharacterDialogFlow = DialogFlow.CreateDialogFlow("start", 125).NpcLine(GameTexts.FindText("rf_uliah_text_1").ToString(), null, null)
                .Condition(() => Hero.OneToOneConversationHero == Uliah && this.JournalEntries.Count == 1).BeginPlayerOptions()
                .PlayerOption(GameTexts.FindText("rf_uliah_text_2").ToString(), null).NpcLine(GameTexts.FindText("rf_uliah_text_3").ToString(), null, null).GotoDialogState("start").PlayerOption(GameTexts.FindText("rf_uliah_text_4").ToString())
                .NpcLine(GameTexts.FindText("rf_uliah_text_5").ToString()).NpcLine(GameTexts.FindText("rf_uliah_text_8").ToString(), null, null).Consequence(this.QuestAcceptedConsequences).CloseDialog()
                .PlayerOption(GameTexts.FindText("rf_uliah_text_5").ToString(),
                null).NpcLine(GameTexts.FindText("rf_uliah_text_7").ToString(), null, null)
                .NpcLine(GameTexts.FindText("rf_uliah_text_8").ToString(), null, null).Consequence(this.QuestAcceptedConsequences).CloseDialog()
                .EndPlayerOptions();
                Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_first_quest_queen_small_player_army_1")).Condition(() => Hero.OneToOneConversationHero == Queen && _bringZombiesJournalLog == null && _smallPlayerArmyPendentQuest)
                    .BeginNpcOptions().NpcOption(GameTexts.FindText("rf_first_quest_queen_text_2").ToString(), () => PartyBase.MainParty?.NumberOfAllMembers >= minimumSoldiersAmountForQuest && _smallPlayerArmyPendentQuest)
                    .BeginPlayerOptions().PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_4").ToString()).NpcLine(GameTexts.FindText("rf_first_quest_queen_text_3").ToString()).BeginPlayerOptions().PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_7").ToString()).NpcLine(GameTexts.FindText("rf_first_quest_queen_text_5").ToString()).Consequence(AddSecondPhase)
                    .CloseDialog().PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_8").ToString()).Consequence(() => Success(100000)).CloseDialog().EndPlayerOptions().CloseDialog()
                    .PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_6").ToString()).Consequence(() => Success(100000)).CloseDialog().EndPlayerOptions().NpcOption(GameTexts.FindText("rf_first_quest_queen_small_player_army_2").ToString(), () => PartyBase.MainParty?.NumberOfAllMembers < minimumSoldiersAmountForQuest && _smallPlayerArmyPendentQuest).PlayerLine(GameTexts.FindText("rf_ok")).CloseDialog().EndNpcOptions());
                Campaign.Current.ConversationManager.AddDialogFlow(this.PlayerDeliverPrisonersToQueen(), this);
                Campaign.Current.ConversationManager.AddDialogFlow(this.SetSecondPhaseHiddenHandDialog(), this);
                Campaign.Current.ConversationManager.AddDialogFlow(this.SetSecondPhaseHiddenHandDialog2(), this);
                Campaign.Current.ConversationManager.AddDialogFlow(this.SetQueenDialog(), this); 
                    Campaign.Current.ConversationManager.AddDialogFlow(this.SetOwlDialog(), this);

            }

            private void PlayerDeliverPrisonersToHiddenHand(Hero giver)
            {
                
                giver.PartyBelongedTo.Position2D = giver.HomeSettlement.Position2D;
                giver.PartyBelongedTo.Ai.SetMovePatrolAroundSettlement(giver.HomeSettlement);
                giver.PartyBelongedTo.Ai.RecalculateShortTermAi();

                
                List<TroopRosterElement> prisoners = MobileParty.MainParty.PrisonRoster.GetTroopRoster()
                    .Where(x => !x.Character.IsHero && x.Character?.Culture?.StringId == "sea_raiders").ToList();
                
                if (prisoners != null && !prisoners.IsEmpty())

                {
                    foreach (TroopRosterElement prisoner in prisoners)
                    {
                        for (int i = 0; i < prisoner.Number; i++)
                        {
                            TransferPrisonerAction.Apply(prisoner.Character, PartyBase.MainParty, giver.PartyBelongedTo.Party);
                        }
                    }
                }

                _deliveredToAlchemistsTime = CampaignTime.Now;
            }
            private void SpawnOwlParty()
            {
                Clan clan = Clan.FindFirst(x => x.StringId == "clan_empire_north_7");
                Hero hero = HeroCreator.CreateSpecialHero(
                    MBObjectManager.Instance.GetObject<CharacterObject>(QueenQuest._theOwlId), QuestGiver.HomeSettlement, clan,
                    clan, 30);
                hero.StringId = "the_owl_hero";
                hero.CharacterObject.StringId = "the_owl_hero";


                MobileParty mobileParty = LordPartyComponent.CreateLordParty("owl_party", hero, MobileParty.MainParty.Position2D, 1f, QuestGiver.HomeSettlement, hero);
                mobileParty.MemberRoster.RemoveIf(x=>x.Character.HeroObject?.StringId != hero.StringId);

                mobileParty.InitializeMobilePartyAroundPosition(clan.DefaultPartyTemplate, MobileParty.MainParty.Position2D, 1f, 0, 0);
                mobileParty.StringId = "owl_party";
                mobileParty.Ai.SetMoveEngageParty(MobileParty.MainParty);
                mobileParty.IgnoreForHours(1);
            }
            private void DeliverPrisonersToQueen()
            {
                List<TroopRosterElement> prisoners = MobileParty.MainParty.PrisonRoster.GetTroopRoster()
                    .Where(x => !x.Character.IsHero && x.Character?.Culture?.StringId == "sea_raiders").ToList();

                if (prisoners != null && !prisoners.IsEmpty())

                {
                    foreach (TroopRosterElement prisoner in prisoners)
                    {
                        for (int i = 0; i < prisoner.Number; i++)
                        {
                            TransferPrisonerAction.Apply(prisoner.Character, PartyBase.MainParty, Queen.CurrentSettlement?.Party ?? Queen.PartyBelongedTo.Party);
                        }
                    }
                }
            }


            private bool PlayerHaveprisoners => Hero.OneToOneConversationHero == Queen && _bringZombiesJournalLog != null && _bringZombiesJournalLog.HasBeenCompleted();
            private void QuestAcceptedConsequences()
            {
                TextObject questObjectiveText = GameTexts.FindText("rf_first_quest_objective_2");
                questObjectiveText.SetCharacterProperties("QUEEN", Queen.CharacterObject, false);
                this.AddLog(questObjectiveText);
            }
            private void AddSecondPhase()
            {
                GiveGoldAction.ApplyBetweenCharacters(Queen, Hero.MainHero, 10000, false);
                TextObject questObjectiveText = GameTexts.FindText("rf_first_quest_objective_3");
                questObjectiveText.SetCharacterProperties("QUEEN", Queen.CharacterObject, false);
                _bringZombiesJournalLog = this.AddDiscreteLog(questObjectiveText, GameTexts.FindText("rf_first_quest_objective_3_task"), 0, prisonersAmount);

                if (MobileParty.MainParty.MemberRoster.GetTroopRoster()
                    .Any(x => x.Character?.HeroObject?.StringId == uliahId))
                {
                    if (Queen.PartyBelongedTo != null)
                        AddHeroToPartyAction.Apply(Uliah, Queen.PartyBelongedTo);
                    else
                        AddHeroToPartyAction.Apply(MobileParty.MainParty.MemberRoster.GetTroopRoster().First(x => x.Character?.HeroObject?.StringId == uliahId).Character?.HeroObject, Queen.HomeSettlement.Parties[0]);
                }

            }

            public override int GetCurrentProgress()
            {
                return MobileParty.MainParty.PrisonRoster.GetTroopRoster().Where(x => !x.Character.IsHero && x.Character?.Culture?.StringId == "sea_raiders").Sum(x => x.Number);
            }


            private void Success(int reward)
            {
                base.CompleteQuestWithSuccess();
                
                TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[]
                {
                    new Tuple<TraitObject, int>(DefaultTraits.Mercy, 50),
                    new Tuple<TraitObject, int>(DefaultTraits.Generosity, 30)
                });
                GiveGoldAction.ApplyBetweenCharacters(Queen, Hero.MainHero, reward, false);
                this.RelationshipChangeWithQuestGiver = 10;
                ChangeRelationAction.ApplyPlayerRelation(Queen, 10, true, true);
                ChangeRelationAction.ApplyPlayerRelation(this.QuestGiver, RelationshipChangeWithQuestGiver, true, true);

                if (Queen.PartyBelongedTo != null)
                    AddHeroToPartyAction.Apply(Uliah, Queen.PartyBelongedTo);
                else
                    TeleportHeroAction.ApplyImmediateTeleportToSettlement(Uliah, Queen.CurrentSettlement);
            }

            private void SmallPlayerArmyConsequence()
            {
                if (Queen.PartyBelongedTo != null)
                    AddHeroToPartyAction.Apply(Uliah, Queen.PartyBelongedTo);
                else
                    AddHeroToPartyAction.Apply(MobileParty.MainParty.MemberRoster.GetTroopRoster().First(x => x.Character?.HeroObject?.StringId == uliahId).Character?.HeroObject, Queen.HomeSettlement.Parties[0]);

                GiveGoldAction.ApplyBetweenCharacters(Queen, Hero.MainHero, 10000, false);
                _smallPlayerArmyPendentQuest = true;
                _smallPlayerArmyJournalLog = this.AddDiscreteLog(SmallPlayerArmyText,
                    GameTexts.FindText("rf_soldiers_in_army"), PartyBase.MainParty.NumberOfHealthyMembers, minimumSoldiersAmountForQuest); 
                
                
            }
            private TextObject SmallPlayerArmyText
            { 
                get
                {
                    TextObject textObject = GameTexts.FindText("rf_small_player_army_log");
                    textObject.SetTextVariable("MINIMUM_AMOUNT", minimumSoldiersAmountForQuest);
                    return textObject;
                } 
            }
            private void StartSecondQuest()
            {
                this.CompleteQuestWithSuccess();
                GiveGoldAction.ApplyBetweenCharacters(Queen, Hero.MainHero, 20000, false);
                new QueenQuest("rf_queen_quest", Queen, CampaignTime.Never, 50000, false).StartQuest();
            }
            private DialogFlow SetQueenDialog() => DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_first_quest_queen_text_1").ToString())
         .Condition(() => MobileParty.MainParty.MemberRoster.GetTroopRoster().Any(x => x.Character.HeroObject?.StringId == uliahId) && Hero.OneToOneConversationHero == Queen && _bringZombiesJournalLog == null && !_smallPlayerArmyPendentQuest).BeginNpcOptions().NpcOption(GameTexts.FindText("rf_first_quest_queen_text_2").ToString(), () => PartyBase.MainParty?.NumberOfAllMembers >= minimumSoldiersAmountForQuest)
         .BeginPlayerOptions().PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_4").ToString()).NpcLine(GameTexts.FindText("rf_first_quest_queen_text_3").ToString()).BeginPlayerOptions().PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_7").ToString()).NpcLine(GameTexts.FindText("rf_first_quest_queen_text_5").ToString()).Consequence(AddSecondPhase)
         .CloseDialog().PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_8").ToString()).Consequence(() => Success(100000)).CloseDialog().EndPlayerOptions().CloseDialog()
         .PlayerOption(GameTexts.FindText("rf_first_quest_queen_text_6").ToString()).Consequence(() => Success(100000)).CloseDialog().EndPlayerOptions().NpcOption(GameTexts.FindText("rf_first_quest_queen_small_player_army").ToString(), () => PartyBase.MainParty?.NumberOfAllMembers < minimumSoldiersAmountForQuest).PlayerLine(GameTexts.FindText("rf_ok")).Consequence(SmallPlayerArmyConsequence).CloseDialog().EndNpcOptions();

            private DialogFlow SetSecondPhaseHiddenHandDialog()
            {
                DialogFlow HiddenHandDialog = DialogFlow.CreateDialogFlow("start", 125);

                HiddenHandDialog.AddDialogLine("hidden_hand_dialog_id_1", "start", "hidden_hand_dialog_1", GameTexts.FindText("rf_first_quest_hidden_hand_text_1").ToString(), () => MobileParty.ConversationParty?.ActualClan?.StringId == "hidden_hand" && Hero.OneToOneConversationHero?.PartyBelongedTo != null
                    && MobileParty.MainParty.PrisonRoster.GetTroopRoster().Where(x => !x.Character.IsHero && x.Character?.Culture?.StringId == "sea_raiders").Sum(x => x.Number) >= prisonersAmount, null, this, 125);

                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_2", "hidden_hand_dialog_1", "hidden_hand_dialog_2", GameTexts.FindText("rf_first_quest_hidden_hand_text_2").ToString(), null, null, this, 125);
                HiddenHandDialog.AddDialogLine("hidden_hand_dialog_id_3", "hidden_hand_dialog_2", "hidden_hand_dialog_3", GameTexts.FindText("rf_first_quest_hidden_hand_text_3").ToString(), null, null, this, 125);
                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_4", "hidden_hand_dialog_3", "hidden_hand_dialog_4", GameTexts.FindText("rf_first_quest_hidden_hand_text_4").ToString(), null, null, this, 125);
                HiddenHandDialog.AddDialogLine("hidden_hand_dialog_id_5", "hidden_hand_dialog_4", "hidden_hand_dialog_5", GameTexts.FindText("rf_first_quest_hidden_hand_text_5").ToString(), null, null, this, 125);

                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_6", "hidden_hand_dialog_5", "close_window", GameTexts.FindText("rf_first_quest_hidden_hand_text_6").ToString(), null, null, this, 125);
                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_7", "hidden_hand_dialog_5", "hidden_hand_dialog_6", GameTexts.FindText("rf_first_quest_hidden_hand_text_7").ToString(), null, () => { PlayerEncounter.Finish(); GiveGoldAction.ApplyBetweenCharacters(Hero.OneToOneConversationHero, Hero.MainHero, 10000, false); }, this, 125);
                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_8", "hidden_hand_dialog_5", "close_window", GameTexts.FindText("rf_first_quest_hidden_hand_text_8").ToString(), null, null, this, 125);
                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_9", "hidden_hand_dialog_5", "hidden_hand_dialog_6", GameTexts.FindText("rf_first_quest_hidden_hand_text_9").ToString(), null, () => { PlayerEncounter.Finish(); GiveGoldAction.ApplyBetweenCharacters(Hero.OneToOneConversationHero, Hero.MainHero, 10000, false); }, this, 125);
                
                HiddenHandDialog.AddDialogLine("hidden_hand_dialog_id_10", "hidden_hand_dialog_6", "hidden_hand_dialog_7", GameTexts.FindText("rf_first_quest_hidden_hand_text_10").ToString(), null, null, this, 125);
                HiddenHandDialog.AddDialogLine("hidden_hand_dialog_id_11", "hidden_hand_dialog_7", "hidden_hand_dialog_8", GameTexts.FindText("rf_first_quest_hidden_hand_text_11").ToString(), null, null, this, 125);
                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_12", "hidden_hand_dialog_8", "hidden_hand_dialog_9", GameTexts.FindText("rf_first_quest_hidden_hand_text_12").ToString(), null, null, this, 125);
                HiddenHandDialog.AddDialogLine("hidden_hand_dialog_id_13", "hidden_hand_dialog_9", "hidden_hand_dialog_10", GameTexts.FindText("rf_first_quest_hidden_hand_text_13").ToString(), null, null, this, 125);
                HiddenHandDialog.AddPlayerLine("hidden_hand_dialog_id_14", "hidden_hand_dialog_10", "close_window", GameTexts.FindText("rf_ok").ToString(), null, () => { PlayerDeliverPrisonersToHiddenHand(Hero.OneToOneConversationHero); }, this, 125);
                return HiddenHandDialog;
            }

            private DialogFlow SetOwlDialog() => DialogFlow.CreateDialogFlow("start", 125).NpcLine(GameTexts.FindText("rf_the_owl_after_alchemist_text_1")).Condition(()=>Hero.OneToOneConversationHero?.StringId=="the_owl_hero" && Hero.OneToOneConversationHero?.PartyBelongedTo != null).PlayerLine(GameTexts.FindText("rf_the_owl_after_alchemist_text_2"))
                .NpcLine(GameTexts.FindText("rf_the_owl_after_alchemist_text_3")).BeginPlayerOptions().PlayerOption(GameTexts.FindText("rf_the_owl_after_alchemist_text_4")).NpcLine(GameTexts.FindText("rf_the_owl_after_alchemist_text_6")).Consequence(() => CompleteWithBetrayal(true)).CloseDialog()
                .PlayerOption(GameTexts.FindText("rf_the_owl_after_alchemist_text_5")).NpcLine(GameTexts.FindText("rf_the_owl_after_alchemist_text_7")).Consequence(()=>CompleteWithBetrayal(false)).CloseDialog().EndPlayerOptions();
            private void CompleteWithBetrayal(bool mergeparty)
            {
                
                if (mergeparty)
                {
                    MobileParty owlparty = Hero.OneToOneConversationHero.PartyBelongedTo;
                    Tools.Tools.MergeDisbandParty(owlparty, MobileParty.MainParty.Party);
                    owlparty.Ai.SetMoveGoToSettlement(Queen.HomeSettlement);
                    Vec2 pos = owlparty.Position2D;
                    pos.x += 1;
                    owlparty.Position2D = pos;
                    owlparty.SetCustomName(new TextObject("{rf_messenger}Messenger"));
                }
                else
                {
                    Vec2 position = Hero.OneToOneConversationHero.PartyBelongedTo.Position2D;
                    position.x += 0.5f;

                    Hero.OneToOneConversationHero.PartyBelongedTo.Ai.SetMoveGoToSettlement(Queen.HomeSettlement);
                    Hero.OneToOneConversationHero.PartyBelongedTo.Ai.RecalculateShortTermAi();
                }


                PlayerEncounter.Finish();
                this.CompleteQuestWithBetrayal();
                QueenQuest queenQuest = new QueenQuest("rf_queen_quest", Queen, CampaignTime.Never, 50000, true);
                queenQuest.StartQuest();
                queenQuest.OwlDialogConsequence();
            }

            private DialogFlow SetSecondPhaseHiddenHandDialog2() => DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_first_quest_hidden_hand_text_prisoner_1").ToString())
                .Condition(() => Hero.OneToOneConversationHero?.PartyBelongedTo == null && Hero.OneToOneConversationHero?.Clan?.StringId == "hidden_hand").NpcLine(GameTexts.FindText("rf_first_quest_hidden_hand_text_prisoner_2").ToString())
                .PlayerLine(GameTexts.FindText("rf_first_quest_hidden_hand_text_prisoner_3").ToString()).NpcLine(GameTexts.FindText("rf_first_quest_hidden_hand_text_prisoner_4").ToString());
            private DialogFlow PlayerDeliverPrisonersToQueen() => DialogFlow.CreateDialogFlow("start", 125).PlayerLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_1").ToString())
                .Condition(() => PlayerHaveprisoners && !_refusedQueenMapQuest).NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_2").ToString())
                .PlayerLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_3").ToString()).NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_4").ToString()).PlayerLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_5").ToString())
                .NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_6").ToString()).NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_7").ToString()).Consequence(DeliverPrisonersToQueen)
                .BeginPlayerOptions()
                .PlayerOption(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_8").ToString(), null).NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_11").ToString()).Consequence(StartSecondQuest).CloseDialog()
                .PlayerOption(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_9").ToString(), null).NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_10").ToString())
                .Consequence(RefusedQueenMapQuest).CloseDialog().EndPlayerOptions();

            private void RefusedQueenMapQuest()
            {
                Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 125).NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_7").ToString())
                    .Condition(() => Hero.OneToOneConversationHero == Queen).BeginPlayerOptions()
                    .PlayerOption(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_8").ToString(), null)
                    .NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_11").ToString()).Consequence(StartSecondQuest).CloseDialog()
                    .PlayerOption(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_9").ToString(), null)
                    .NpcLine(GameTexts.FindText("rf_first_quest_player_deliver_prisoners_text_10").ToString()).CloseDialog().EndPlayerOptions());
                _refusedQueenMapQuest = true;
            }

            protected override void HourlyTick()
            {
                if (_bringZombiesJournalLog != null)
                    this._bringZombiesJournalLog.UpdateCurrentProgress(MobileParty.MainParty.PrisonRoster.GetTroopRoster().Where(x => !x.Character.IsHero && x.Character?.Culture?.StringId == "sea_raiders").Sum(x => x.Number));
                if (hideoutDefeated)
                {
                    ConversationCharacterData playerData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
                    Uliah.MakeWounded();
                    ConversationCharacterData manData = new ConversationCharacterData(Uliah.CharacterObject);
                    Campaign.Current.ConversationManager.OpenMapConversation(playerData, manData);
                    hideoutDefeated = false;
                }
                else if (!_thirdTextBox && hideoutPosition.IsValid)
                {
                    int radius = 8;

                    //Check if hideout is near
                    if (Math.Sqrt(Math.Pow(hideoutPosition.X - MobileParty.MainParty.Position2D.X, 2) + Math.Pow(hideoutPosition.Y - MobileParty.MainParty.Position2D.Y, 2)) <= radius)
                    {
                        InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_main_quest_start_inquiry_5").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", () => { }, null), true);
                        _thirdTextBox = true;
                    }

                }

                if (_deliveredToAlchemistsTime != CampaignTime.Zero && _deliveredToAlchemistsTime.ElapsedHoursUntilNow >= 24)
                {
                    SpawnOwlParty();
                    _deliveredToAlchemistsTime = CampaignTime.Zero;
                }
            }
        }
            

    }
}