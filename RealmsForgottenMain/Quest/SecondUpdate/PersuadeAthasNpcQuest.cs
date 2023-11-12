﻿using System;
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
using FaceGen = TaleWorlds.Core.FaceGen;
using System.Drawing;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.SaveSystem.Save;
using SandBox.Missions.AgentBehaviors;
using SandBox;
using SandBox.Conversation.MissionLogics;
using System.Collections;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using System.Timers;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using CharacterObject = TaleWorlds.CampaignSystem.CharacterObject;

namespace RealmsForgotten.Quest.SecondUpdate
{
     
    [HarmonyPatch(typeof(PrisonerReleaseCampaignBehavior), "DailyHeroTick")]
    public static class AvoidScholarFromEscapingPatch
    {
        public static bool Prefix(Hero hero)
        {
            if (PersuadeAthasNpcQuest.MustAvoidPrisonerEscape && hero.CharacterObject == PersuadeAthasNpcQuest.PrisonerCharacter)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(DisbandArmyAction), "ApplyInternal")]
    public static class AvoidQuestArmyDisbandingPatch
    {
        public static bool AvoidDisbanding = false;
        public static bool Prefix(Army army, Army.ArmyDispersionReason reason)
        {
            if (AvoidDisbanding && army.Parties.Any(x => x == MobileParty.MainParty))
            {
                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(PlayerArmyWaitBehavior), "wait_menu_army_leave_on_condition")]
    public static class RemoveLeaveOptionFromArmyMenuPatch
    {
        public static void Postfix(MenuCallbackArgs args, ref bool __result)
        {
            if (AvoidQuestArmyDisbandingPatch.AvoidDisbanding)
            {
                __result = false;
            }
        }
    }

    public class PersuadeAthasNpcQuest : QuestBase
    {
        [SaveableField(1)] 
        private JournalLog goToAnoritLordLog;
        [SaveableField(2)] 
        private JournalLog persuadeAthasScholarLog;
        [SaveableField(3)]
        private JournalLog waitAthasScholarLog;
        [SaveableField(4)]
        private JournalLog captureAthasScholarLog;
        [SaveableField(5)]
        private Hero athasScholarHero;
        [SaveableField(6)]
        private bool _isPlayerInOwlArmy;
        [SaveableField(7)]
        private bool _willGoAsCaravan;
        [SaveableField(8)]
        private CampaignTime waitAthasScholarTime;
        [SaveableField(9)]
        private JournalLog escortAthasScholarLog;
        [SaveableField(10)]
        private JournalLog failedPersuasionLog;
        [SaveableField(11)]
        private JournalLog defeatedByScholarLog;
        [SaveableField(12)]
        private JournalLog waitUntilDecipherLog;
        [SaveableField(13)]
        private JournalLog goToHideoutLog;
        [SaveableField(14)]
        private Settlement questHideout;

        private bool IsPlayerInOwlArmy
        {
            get => _isPlayerInOwlArmy;
            set
            {
                _isPlayerInOwlArmy = value;
                AvoidQuestArmyDisbandingPatch.AvoidDisbanding = value;
            }
        }

        private Agent scholarAgent;
        private bool scholarDefeated = false;
        private bool playerDefeated = false;

        public static PersuadeAthasNpcQuest Instance;

        private PersuasionTask _persuasionTask;
        private Hero TheOwl => Hero.FindFirst(x => x.StringId == "the_owl_hero");
        public static bool MustAvoidPrisonerEscape
        {
            get
            {
                if (Instance?.escortAthasScholarLog?.CurrentProgress == 1 ||
                    Instance?.failedPersuasionLog?.CurrentProgress == 2)
                {
                    PrisonerCharacter = AthasHero.CharacterObject;
                    return true;
                }
                else if (Instance?.goToHideoutLog?.CurrentProgress == 5)
                {
                    PrisonerCharacter = CharacterObject.Find(hideoutBossCharacterId);
                    return true;
                }
                return false;
            }
        }

