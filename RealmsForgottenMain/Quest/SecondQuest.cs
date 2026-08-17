using RealmsForgotten.Quest.MissionBehaviors;
using RealmsForgotten.Quest.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static RealmsForgotten.Quest.QuestLibrary;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace RealmsForgotten.Quest
{
    public class SecondQuest : QuestBase
    {
        public static readonly string TheOwlId = "rf_the_owl";
        private const string HellboundClanId = "hellbound_outlaw";
        private const string HellboundPartyTemplateId = "hellbound_outlaw_template";
        private const string HellboundBossPartyTemplateId = "hellbound_outlaw_boss_party_template";

        [SaveableField(0)]
        public bool HasTalkedToOwl;
        [SaveableField(1)]
        private JournalLog? findMapJournalLog;
        [SaveableField(2)]
        private bool hasTalkedToOwl2;
        [SaveableField(3)]
        private CampaignTime lastHideoutTime;
        [SaveableField(4)]
        private CampaignTime anoritLordConversationTime;
        [SaveableField(5)]
        private bool _isPlayerInOwlArmy;
        [SaveableField(6)]
        private bool escapedPrison;
        public SecondQuest(string text, Hero hero, CampaignTime time, int number, bool alreadyTalkedToOwl) : base(text, hero, time, number)
        {
            if (alreadyTalkedToOwl)
                HasTalkedToOwl = true;
            else
                AddLog(GameTexts.FindText("rf_second_quest_first_log"));

            SetDialogs();

            lastHideoutTime = CampaignTime.Never;
            anoritLordConversationTime = CampaignTime.Never;
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnHideoutDefeat);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, imission =>
            {
                Mission mission = (Mission)imission;
                if (HasTalkedToOwl && findMapJournalLog?.CurrentProgress < 3 && mission != null && PlayerEncounter.InsideSettlement && Settlement.CurrentSettlement?.IsHideout == true &&
                   (Settlement.CurrentSettlement.Hideout.StringId == "hideout_seaside_11" ||
                    Settlement.CurrentSettlement.Hideout.StringId == "hideout_seaside_13" ||
                    Settlement.CurrentSettlement.Hideout.StringId == "hideout_seaside_14"))

                    mission.AddMissionBehavior(new FindRelicsHideoutMissionBehavior(findMapJournalLog));
            });
            RegisterQuestEvents(this);
        }
        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            bool owlEscortArrived = mobileParty == MobileParty.MainParty || mobileParty?.LeaderHero == TheOwl;
            if (_isPlayerInOwlArmy && owlEscortArrived && settlement?.StringId == "town_EW3")
            {
                Hero anoritLord = AnoritLord;
                ConversationManager conversationManager = Campaign.Current?.ConversationManager;
                if (anoritLord == null || conversationManager == null)
                    return;

                EnterSettlementAction.ApplyForCharacterOnly(anoritLord, settlement);

                ConversationCharacterData playerData = new(CharacterObject.PlayerCharacter, PartyBase.MainParty);
                ConversationCharacterData anoritData = new(anoritLord.CharacterObject, anoritLord.PartyBelongedTo?.Party ?? settlement.Party);
                conversationManager.OpenMapConversation(playerData, anoritData);
                _isPlayerInOwlArmy = false;
                MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
                QuestPatches.AvoidDisbanding = false;
            }
            if (mobileParty == MobileParty.MainParty && QuestHelperCampaignBehavior.IsInHideoutForQuest2())
            {
                InitializeSecondQuestHideout(settlement.Hideout);
                while (settlement.Hideout.GetDefenderParties(MapEvent.BattleTypes.Hideout).Sum(p => p.MemberRoster.TotalManCount) < 60)
                {
                    if (!AddHellboundPartyToHideout(settlement.Hideout, false))
                        break;
                }
            }
        }

        private static void InitializeSecondQuestHideout(Hideout hideout, bool reset = false)
        {
            if (hideout == null)
                return;

            Clan hellboundClan = Clan.FindFirst(x => x.StringId == HellboundClanId);
            if (hellboundClan == null)
                return;

            List<PartyBase> defenders = hideout.GetDefenderParties(MapEvent.BattleTypes.Hideout).ToList();
            if (reset || defenders.Any(p => p.MapFaction != hellboundClan))
            {
                foreach (PartyBase defender in defenders.Where(p => p?.MobileParty?.IsActive == true).ToList())
                    DestroyPartyAction.Apply(null, defender.MobileParty);

                defenders.Clear();
            }

            if (!hideout.IsInfested || defenders.Count == 0)
            {
                AddHellboundPartyToHideout(hideout, false);
                AddHellboundPartyToHideout(hideout, false);
                AddHellboundPartyToHideout(hideout, true);
                HarmonyLib.AccessTools.Field(typeof(Hideout), "_nextPossibleAttackTime").SetValue(hideout, CampaignTime.Now);
                hideout.IsSpotted = true;
            }

            hideout.Settlement.IsVisible = true;
        }

        private static bool AddHellboundPartyToHideout(Hideout hideout, bool isBossParty)
        {
            Clan hellboundClan = Clan.FindFirst(x => x.StringId == HellboundClanId);
            string templateId = isBossParty ? HellboundBossPartyTemplateId : HellboundPartyTemplateId;
            PartyTemplateObject partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>(templateId);
            if (hellboundClan == null || partyTemplate == null)
                return false;

            string partyId = $"second_quest_hellbound_{hideout.StringId}_{Guid.NewGuid():N}";
            MobileParty bandits = BanditPartyComponent.CreateBanditParty(
                partyId,
                hellboundClan,
                hideout,
                isBossParty,
                partyTemplate,
                hideout.Settlement.Position);
            if (bandits == null)
                return false;

            bandits.InitializeMobilePartyAtPosition(partyTemplate, hideout.Settlement.Position);
            bandits.SetMoveGoToSettlement(hideout.Settlement, MobileParty.NavigationType.All, false);
            bandits.RecalculateShortTermBehavior();
            EnterSettlementAction.ApplyForParty(bandits, hideout.Settlement);
            return true;
        }

        private bool isOwlOnPlayerParty => PartyBase.MainParty.MemberRoster.GetTroopRoster().Any(x => x.Character?.HeroObject == TheOwl);

        public void OnHideoutDefeat(IMission iMission)
        {
            if (Settlement.CurrentSettlement?.IsHideout == true)
            {
                Settlement hideout = Settlement.CurrentSettlement;
                if (hideout.IsHideout && HasTalkedToOwl)
                {
                    Hideout nextHideout;
                    switch (hideout.Hideout.StringId)
                    {

                        case "hideout_seaside_13":
                            if (findMapJournalLog?.CurrentProgress == 0)
                            {
                                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_map_not_found_inquiry").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", null, null), true);
                                InitializeSecondQuestHideout(hideout.Hideout, true);
                                return;
                            }
                            this.RemoveTrackedObject(hideout);
                            nextHideout = Settlement.Find("hideout_seaside_14").Hideout;
                            InitializeSecondQuestHideout(nextHideout);
                            this.AddTrackedObject(nextHideout.Settlement);
                            break;
                        case "hideout_seaside_14":
                            if (findMapJournalLog?.CurrentProgress == 1)
                            {
                                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_map_not_found_inquiry").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", null, null), true);
                                InitializeSecondQuestHideout(hideout.Hideout, true);
                                return;
                            }
                            this.RemoveTrackedObject(hideout);
                            nextHideout = Settlement.Find("hideout_seaside_11").Hideout;
                            InitializeSecondQuestHideout(nextHideout);
                            this.AddTrackedObject(nextHideout.Settlement);
                            break;
                        case "hideout_seaside_11":
                            if (findMapJournalLog?.CurrentProgress == 2)
                            {
                                InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_map_not_found_inquiry").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", null, null), true);
                                InitializeSecondQuestHideout(hideout.Hideout, true);
                                return;
                            }
                            this.RemoveTrackedObject(hideout);
                            lastHideoutTime = CampaignTime.Now;
                            break;
                    }
                }
            }
        }
        protected void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
        }
        public override TextObject Title => GameTexts.FindText("rf_second_quest_title");

        public override bool IsRemainingTimeHidden => true;
        public override string SpecialQuestType => "RfMainQuest";

        protected override void InitializeQuestOnGameLoad()
        {
            QuestLibrary.InitializeVariables();
            SetDialogs();
            QuestPatches.AvoidDisbanding = _isPlayerInOwlArmy;
            if (_isPlayerInOwlArmy)
            {
                Hero owl = TheOwl;
                CreateOwlArmy(owl == null ? null : MobileParty.All.FirstOrDefault(x => x?.LeaderHero == owl));
            }
        }
        private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
            if (settlement.IsTown && CampaignMission.Current != null && !HasTalkedToOwl)
            {
                Location location = CampaignMission.Current.Location;
                if (location != null 
                    && (settlement.StringId == QuestGiver.HomeSettlement.StringId 
                    || QuestGiver.CurrentSettlement != null && QuestGiver.CurrentSettlement == Hero.MainHero.CurrentSettlement
                    && location.StringId == "tavern"))
                {
                    LocationCharacter locationCharacter = CreateTheOwl(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                    location.AddCharacter(locationCharacter);
                }
            }
        }
        private static LocationCharacter CreateTheOwl(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>(TheOwlId);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(@object, out int minValue, out int maxValue, "");
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement");
            AgentData agentData = new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age((int)@object.Age).BodyProperties(@object.GetBodyProperties(@object.Equipment, 0));
            var owl = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_tavern_townsman", true, relation, null, true, false, null, false, false, false);
            owl.PrefabNamesForBones.Add(agentData.AgentMonster.OffHandItemBoneIndex, "kitchen_pitcher_b_tavern");
            return owl;
        }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(TheOwlInterceptDialog());
            Campaign.Current.ConversationManager.AddDialogFlow(TheOwlDialog);
            Campaign.Current.ConversationManager.AddDialogFlow(TheOwlDialogGoodLuck);
            Campaign.Current.ConversationManager.AddDialogFlow(AnoritLordDialog);
        }


        public void OwlDialogConsequence()
        {
            HasTalkedToOwl = true;
            Hideout hideout = Settlement.Find("hideout_seaside_13").Hideout;
            InitializeSecondQuestHideout(hideout);

            findMapJournalLog = this.AddDiscreteLog(GameTexts.FindText("rf_second_quest_find_map_log"), GameTexts.FindText("rf_second_quest_first_part_log_task"), 0, 3);
            this.AddTrackedObject((ITrackableCampaignObject)hideout.Settlement);
        }


        public static void OpenPrisonBreak()
        {
            Settlement settlement = Settlement.Find("town_B4");
            PlayerEncounter.Start();
            EnterSettlementAction.ApplyForParty(MobileParty.MainParty, settlement);
            PlayerEncounter.LocationEncounter = new TownEncounter(settlement);
            EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
            EncounterManager.StartPartyEncounter(PartyBase.MainParty, settlement.Party);
            PlayerEncounter.EnterSettlement();

            PlayerEncounter.Current.SetupFields(PartyBase.MainParty, settlement.Party);

            RFPrisonBreakMissionController.OpenPrisonBreakMission(settlement.LocationComplex.GetScene("prison", 0), settlement.LocationComplex.GetLocationWithId("prison"));

        }

        private bool CreateOwlArmy(MobileParty owlParty)
        {
            Settlement settlement = Settlement.Find("town_EW3");
            MobileParty mainParty = MobileParty.MainParty;
            Hero owl = TheOwl;
            Hero questGiver = QuestGiver;
            Kingdom kingdom = questGiver?.Clan?.Kingdom;

            if (settlement == null || mainParty == null || owl == null || questGiver?.HomeSettlement == null ||
                questGiver.Culture?.DefaultPartyTemplate == null || kingdom == null)
                return false;

            if (owlParty == null || !owlParty.IsActive)
            {
                owlParty = LordPartyComponent.CreateLordParty("the_owl_party", owl, CampaignVec2.Zero, 0f,
                    questGiver.HomeSettlement, owl);
                if (owlParty == null)
                    return false;

                owlParty.InitializeMobilePartyAtPosition(questGiver.Culture.DefaultPartyTemplate, new(mainParty.GetPosition2D, !mainParty.IsCurrentlyAtSea));
            }

            Army owlArmy = owlParty.Army;
            bool createdArmy = owlArmy == null || owlArmy.LeaderParty != owlParty;
            if (createdArmy)
            {
                owlArmy = new Army(kingdom, owlParty, Army.ArmyTypes.Patrolling);
                owlParty.Army = owlArmy;
            }

            mainParty.Army = owlArmy;

            if (!owlArmy.DoesLeaderPartyAndAttachedPartiesContain(mainParty))
                owlArmy.AddPartyToMergedParties(mainParty);

            MobileParty leaderParty = owlArmy.LeaderParty;
            if (leaderParty == null)
                return false;

            owlArmy.AiBehaviorObject = settlement;
            leaderParty.SetMoveGoToSettlement(settlement, MobileParty.NavigationType.All, false);

            leaderParty.Ai?.SetDoNotMakeNewDecisions(true);

            leaderParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
            mainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);

            if (createdArmy)
            {
                leaderParty.SpeedExplained.AddFactor(1.0f);
                owlArmy.DailyCohesionChangeExplanation.Add(100f);
            }
            owlArmy.Cohesion = 100f;

            QuestPatches.AvoidDisbanding = true;
            _isPlayerInOwlArmy = true;
            return true;
        }
        private void GoToAnoritLord()
        {
            MobileParty owlParty;

            if (!isOwlOnPlayerParty && MobileParty.All.Any(x => x.LeaderHero == Hero.OneToOneConversationHero))
                owlParty = MobileParty.All.First(x => x.LeaderHero == Hero.OneToOneConversationHero);
            else
            {
                owlParty = LordPartyComponent.CreateLordParty("owl_party", Hero.OneToOneConversationHero,
                    new(MobileParty.MainParty.GetPosition2D, !MobileParty.MainParty.IsCurrentlyAtSea), 1f, QuestGiver.HomeSettlement, Hero.OneToOneConversationHero);
                owlParty.InitializeMobilePartyAroundPosition(TroopRoster.CreateDummyTroopRoster(), TroopRoster.CreateDummyTroopRoster(), new(MobileParty.MainParty.GetPosition2D, !MobileParty.MainParty.IsCurrentlyAtSea), 1f);
            }

            CreateOwlArmy(owlParty);

            hasTalkedToOwl2 = true;
        }


        protected override void OnStartQuest()
        {
            SetDialogs();
        }

        private void BetrayQueenConsequence(Hero anoritLord)
        {
            DisbandArmyAction.ApplyByObjectiveFinished(MobileParty.MainParty.Army);
            this.CompleteQuestWithBetrayal();
            DeclareWarAction.ApplyByDefault(Clan.PlayerClan.Kingdom ?? (IFaction)Clan.PlayerClan, QuestGiver.Clan.Kingdom);
            ChangeRelationAction.ApplyPlayerRelation(QuestGiver, -40, false);
            PlayerEncounter.Finish();
            new AnoritFindRelicsQuest("anorit_quest", anoritLord, CampaignTime.Never, 50000).StartQuest();
        }
        private void NotBetrayQueenConsequence()
        {
            DisbandArmyAction.ApplyByObjectiveFinished(MobileParty.MainParty.Army);
            ChangeRelationAction.ApplyPlayerRelation(TheOwl, -20, false);
            PlayerEncounter.Finish();
            anoritLordConversationTime = CampaignTime.Now;
        }
        private DialogFlow TheOwlDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_1"))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == TheOwlId && !HasTalkedToOwl)
            .NpcLine(GameTexts.FindText("rf_the_owl_text_2"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_3")).NpcLine(GameTexts.FindText("rf_the_owl_text_4"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_5")).NpcLine(GameTexts.FindText("rf_the_owl_text_6"))
            .NpcLine(GameTexts.FindText("rf_the_owl_text_7")).NpcLine(GameTexts.FindText("rf_the_owl_text_8"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_9")).NpcLine(GameTexts.FindText("rf_the_owl_text_10"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_11")).NpcLine(GameTexts.FindText("rf_the_owl_text_12")).Consequence(OwlDialogConsequence).CloseDialog();
        private DialogFlow TheOwlDialogGoodLuck => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_the_owl_text_13")).Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == TheOwlId && HasTalkedToOwl).CloseDialog();


        private DialogFlow TheOwlInterceptDialog()
        {
            DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions()
                .NpcOption(GameTexts.FindText("rf_the_owl_intercept_text_4").ToString(), () => MobileParty.ConversationParty?.LeaderHero?.StringId == TheOwl?.StringId && !hasTalkedToOwl2 && findMapJournalLog?.CurrentProgress == 3 && !isOwlOnPlayerParty)
                .PlayerLine(GameTexts.FindText("rf_the_owl_intercept_text_2").ToString()).NpcLine(GameTexts.FindText("rf_the_owl_intercept_text_3").ToString()).Consequence(GoToAnoritLord).CloseDialog()
                .NpcOption(GameTexts.FindText("rf_the_owl_intercept_text").ToString(), () => Hero.OneToOneConversationHero == TheOwl && !hasTalkedToOwl2 && findMapJournalLog?.CurrentProgress == 3 && isOwlOnPlayerParty)
                .PlayerLine(GameTexts.FindText("rf_the_owl_intercept_text_2").ToString()).NpcLine(GameTexts.FindText("rf_the_owl_intercept_text_3").ToString()).Consequence(GoToAnoritLord).CloseDialog().EndNpcOptions();

            return dialogFlow;


        }


        private DialogFlow AnoritLordDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_anorit_lord_text_1")).Condition(() => Hero.OneToOneConversationHero == AnoritLord && _isPlayerInOwlArmy).NpcLine(GameTexts.FindText("rf_anorit_lord_text_2")).
            PlayerLine(GameTexts.FindText("rf_anorit_lord_text_3")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_4")).PlayerLine(GameTexts.FindText("rf_anorit_lord_text_5"))
            .NpcLine(GameTexts.FindText("rf_anorit_lord_text_6")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_7")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_8"))
            .PlayerLine(GameTexts.FindText("rf_anorit_lord_text_9")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_10")).PlayerLine(GameTexts.FindText("rf_anorit_lord_text_11"))
            .NpcLine(GameTexts.FindText("rf_anorit_lord_text_12")).PlayerLine(GameTexts.FindText("rf_anorit_lord_text_13")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_14"))
            .PlayerLine(GameTexts.FindText("rf_anorit_lord_text_15")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_16")).BeginPlayerOptions()
            .PlayerOption(GameTexts.FindText("rf_anorit_lord_option_1")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_17")).Consequence(() => BetrayQueenConsequence(Hero.OneToOneConversationHero)).CloseDialog()
            .PlayerOption(GameTexts.FindText("rf_anorit_lord_option_2")).Consequence(NotBetrayQueenConsequence).CloseDialog().EndPlayerOptions();


        protected override void HourlyTick()
        {
            if (_isPlayerInOwlArmy)
            {
                Hero owl = TheOwl;
                MobileParty owlParty = owl == null ? null : MobileParty.All.FirstOrDefault(x => x?.LeaderHero == owl);
                Army owlArmy = MobileParty.MainParty?.Army;

                if (owlParty == null || owlArmy?.LeaderParty != owlParty)
                {
                    CreateOwlArmy(owlParty);
                }
                else
                {
                    Settlement destination = Settlement.Find("town_EW3");
                    if (destination != null && owlParty.CurrentSettlement == destination)
                    {
                        OnSettlementEntered(owlParty, destination, owl);
                    }
                    else if (destination != null && owlParty.MapEvent == null &&
                             owlParty.TargetSettlement?.StringId != destination.StringId)
                    {
                        owlArmy.AiBehaviorObject = destination;
                        owlParty.Ai?.SetDoNotMakeNewDecisions(false);
                        owlParty.SetMoveGoToSettlement(destination, MobileParty.NavigationType.All, false);
                        owlParty.Ai?.SetDoNotMakeNewDecisions(true);
                    }
                }
            }

            if (findMapJournalLog?.CurrentProgress == 3 && !hasTalkedToOwl2 && lastHideoutTime != CampaignTime.Never && lastHideoutTime.ElapsedHoursUntilNow >= 2 && !PlayerEncounter.InsideSettlement)
            {
                MobileParty mobileParty = null;
                if (!isOwlOnPlayerParty)
                {
                    Clan clan = Clan.FindFirst(x => x.StringId == "clan_empire_north_7");

                    mobileParty = MobileParty.All.FirstOrDefault(x => x?.StringId == "owl_party") ?? LordPartyComponent.CreateLordParty("owl_party", TheOwl,
                        new(MobileParty.MainParty.GetPosition2D, !MobileParty.MainParty.IsCurrentlyAtSea), 1f, QuestGiver.HomeSettlement, TheOwl);

                    mobileParty.InitializeMobilePartyAroundPosition(QuestGiver.Clan.DefaultPartyTemplate, new(MobileParty.MainParty.GetPosition2D, !MobileParty.MainParty.IsCurrentlyAtSea), 1f, 0);
                    mobileParty.StringId = "owl_party";
                }

                Campaign.Current.ConversationManager.OpenMapConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(TheOwl.CharacterObject, mobileParty != null ? mobileParty.Party : null));

                lastHideoutTime = CampaignTime.Never;
                anoritLordConversationTime = CampaignTime.Never;
            }
            if (anoritLordConversationTime != CampaignTime.Never && anoritLordConversationTime.ElapsedHoursUntilNow >= 40 && !PlayerEncounter.InsideSettlement && CampaignTime.Now.IsNightTime)
            {
                escapedPrison = true;

                QuestUIManager.ShowNotification(GameTexts.FindText("rf_kidnapped_text").ToString(), () => { SaveCurrentQuestCampaignBehavior.Instance.SaveQuestState("queen"); }, true, "prisoner_image");

                anoritLordConversationTime = CampaignTime.Never;
            }
        }
    }
}
