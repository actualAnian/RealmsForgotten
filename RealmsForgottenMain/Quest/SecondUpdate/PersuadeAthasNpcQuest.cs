using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using NetworkMessages.FromServer;
using RealmsForgotten.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using Helpers;

namespace RealmsForgotten.Quest.SecondUpdate
{

    [HarmonyPatch(typeof(DisbandArmyAction), "ApplyInternal")]
    public static class PersuadeAthasNpcQuest_DisbandArmyPatch
    {
        public static bool Prefix(Army army, Army.ArmyDispersionReason reason)
        {
            if (PersuadeAthasNpcQuest.IsPlayerInOwlArmy && army.Parties.Any(x => x.LeaderHero == Hero.MainHero))
            {
                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(PlayerArmyWaitBehavior), "wait_menu_army_leave_on_condition")]
    public static class PersuadeAthasNpcQuest_wait_menu_army_leave_on_conditionPatch
    {
        public static void Postfix(MenuCallbackArgs args, ref bool __result)
        {
            if (PersuadeAthasNpcQuest.IsPlayerInOwlArmy)
            {
                __result = false;
            }
        }
    }
    public class PersuadeAthasNpcBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckIfFirstQuestHasEnded);
        }

        private void CheckIfFirstQuestHasEnded()
        {
            SaveCurrentQuestCampaignBehavior currentQuestCampaignBehavior = SaveCurrentQuestCampaignBehavior.Instance;
            if (currentQuestCampaignBehavior != null && currentQuestCampaignBehavior.questStoppedAt != null && !Campaign.Current.QuestManager.Quests.Any(x=>x is PersuadeAthasNpcQuest))
            {
                Hero hero = null;
                if (currentQuestCampaignBehavior.questStoppedAt == "anorit")
                
                    hero = Hero.FindFirst(x => x.StringId == "lord_WE9_l");
                
                else if (currentQuestCampaignBehavior.questStoppedAt == "queen")
                
                    hero = Kingdom.All.First(x => x.StringId == "empire").Leader.Spouse;

                    

                new PersuadeAthasNpcQuest("athas_quest", hero, CampaignTime.Never, 0).StartQuest();
                Campaign.Current.CampaignBehaviorManager.RemoveBehavior<PersuadeAthasNpcBehavior>();


                
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
    
    public class PersuadeAthasNpcQuest : QuestBase
    {
        [SaveableField(1)] 
        private JournalLog goToAnoritLordLog;
        [SaveableField(2)] 
        private JournalLog takeAthasScholarLog;
        [SaveableField(3)]
        private Hero athasScholarHero;
        [SaveableField(4)]
        private bool _isPlayerInOwlArmy;
        [SaveableField(5)]
        private bool _willGoAsCaravan;

        public static PersuadeAthasNpcQuest Instance;

        private PersuasionTask _persuasionTask;
        private Hero TheOwl => Hero.FindFirst(x => x.StringId == "the_owl_hero");
        public static bool IsPlayerInOwlArmy => Instance?._isPlayerInOwlArmy == true;

        public PersuadeAthasNpcQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            Instance = this;
        }
        Settlement Ityr => Settlement.Find("town_A1");
        public override bool IsSpecialQuest => true;
       
        public override TextObject Title => GameTexts.FindText("rf_quest_title_part_three");
        protected override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this,
                (CampaignGameStarter campaignGameStarter) =>
                {
                    campaignGameStarter.AddGameMenuOption("town", "town_athas_quest_option", GameTexts.FindText("town_athas_quest_option").ToString(),
                        x =>
                        {
                            x.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;
                            return Settlement.CurrentSettlement == Ityr && !_willGoAsCaravan && takeAthasScholarLog?.CurrentProgress == 1;
                        },
                        args =>
                        {
                            PrepareAthasScholarHero();
                        });
                });

            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnLeaveSettlement);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (_isPlayerInOwlArmy && hero == Hero.MainHero && settlement == Ityr && takeAthasScholarLog?.CurrentProgress == 1)
            {
                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_ityr_arrive_message").ToString(), true, false, GameTexts.FindText("str_done").ToString(), "",
                    PrepareAthasScholarHero, null), true);
                _isPlayerInOwlArmy = false;

            }
        }

        private void PrepareAthasScholarHero()
        {
            Location currentLocation = LocationComplex.Current.GetListOfLocations()
                .FirstOrDefaultQ(x => x.StringId == "lordshall");

            

            athasScholarHero =
                HeroCreator.CreateSpecialHero(CharacterObject.Find("rf_athas_scholar"), Settlement.CurrentSettlement);

            athasScholarHero.SetName(new TextObject("{=athas_scholar_name}Athas Scholar"), null);

            athasScholarHero.StringId = "rf_athas_scholar";

            AgentData agentData = new AgentData(athasScholarHero.CharacterObject);
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_villager");
            LocationCharacter locationCharacter = new(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors), CampaignData.NotableTag, true,
                LocationCharacter.CharacterRelations.Friendly, actionSet, true);

            athasScholarHero.SetHasMet();

            PrepareFeast(ref currentLocation);

            currentLocation.AddCharacter(locationCharacter);

            OpenMissionWithSettingPreviousLocation("center", "lordshall");



        }

        private void PrepareFeast(ref Location currentLocation)
        {
            List<Hero> lords = Settlement.CurrentSettlement.OwnerClan.Kingdom.Lords;
            lords.Randomize();
            for (int i = 0; i < lords.Count / 2; i++)
            {
                AgentData agentData = new AgentData(lords[i].CharacterObject);

                string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_lord");

                LocationCharacter locationCharacter = new(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, CampaignData.NotableTag, false,
                    LocationCharacter.CharacterRelations.Friendly, actionSet, true);

                currentLocation.AddCharacter(locationCharacter);
            }

            foreach (var locationCharacter in CreateEntertainers(Settlement.CurrentSettlement.Culture))
            {
                currentLocation.AddCharacter(locationCharacter);
            }
        }

        private IEnumerable<LocationCharacter> CreateEntertainers(CultureObject culture)
        {

            for (int i = 0; i < 2; i++)
            {
                AgentData agentData = new AgentData(culture.Musician);

                string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_musician");

                LocationCharacter locationCharacter = new(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors, CampaignData.MusicianTag, true,
                    LocationCharacter.CharacterRelations.Friendly, actionSet, true);

                yield return locationCharacter;
            }

            for (int i = 0; i < 3; i++)
            {
                AgentData agentData = new AgentData(culture.FemaleDancer);

                string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_dancer");

                LocationCharacter locationCharacter = new(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors, CampaignData.DancerTag, true,
                    LocationCharacter.CharacterRelations.Friendly, actionSet, true);

                yield return locationCharacter;
            }
        }
        private void OnLeaveSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (_willGoAsCaravan && takeAthasScholarLog is { CurrentProgress: 0 })
            {
                MakeCaravanForQuest();
                takeAthasScholarLog.UpdateCurrentProgress(1);

            }
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
            if (_willGoAsCaravan && MobileParty.MainParty.Army != null)
            {
                MobileParty.MainParty.Army.Cohesion = 100f;
                if (MobileParty.MainParty.Army.LeaderParty.GetNumDaysForFoodToLast() < 1)
                {
                    MobileParty.MainParty.Army.LeaderParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>(MBRandom.RandomFloat > 0.5f ? "grain" : "fish"), 100);
                }
            }
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            Instance = this;
        }

        private void MakeCaravanForQuest()
        {
            Hero Owl = TheOwl;

            MobileParty caravanParty =
                QuestCaravanPartyComponent.CreateQuestCaravanParty(Owl, QuestGiver.HomeSettlement);

            caravanParty.InitializeMobilePartyAtPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), QuestGiver.HomeSettlement.GatePosition);

            caravanParty.AddElementToMemberRoster(Owl.CharacterObject, 1);
            caravanParty.ChangePartyLeader(Owl);

            caravanParty.Army = new Army(Hero.MainHero.Clan.Kingdom, caravanParty, Army.ArmyTypes.Patrolling);

            MobileParty.MainParty.Army = caravanParty.Army;

            MobileParty.MainParty.Army.AddPartyToMergedParties(MobileParty.MainParty);

            MobileParty.MainParty.Army.AiBehaviorObject = Ityr;
            MobileParty.MainParty.Army.AIBehavior = Army.AIBehaviorFlags.GoToSettlement;
            MobileParty.MainParty.Army.LeaderParty.Ai.SetMoveGoToSettlement(Ityr);

            MobileParty.MainParty.Army.LeaderParty.Ai.SetDoNotMakeNewDecisions(true);

            MobileParty.MainParty.Army.LeaderParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
            MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Never);

            MobileParty.MainParty.Army.LeaderParty.SpeedExplained.AddFactor(1.0f);
            MobileParty.MainParty.Army.Cohesion = 100f;
            MobileParty.MainParty.Army.DailyCohesionChangeExplanation.Add(100f);

            _isPlayerInOwlArmy = true;
        }
        private void AnoritFirstDialogConsequence(bool willGoAsCaravan)
        {
            TextObject textObject = GameTexts.FindText("rf_third_quest_anorit_objective_2_" + (willGoAsCaravan ? 'a' : 'b'));
            _willGoAsCaravan = willGoAsCaravan;

            textObject.SetTextVariable("SETTLEMENT", Ityr.EncyclopediaLinkWithName);
            takeAthasScholarLog = this.AddLog(textObject);
            this.AddTrackedObject(Ityr);
        }
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(AnoritFirstDialogFlow, this);

            Campaign.Current.ConversationManager.AddDialogFlow(AthasPersuasionDialogFlow(), this);
        }

        
        private DialogFlow AnoritFirstDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_1")).Condition(() => Hero.OneToOneConversationHero == this.QuestGiver && goToAnoritLordLog.CurrentProgress == 0)
            .NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_2")).PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_3")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_5")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_6")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_7"))
            .BeginPlayerOptions().PlayerOption(GameTexts.FindText("rf_third_quest_anorit_dialog_8"))
            .Consequence(() => AnoritFirstDialogConsequence(true)).CloseDialog().PlayerOption(GameTexts.FindText("rf_third_quest_anorit_dialog_9"))
            .Consequence(() => AnoritFirstDialogConsequence(false)).CloseDialog()
            .EndPlayerOptions();

        private DialogFlow AthasPersuasionDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 50);

            dialogFlow.AddDialogLine("athas_persuasion_dialog", "start", "athas_persuasion_output",
                GameTexts.FindText("rf_greetings").ToString(), () => CharacterObject.OneToOneConversationCharacter == athasScholarHero.CharacterObject && takeAthasScholarLog?.CurrentProgress == 1, null, this);

            dialogFlow.AddPlayerLine("athas_persuasion_dialog_1", "athas_persuasion_output", "athas_persuasion_output_1",
                GameTexts.FindText("rf_third_quest_scholar_dialog_1").ToString(), null, null, this);

            dialogFlow.AddDialogLine("athas_persuasion_dialog_2", "athas_persuasion_output_1", "athas_persuasion_output_2",
                GameTexts.FindText("rf_third_quest_scholar_dialog_2").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("athas_persuasion_dialog_3", "athas_persuasion_output_2", "athas_persuasion_output_3",
                GameTexts.FindText("rf_third_quest_scholar_dialog_3").ToString(), () =>
                {
                    GameTexts.SetVariable("PLAYER", CharacterObject.PlayerCharacter.EncyclopediaLinkWithName);
                    return true; 
                }, null, this);

            dialogFlow.AddDialogLine("athas_persuasion_dialog_4", "athas_persuasion_output_3", "athas_persuasion_output_4",
                GameTexts.FindText("rf_third_quest_scholar_dialog_4").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("athas_persuasion_dialog_5", "athas_persuasion_output_4", "athas_persuasion_output_5",
                GameTexts.FindText("rf_third_quest_scholar_dialog_5").ToString(), null, null, this);

            dialogFlow.AddDialogLine("athas_persuasion_dialog_6", "athas_persuasion_output_5", "athas_persuasion_output_6",
                GameTexts.FindText("rf_third_quest_scholar_dialog_6").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("athas_persuasion_dialog_7", "athas_persuasion_output_6", "athas_persuasion_output_7",
                GameTexts.FindText("rf_third_quest_scholar_dialog_7").ToString(), null, null, this);

            dialogFlow.AddDialogLine("athas_persuasion_dialog_8", "athas_persuasion_output_7", "athas_persuasion_output_8",
                GameTexts.FindText("rf_third_quest_scholar_dialog_8").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("athas_persuasion_dialog_9", "athas_persuasion_output_8", "athas_persuasion_output_9",
                GameTexts.FindText("rf_third_quest_scholar_dialog_9").ToString(), null, null, this);

            dialogFlow.AddDialogLine("athas_persuasion_dialog_10", "athas_persuasion_output_9", "athas_persuasion_output_10",
                GameTexts.FindText("rf_third_quest_scholar_dialog_10").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("athas_persuasion_dialog_11", "athas_persuasion_output_10", "athas_persuasion_output_11",
                GameTexts.FindText("rf_third_quest_scholar_dialog_11").ToString(), null, null, this);

            dialogFlow.AddPlayerLine("athas_persuasion_dialog_12", "athas_persuasion_output_11", "athas_persuasion_output_12",
                GameTexts.FindText("rf_third_quest_scholar_dialog_12").ToString(), null, StartPersuasion, this);

            dialogFlow.AddDialogLine("athas_persuasion_attempt", "athas_persuasion_output_12", "athas_persuasion_options",
                GameTexts.FindText("rf_third_quest_scholar_dialog_12").ToString(), ()=>!PersuasionFailedCondition(), null, this);


            dialogFlow.AddPlayerLine("athas_persuasion_option_1", "athas_persuasion_options", "athas_persuasion_outcome",
                "{=!}{PERSUADE_ATTEMPT_1}", PersuasionOptionCondition_1, PersuasionOptionConsequence_1, this, 100, null, ()=> _persuasionTask.Options.ElementAt(0));

            dialogFlow.AddPlayerLine("athas_persuasion_option_2", "athas_persuasion_options", "athas_persuasion_outcome",
                "{=!}{PERSUADE_ATTEMPT_2}", PersuasionOptionCondition_2, PersuasionOptionConsequence_2, this, 100, null, () => _persuasionTask.Options.ElementAt(1));


            dialogFlow.AddDialogLine("athas_persuasion_success", "athas_persuasion_outcome", "close_window",
                GameTexts.FindText("rf_third_quest_scholar_dialog_persuasion_success").ToString(), ConversationManager.GetPersuasionProgressSatisfied, PersuasionComplete, this);

            dialogFlow.AddDialogLine("athas_persuasion_failed", "athas_persuasion_outcome", "close_window",
                GameTexts.FindText("rf_third_quest_scholar_dialog_persuasion_fail").ToString(), PersuasionFailedCondition, PersuasionComplete, this);



            return dialogFlow;;
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
        private void StartPersuasion()
        {
            _persuasionTask = GetPersuasionTask();
            ConversationManager.StartPersuasion(1f, 1f, 0f, 1f, 1f, 0f, PersuasionDifficulty.MediumHard);
        }

        private PersuasionTask GetPersuasionTask()
        {
            PersuasionTask persuasionTask = new PersuasionTask(0);

            persuasionTask.FinalFailLine = GameTexts.FindText("rf_third_quest_scholar_dialog_persuasion_fail");
            persuasionTask.TryLaterLine = null;
            persuasionTask.SpokenLine = GameTexts.FindText("rf_third_quest_scholar_dialog_12");

            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, PersuasionArgumentStrength.ExtremelyEasy, true,
                GameTexts.FindText("rf_third_quest_scholar_dialog_13_persuade_1"), null, false, false, false);
            persuasionTask.AddOptionToTask(option);
            PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Negative, PersuasionArgumentStrength.ExtremelyHard, false,
                GameTexts.FindText("rf_third_quest_scholar_dialog_13_persuade_2"), null, false, false, false);
            persuasionTask.AddOptionToTask(option2);

            return persuasionTask;
        }

        private bool PersuasionFailedCondition()
        {
            if (_persuasionTask.Options.All((x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
            {
                return true;
            }
            return false;
        }
        private void PersuasionComplete()
        {
            ConversationManager.EndPersuasion();
            takeAthasScholarLog.UpdateCurrentProgress(2);
        }

        private static void OpenMissionWithSettingPreviousLocation(string previousLocationId, string missionLocationId)
        {
            Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId(missionLocationId);
            Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId(previousLocationId);
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, null, null, null);
            Campaign.Current.GameMenuManager.NextLocation = null;
            Campaign.Current.GameMenuManager.PreviousLocation = null;
        }

        
    }

    public class QuestCaravanPartyComponent : CaravanPartyComponent
    {
        protected internal QuestCaravanPartyComponent(Settlement settlement, Hero owner, Hero partyLeader) : base(settlement, owner, partyLeader)
        {
        }
        public static MobileParty CreateQuestCaravanParty(Hero caravanOwner, Settlement spawnSettlement, bool isInitialSpawn = false, Hero caravanLeader = null, ItemRoster caravanItems = null, int troopToBeGiven = 0, bool isElite = false)
        {
            MobileParty mobileParty2 = MobileParty.CreateParty("caravan_template_" + spawnSettlement.Culture.StringId.ToLower() + "_1", new QuestCaravanPartyComponent(spawnSettlement, caravanOwner, caravanLeader), delegate (MobileParty mobileParty)
            {
                (mobileParty.PartyComponent as QuestCaravanPartyComponent).InitializeCaravanOnCreation(mobileParty, caravanLeader, caravanItems, troopToBeGiven, isElite);
            });
            if (spawnSettlement.Party.MapEvent == null && spawnSettlement.SiegeEvent == null)
            {
                mobileParty2.Ai.SetMoveGoToSettlement(spawnSettlement);
                mobileParty2.Ai.RecalculateShortTermAi();
                EnterSettlementAction.ApplyForParty(mobileParty2, spawnSettlement);
            }
            else
            {
                mobileParty2.Ai.SetMoveModeHold();
            }

            if (mobileParty2.LeaderHero != null)
            {
                CampaignEventDispatcher.Instance.OnHeroGetsBusy(mobileParty2.LeaderHero, HeroGetsBusyReasons.BecomeCaravanLeader);
            }

            return mobileParty2;
        }
        private void InitializeCaravanOnCreation(MobileParty mobileParty, Hero caravanLeader, ItemRoster caravanItems, int troopToBeGiven, bool isElite)
        {
            InitializeCaravanProperties();
            if (troopToBeGiven == 0)
            {
                float num = 1f;
                num = ((!(MBRandom.RandomFloat < 0.67f)) ? 1f : ((1f - MBRandom.RandomFloat * MBRandom.RandomFloat) * 0.5f + 0.5f));
                int num2 = (int)((float)mobileParty.Party.PartySizeLimit * num);
                if (num2 >= 10)
                {
                    num2--;
                }

                troopToBeGiven = num2;
            }

            PartyTemplateObject pt = (isElite ? Settlement.Culture.EliteCaravanPartyTemplate : Settlement.Culture.CaravanPartyTemplate);
            mobileParty.InitializeMobilePartyAtPosition(pt, Settlement.GatePosition, troopToBeGiven);
            if (caravanLeader != null)
            {
                mobileParty.MemberRoster.AddToCounts(caravanLeader.CharacterObject, 1, insertAtFront: true);
            }
            else
            {
                CharacterObject character2 = CharacterObject.All.First((CharacterObject character) => character.Occupation == Occupation.CaravanGuard && character.IsInfantry && character.Level == 26 && character.Culture == mobileParty.Party.Owner.Culture);
                mobileParty.MemberRoster.AddToCounts(character2, 1, insertAtFront: true);
            }

            mobileParty.ActualClan = Owner.Clan;
            mobileParty.Party.SetVisualAsDirty();
            mobileParty.InitializePartyTrade(10000 + ((Owner.Clan == Clan.PlayerClan) ? 5000 : 0));
            if (caravanItems != null)
            {
                mobileParty.ItemRoster.Add(caravanItems);
                return;
            }

            float num3 = 10000f;
            ItemObject itemObject = null;
            foreach (ItemObject item in Items.All)
            {
                if (item.ItemCategory == DefaultItemCategories.PackAnimal && !item.NotMerchandise && (float)item.Value < num3)
                {
                    itemObject = item;
                    num3 = item.Value;
                }
            }

            if (itemObject != null)
            {
                mobileParty.ItemRoster.Add(new ItemRosterElement(itemObject, (int)((float)mobileParty.MemberRoster.TotalManCount * 0.5f)));
            }
        }

        private void InitializeCaravanProperties()
        {
            base.MobileParty.Aggressiveness = 0f;

        }
        public override void ChangePartyLeader(Hero newLeader)
        {
            if (newLeader != null)
                base.ChangePartyLeader(newLeader);
        }

        
    }
}