        public static Hero? AthasHero => Instance?.athasScholarHero;

        private static readonly string hideoutBossCharacterId = "necromancer_boss";

        public static CharacterObject PrisonerCharacter;
        

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
                            return Settlement.CurrentSettlement == Ityr && !_willGoAsCaravan && persuadeAthasScholarLog?.CurrentProgress == 1 && failedPersuasionLog == null;
                        },
                        args =>
                        {
                            PrepareScholarHeroAndEvent();
                        });
                });

            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnLeaveSettlement);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStarted);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionOpened);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, BattleEnd);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, (partyBase, hero) =>
            {
                if (defeatedByScholarLog?.CurrentProgress == 1 || (persuadeAthasScholarLog?.CurrentProgress == 1 && failedPersuasionLog?.CurrentProgress == 1) && partyBase == PartyBase.MainParty &&
                    hero == athasScholarHero)
                {
                    if (failedPersuasionLog?.CurrentProgress == 1)
                    {
                        failedPersuasionLog.UpdateCurrentProgress(2);
                        AddEscortLog();
                        escortAthasScholarLog.UpdateCurrentProgress(1);

                    }
                    else
                    {
                        defeatedByScholarLog.UpdateCurrentProgress(2);
                        AddEscortLog();
                        escortAthasScholarLog.UpdateCurrentProgress(1);
                        RemoveTrackedObject(Ityr.BoundVillages[0].Settlement);
                    }
                }
            });
        }

        private void BattleEnd(MapEvent mapEvent)
        {

            if (goToHideoutLog?.CurrentProgress == 1  && Settlement.CurrentSettlement == questHideout)
            {
                if (mapEvent.DefeatedSide == mapEvent.DefenderSide.MissionSide)
                {
                    goToHideoutLog.UpdateCurrentProgress(2);
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(CharacterObject.Find(hideoutBossCharacterId)));
                    goToHideoutLog.UpdateCurrentProgress(3);
                }
                else
                {
                    QuestLibrary.InitializeHideoutIfNeeded(questHideout.Hideout);
                }
                
            }
        }

        private void OnTick(float obj)
        {
            if (goToHideoutLog?.CurrentProgress == 0 && questHideout.GatePosition.DistanceSquared(MobileParty.MainParty.Position2D) <= 5)
            {
                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_near_hideout_message").ToString(), true, false, GameTexts.FindText("str_done").ToString(), "",
                    null, null), true);
                goToHideoutLog.UpdateCurrentProgress(1);
            }
        }

        private void OnLeaveSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (mobileParty == MobileParty.MainParty && settlement == Ityr.BoundVillages[0].Settlement || settlement == Ityr)
            {
                if (escortAthasScholarLog?.CurrentProgress == 0)
                {
                    TakePrisonerAction.Apply(PartyBase.MainParty, athasScholarHero);
                    escortAthasScholarLog.UpdateCurrentProgress(1);
                    RemoveTrackedObject(settlement);
                }
                else if (defeatedByScholarLog?.CurrentProgress == 0)
                {
                    LordPartyComponent.CreateLordParty("athas_scholar_party", athasScholarHero, settlement.GatePosition,
                        10f, settlement, athasScholarHero).InitializeMobilePartyAroundPosition(athasScholarHero.Culture.DefaultPartyTemplate, settlement.GatePosition, 10f, 10f);
                    defeatedByScholarLog.UpdateCurrentProgress(1);

                    RemoveTrackedObject(settlement);
                }
                else if (failedPersuasionLog?.CurrentProgress == 0)
                {
                    LordPartyComponent.CreateLordParty("athas_scholar_party", athasScholarHero, settlement.GatePosition,
                       10f, settlement, athasScholarHero).InitializeMobilePartyAroundPosition(athasScholarHero.Culture.DefaultPartyTemplate, settlement.GatePosition, 10f, 10f);
                    failedPersuasionLog.UpdateCurrentProgress(1);
                    RemoveTrackedObject(settlement);
                }
            }
        }

        private void OnBeforeMissionOpened()
        {
            if (Settlement.CurrentSettlement == Ityr && persuadeAthasScholarLog?.CurrentProgress == 1)
            {
                IsPlayerInOwlArmy = false;
                DisbandArmyAction.ApplyByObjectiveFinished(MobileParty.MainParty.Army);
            }

            if (Settlement.CurrentSettlement == questHideout && goToHideoutLog?.CurrentProgress == 1)
            {
                CharacterObject bossCharacterObject = CharacterObject.Find("forest_bandits_boss");
                MobileParty bossParty = questHideout.Parties.Find(x => x.MemberRoster.Contains(bossCharacterObject));


                bossParty.MemberRoster.RemoveIf(x => x.Character.StringId.Contains("boss"));
                bossParty.Party.AddMember(CharacterObject.Find(hideoutBossCharacterId), 1);
            }
        }

        private void OnMissionStarted(IMission imission)
        {
            if (imission is Mission mission)
            {
                if (Settlement.CurrentSettlement == Ityr &&
                    (persuadeAthasScholarLog?.CurrentProgress == 1 || failedPersuasionLog != null))
                {
                    mission.AddMissionBehavior(new PersuadeScholarMissionLogic());
                }
                if (Settlement.CurrentSettlement == Ityr.BoundVillages[0].Settlement && waitAthasScholarLog?.HasBeenCompleted() == true && captureAthasScholarLog?.CurrentProgress == 0)
                {
                    mission.AddMissionBehavior(new FightScholarMissionLogic((victim, attacker, damage) =>
                    {
                        if (victim == scholarAgent && damage >= scholarAgent.Health)
                            scholarDefeated = true;
                        if (victim == Agent.Main && damage >= Agent.Main?.Health)
                            playerDefeated = true;
                    }));
                }

            }
        }

        private void OnBeforeMissionStarted()
        {
            if (captureAthasScholarLog?.CurrentProgress == 0 && Settlement.CurrentSettlement == Ityr.BoundVillages[0].Settlement)
            {
                Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(athasScholarHero.CharacterObject.Race, "_settlement");
                string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, athasScholarHero.IsFemale, ActionSetCode.VillainActionSetSuffix);
                AgentData agentData = new AgentData(new SimpleAgentOrigin(athasScholarHero.CharacterObject)).Monster(monsterWithSuffix).NoHorses(true).ClothingColor1(Settlement.CurrentSettlement.MapFaction.Color).ClothingColor2(Settlement.CurrentSettlement.MapFaction.Color2);

                LocationCharacter locationCharacter = new(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, CampaignData.Alley2Tag, false,
                    LocationCharacter.CharacterRelations.Neutral, actionSet, false);

                Location location = LocationComplex.Current.GetLocationWithId("village_center");

                location.AddCharacter(locationCharacter);

                location.AddLocationCharacters(CreateBodyGuard, athasScholarHero.Culture, LocationCharacter.CharacterRelations.Neutral, 2);
                
            }
        }

        private LocationCharacter CreateBodyGuard(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(culture.Guard.Race, "_settlement");
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, culture.Guard.IsFemale, ActionSetCode.VillainActionSetSuffix);
            AgentData agentData = new AgentData(new SimpleAgentOrigin(culture.Guard)).Monster(monsterWithSuffix).NoHorses(true).ClothingColor1(Settlement.CurrentSettlement.MapFaction.Color).ClothingColor2(Settlement.CurrentSettlement.MapFaction.Color2);

            return new(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, CampaignData.Alley2Tag, false,
                LocationCharacter.CharacterRelations.Neutral, actionSet, false);
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (IsPlayerInOwlArmy && hero == Hero.MainHero && settlement == Ityr && persuadeAthasScholarLog.CurrentProgress == 1 && failedPersuasionLog == null)
            {
                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_ityr_arrive_message").ToString(), true, false, GameTexts.FindText("str_done").ToString(), "",
                    PrepareScholarHeroAndEvent, null), true);
                IsPlayerInOwlArmy = false;
                goToAnoritLordLog.UpdateCurrentProgress(2);
            }
        }

        private void PrepareScholarHeroAndEvent()
        {
            athasScholarHero =
                HeroCreator.CreateSpecialHero(CharacterObject.Find("rf_athas_scholar"), Settlement.CurrentSettlement);

            athasScholarHero.Clan = Ityr.OwnerClan;

            athasScholarHero.SetName(new TextObject("{=athas_scholar_name}Athas Scholar"), null);

            athasScholarHero.StringId = "rf_athas_scholar";

            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(athasScholarHero.CharacterObject.Race, "_settlement");
            string actionSet = ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, athasScholarHero.IsFemale, ActionSetCode.LordActionSetSuffix);
            AgentData agentData = new AgentData(new SimpleAgentOrigin(athasScholarHero.CharacterObject)).Monster(monsterWithSuffix).NoHorses(true).ClothingColor1(Settlement.CurrentSettlement.MapFaction.Color).ClothingColor2(Settlement.CurrentSettlement.MapFaction.Color2);

            LocationCharacter locationCharacter = new(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, CampaignData.NotableTag, true,
                LocationCharacter.CharacterRelations.Neutral, actionSet, true);

            athasScholarHero.SetHasMet();

            LordsHall.AddCharacter(locationCharacter);

            PrepareFeast();

            OpenMissionWithSettingPreviousLocation("center", "lordshall");
        }
        private static Tuple<string, Monster> GetActionSetAndMonster(CharacterObject character)
        {
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
            return new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, character.IsFemale, ActionSetCode.LordActionSetSuffix), monsterWithSuffix);
        }
        private void PrepareFeast()
        {
            List<Hero> lords = Settlement.CurrentSettlement.OwnerClan.Kingdom.Lords.Where(x => x != Hero.MainHero && !x.IsChild).ToList();
            lords.Randomize();
            int i = lords.Count > 12 ? 12 : lords.Count;
            for (; i >= 0; i--)
            {

                Tuple<string, Monster> actionSetAndMonster = GetActionSetAndMonster(lords[i].CharacterObject);

                AgentData agentData = new AgentData(new SimpleAgentOrigin(lords[i].CharacterObject)).Monster(actionSetAndMonster.Item2).NoHorses(true).ClothingColor1(lords[i].MapFaction.Color).ClothingColor2(lords[i].MapFaction.Color2);

                LocationCharacter locationCharacter = new(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, 
                    lords[i] == Settlement.CurrentSettlement.Owner ? CampaignData.ThroneTag : CampaignData.NotableTag, true,
                    LocationCharacter.CharacterRelations.Neutral, actionSetAndMonster.Item1, true);

                LordsHall.AddCharacter(locationCharacter);

            }
        }
        private static Location LordsHall => LocationComplex.Current.GetLocationWithId("lordshall");

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

            if (_willGoAsCaravan && persuadeAthasScholarLog is { CurrentProgress: 0 } && MobileParty.MainParty.CurrentSettlement == null)
            {
                MakeCaravanForQuest();
                persuadeAthasScholarLog.UpdateCurrentProgress(1);
            }

            if (waitAthasScholarLog != null && captureAthasScholarLog == null)
            {
                waitAthasScholarLog.UpdateCurrentProgress((int)waitAthasScholarTime.ElapsedDaysUntilNow);
                if (waitAthasScholarLog.HasBeenCompleted())
                {
                    TextObject textObject = GameTexts.FindText("rf_third_quest_anorit_objective_4");
                    textObject.SetTextVariable("SETTLEMENT", Ityr.BoundVillages[0].Settlement.EncyclopediaLinkWithName);
                    captureAthasScholarLog = AddLog(textObject);

                    AddTrackedObject(Ityr.BoundVillages[0].Settlement);
                }
            }

            if (waitUntilDecipherLog?.CurrentProgress == 0 && waitUntilDecipherLog?.LogTime.ElapsedDaysUntilNow >= 1)
            {
                waitUntilDecipherLog.UpdateCurrentProgress(1);
                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_decipher_finished_message").ToString(), true, false, GameTexts.FindText("str_done").ToString(), "",
                    null, null), true);
            }
        }

        protected override void InitializeQuestOnGameLoad()
        {
            QuestLibrary.InitializeVariables();
            SetDialogs();
            Instance = this;

            if(goToAnoritLordLog?.CurrentProgress == 1)
             SetCaravanObjective(MobileParty.All.Find(x => x.LeaderHero == TheOwl));

            AvoidQuestArmyDisbandingPatch.AvoidDisbanding = _isPlayerInOwlArmy;
        }

        private void MakeCaravanForQuest()
        {
            Hero Owl = TheOwl;

            MobileParty caravanParty =
                QuestCaravanPartyComponent.CreateQuestCaravanParty(Owl, QuestGiver.HomeSettlement);

            caravanParty.InitializeMobilePartyAtPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), MobileParty.MainParty.Position2D);
            caravanParty.AddElementToMemberRoster(TheOwl.CharacterObject, 1);
            caravanParty.ChangePartyLeader(TheOwl);
            SetCaravanObjective(caravanParty);

        }

        private void SetCaravanObjective(MobileParty caravanParty)
        {
            caravanParty.Army = new Army(QuestGiver.Clan.Kingdom, caravanParty, Army.ArmyTypes.Patrolling);

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

            IsPlayerInOwlArmy = true;
        }
        private void AnoritFirstDialogConsequence(bool willGoAsCaravan)
        {
            TextObject textObject = GameTexts.FindText("rf_third_quest_anorit_objective_2_" + (willGoAsCaravan ? 'a' : 'b'));
            _willGoAsCaravan = willGoAsCaravan;

            textObject.SetTextVariable("SETTLEMENT", Ityr.EncyclopediaLinkWithName);
            persuadeAthasScholarLog = this.AddLog(textObject);
            goToAnoritLordLog.UpdateCurrentProgress(1);
            this.AddTrackedObject(Ityr);

            AvoidBattleAfterConversation();
        }
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(AnoritFirstDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(AthasPersuasionDialogFlow(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(AthasAttackDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(DeliverScholarDialogFlow(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(HideoutBossDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(HideoutOwlDialogFlow, this);
            
            Campaign.Current.ConversationManager.AddDialogFlow(HideoutBossDialogFix(), this);
        }

        private void StartScholarFightInVillage()
        {
            captureAthasScholarLog.UpdateCurrentProgress(1);

            List<Agent> enemyAgents = new();

            scholarAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0];
            enemyAgents.Add(scholarAgent);

            MBList<Agent> agents = new MBList<Agent>();
            foreach (Agent agent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 50, agents))
            {
                if ((CharacterObject)agent.Character == athasScholarHero.Culture.Guard)
                {
                    enemyAgents.Add(agent);
                }
            }

            Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(new (){Agent.Main}, enemyAgents, false, false, delegate (bool isPlayerSideWon)
            {
                if (scholarDefeated)
                {
                    AddEscortLog();
                }
                else if (playerDefeated)
                {
                    if (defeatedByScholarLog == null)
                    {
                        TextObject textObject = GameTexts.FindText("rf_third_quest_anorit_objective_5_defeated");
                        textObject.SetCharacterProperties("SCHOLAR", athasScholarHero.CharacterObject);
                        defeatedByScholarLog = AddLog(textObject);
                    }
                }
            });

        }

        private void OnCapturedHideoutBoss()
        {
            goToHideoutLog?.UpdateCurrentProgress(5);

            CharacterObject hideoutBoss = CharacterObject.Find(hideoutBossCharacterId);

            if (!PartyBase.MainParty.PrisonRoster.Contains(hideoutBoss))
                PartyBase.MainParty.AddPrisoner(hideoutBoss, 1);

            new LastQuest("rf_last_quest", QuestGiver, CampaignTime.Never, 100000).StartQuest();
            CompleteQuestWithSuccess();
        }
        private void AddEscortLog()
        {
            TextObject textObject = GameTexts.FindText("rf_third_quest_anorit_objective_5");
            textObject.SetCharacterProperties("QUESTGIVER", QuestGiver.CharacterObject);
            escortAthasScholarLog = AddLog(textObject);
        }
        private DialogFlow AnoritFirstDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_1")).Condition(() => Hero.OneToOneConversationHero == this.QuestGiver && goToAnoritLordLog.CurrentProgress == 0)
            .NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_2")).PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_3")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_third_quest_anorit_dialog_5")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_6")).NpcLine(GameTexts.FindText("rf_third_quest_anorit_dialog_7"))
            .BeginPlayerOptions().PlayerOption(GameTexts.FindText("rf_third_quest_anorit_dialog_8"))
            .Consequence(() => AnoritFirstDialogConsequence(true)).CloseDialog().PlayerOption(GameTexts.FindText("rf_third_quest_anorit_dialog_9"))
            .Consequence(() => AnoritFirstDialogConsequence(false)).CloseDialog()
            .EndPlayerOptions();

        private DialogFlow AthasAttackDialogFlow => DialogFlow.CreateDialogFlow("start", 125).NpcLine(GameTexts.FindText("rf_third_quest_scholar_dialog_2_1"))
            .Condition(()=> Campaign.Current.ConversationManager.ConversationAgents[0].Character as CharacterObject == athasScholarHero.CharacterObject && captureAthasScholarLog?.CurrentProgress == 0)
            .Consequence(StartScholarFightInVillage).CloseDialog();

        private DialogFlow HideoutOwlDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_third_quest_boss_dialog_4_owl"))
            .Condition(() => goToHideoutLog?.CurrentProgress == 4).Consequence(OnCapturedHideoutBoss);

        private DialogFlow DeliverScholarDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125);
            dialogFlow.AddDialogLine("deliver_scholar_dialog_1", "start", "deliver_scholar_output_1", GameTexts.FindText("rf_third_quest_anorit_dialog_2_1").ToString(),
                ()=> Hero.OneToOneConversationHero == QuestGiver && escortAthasScholarLog?.CurrentProgress == 1, null, this);


            dialogFlow.AddPlayerLine("deliver_scholar_dialog_2", "deliver_scholar_output_1", "close_window",
                GameTexts.FindText("rf_ok").ToString(), null, WaitUntilDecipher, this);

            dialogFlow.AddDialogLine("postponed_dialog", "start", "deliver_scholar_output_3",
                GameTexts.FindText("rf_third_quest_anorit_dialog_2_2").ToString(), () => Hero.OneToOneConversationHero == QuestGiver && (escortAthasScholarLog?.CurrentProgress == 2 || waitUntilDecipherLog?.CurrentProgress == 1), null, this);

            dialogFlow.AddPlayerLine("deliver_scholar_dialog_4", "deliver_scholar_output_3", "close_window",
                GameTexts.FindText("rf_third_quest_anorit_dialog_2_3").ToString(), null, GoToHideoutLog, this);

            dialogFlow.AddPlayerLine("deliver_scholar_dialog_5", "deliver_scholar_output_3", "deliver_scholar_output_4",
                GameTexts.FindText("rf_third_quest_anorit_dialog_2_4").ToString(), null, ()=>
                {
                    escortAthasScholarLog.UpdateCurrentProgress(2);
                }, this);

            dialogFlow.AddDialogLine("deliver_scholar_dialog_6", "deliver_scholar_output_4", "close_window",
                GameTexts.FindText("rf_third_quest_anorit_dialog_2_5").ToString(), null, null, this);

            return dialogFlow;
        }


        private void WaitUntilDecipher()
        {

            AvoidBattleAfterConversation();
            GiveGoldAction.ApplyBetweenCharacters(QuestGiver, Hero.MainHero, 20000);
            TransferPrisonerAction.Apply(athasScholarHero.CharacterObject, PartyBase.MainParty, MobileParty.ConversationParty.Party);

            escortAthasScholarLog?.UpdateCurrentProgress(2);
            waitUntilDecipherLog = AddDiscreteLog(GameTexts.FindText("rf_third_quest_anorit_objective_6"), new TextObject(), 0,1);
        }
        private void AvoidBattleAfterConversation()
        {
            if (PlayerEncounter.EncounteredParty != null)
                PlayerEncounter.Finish();
        }
        private void GoToHideoutLog()
        {

            AvoidBattleAfterConversation();
            EndCaptivityAction.ApplyByReleasedByChoice(athasScholarHero);
            TextObject textObject = GameTexts.FindText("rf_third_quest_anorit_objective_7");
            textObject.SetCharacterProperties("QUESTGIVER", QuestGiver.CharacterObject);

            goToHideoutLog = AddLog(textObject);
            questHideout = Settlement.Find("hideout_forest_13");
            QuestLibrary.InitializeHideoutIfNeeded(questHideout.Hideout);
            AddTrackedObject(questHideout);
        }
        private DialogFlow HideoutBossDialogFix()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125);

            dialogFlow.AddDialogLine("quest_hideout_boss_dialog_fix", "start", "bandit_hideout_defender", "{=nYCXzAYH}You! You've cut quite a swathe through my men there, damn you. How about we settle this, one-on-one?",
                () => CharacterObject.OneToOneConversationCharacter?.StringId == hideoutBossCharacterId && Settlement.CurrentSettlement == questHideout && goToHideoutLog?.CurrentProgress == 1, null, this);

            return dialogFlow;
        }
        private DialogFlow HideoutBossDialog()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125);

            dialogFlow.AddDialogLine("quest_hideout_boss_dialog_1", "start", "hideout_boss_output_1",GameTexts.FindText("rf_third_quest_boss_dialog_1").ToString(),
                () => CharacterObject.OneToOneConversationCharacter?.StringId == hideoutBossCharacterId && Settlement.CurrentSettlement == questHideout && goToHideoutLog?.CurrentProgress == 2, null, this);

            dialogFlow.AddPlayerLine("quest_hideout_boss_dialog_2", "hideout_boss_output_1", "hideout_boss_output_2", GameTexts.FindText("rf_third_quest_boss_dialog_2").ToString(), null, null, this);
            dialogFlow.AddDialogLine("quest_hideout_boss_dialog_3", "hideout_boss_output_2", "hideout_boss_output_3", GameTexts.FindText("rf_third_quest_boss_dialog_3").ToString(), null, null, this);
            dialogFlow.AddPlayerLine("quest_hideout_boss_dialog_4", "hideout_boss_output_3", "close_window", GameTexts.FindText("rf_third_quest_boss_dialog_4").ToString(), null,
                () =>
                {
                    goToHideoutLog?.UpdateCurrentProgress(4);
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(TheOwl.CharacterObject));
                }, this);
            dialogFlow.AddPlayerLine("quest_hideout_boss_dialog_5", "hideout_boss_output_3", "close_window", GameTexts.FindText("rf_third_quest_boss_dialog_5").ToString(), null, OnCapturedHideoutBoss, this);

            return dialogFlow;
        }
        private DialogFlow AthasPersuasionDialogFlow()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125);

            dialogFlow.AddDialogLine("athas_persuasion_dialog", "start", "athas_persuasion_output",
                GameTexts.FindText("rf_greetings").ToString(), () => CharacterObject.OneToOneConversationCharacter == athasScholarHero.CharacterObject &&
                                                                     persuadeAthasScholarLog?.CurrentProgress == 1 && failedPersuasionLog == null, null, this);

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
                GameTexts.FindText("rf_third_quest_scholar_dialog_12").ToString(), () => !ConversationManager.GetPersuasionProgressSatisfied(), null, this);


            dialogFlow.AddPlayerLine("athas_persuasion_option_1", "athas_persuasion_options", "athas_persuasion_outcome",
                "{=!}{PERSUADE_ATTEMPT_1}", PersuasionOptionCondition_1, PersuasionOptionConsequence_1, this, 100, null, ()=> _persuasionTask.Options.ElementAt(0));

            dialogFlow.AddPlayerLine("athas_persuasion_option_2", "athas_persuasion_options", "athas_persuasion_outcome",
                "{=!}{PERSUADE_ATTEMPT_2}", PersuasionOptionCondition_2, PersuasionOptionConsequence_2, this, 100, null, () => _persuasionTask.Options.ElementAt(1));


            dialogFlow.AddDialogLine("athas_persuasion_success", "athas_persuasion_outcome", "close_window",
                GameTexts.FindText("rf_third_quest_scholar_dialog_persuasion_success").ToString(), () =>
                {
                    GameTexts.SetVariable("SETTLEMENT", Ityr.Name.ToString());
                    return ConversationManager.GetPersuasionProgressSatisfied();
                }, () => PersuasionComplete(true), this);

            dialogFlow.AddDialogLine("athas_persuasion_failed", "athas_persuasion_outcome", "close_window",
                GameTexts.FindText("rf_third_quest_scholar_dialog_persuasion_fail").ToString(),()=> !ConversationManager.GetPersuasionProgressSatisfied(), ()=> PersuasionComplete(false), this);

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


        private void PersuasionComplete(bool success)
        {
            ConversationManager.EndPersuasion();
            if (!success)
            {
                failedPersuasionLog = AddLog(GameTexts.FindText("rf_third_quest_anorit_objective_3_failed"));
                return;
            }
            persuadeAthasScholarLog.UpdateCurrentProgress(2);

            TextObject textObject = GameTexts.FindText("rf_third_quest_anorit_objective_3");
            textObject.SetTextVariable("SETTLEMENT", Ityr.BoundVillages[0].Settlement.EncyclopediaLinkWithName);

            RemoveTrackedObject(Ityr);

            waitAthasScholarLog = AddDiscreteLog(textObject, new TextObject(), 0, 1);
            waitAthasScholarTime = CampaignTime.Now;
            


        }

        private static void OpenMissionWithSettingPreviousLocation(string previousLocationId, string missionLocationId)
        {
            Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId(missionLocationId);
            Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId(previousLocationId);
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, null, null, null);
            Campaign.Current.GameMenuManager.NextLocation = null;
            Campaign.Current.GameMenuManager.PreviousLocation = null;
        }
        private class PersuadeScholarMissionLogic : MissionLogic
        {
            public override InquiryData OnEndMissionRequest(out bool canLeave)
            {
                canLeave = true;
                if (Instance?.persuadeAthasScholarLog?.CurrentProgress == 1 || Instance?.failedPersuasionLog != null)
                {
                    canLeave = false;
                    MBInformationManager.AddQuickInformation(GameTexts.FindText("rf_third_quest_cannot_leave"));
                }
                return null;
            }
        }
        private class FightScholarMissionLogic : MissionLogic
        {
            
            public FightScholarMissionLogic(Action<Agent, Agent, int> agentHitAction)
            {
                OnAgentHitAction = agentHitAction;
            }
            public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
            {
                if (OnAgentHitAction == null)
                {
                    return;
                }
                OnAgentHitAction(affectedAgent, affectorAgent, blow.InflictedDamage);
            }

            private Action<Agent, Agent, int> OnAgentHitAction;
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
