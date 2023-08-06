using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using SandBox.Issues.IssueQuestTasks;
using SandBox.Missions.MissionLogics.Towns;
using StoryMode.Quests.PlayerClanQuests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static Quest.RescueUliahBehavior;
using static TaleWorlds.CampaignSystem.Issues.IssueBase;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace Quest
{
    public class QueenQuest : QuestBase
    {
        public static readonly string _theOwlId = "rf_the_owl";

        [SaveableField(0)]
        public bool hasTalkedToOwl;
        [SaveableField(1)]
        public JournalLog _findMapJournalLog;
        [SaveableField(2)]
        private bool hasTalkedToOwl2;
        [SaveableField(3)] 
        private CampaignTime lastHideoutTime;
        [SaveableField(4)]
        private bool _willTalkToAnoritLord;
        [SaveableField(5)]
        private CampaignTime anoritLordConversationTime;
        [SaveableField(6)]
        public static bool escapedPrison;
        

        public QueenQuest(string text, Hero hero, CampaignTime time, int number, bool alreadyTalkedToOwl) : base(text, hero, time, number)
        {
            if(alreadyTalkedToOwl)
                hasTalkedToOwl = true;
            else
                this.AddLog(GameTexts.FindText("rf_second_quest_first_log"));
            this.SetDialogs();
            hasTalkedToOwl = false;
            hasTalkedToOwl2 = false;
            _willTalkToAnoritLord = false;
            lastHideoutTime = CampaignTime.Never;
            anoritLordConversationTime = CampaignTime.Never;
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
            CampaignEvents.OnHideoutDeactivatedEvent.AddNonSerializedListener(this, OnHideoutDefeat);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourTick);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, TickEvent);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, imission =>
            {
                Mission mission = (Mission)imission;
                if(_findMapJournalLog != null &&  hasTalkedToOwl && _findMapJournalLog.CurrentProgress < 3 && mission != null && PlayerEncounter.InsideSettlement && Settlement.CurrentSettlement?.IsHideout == true &&
                   (Settlement.CurrentSettlement.Hideout.StringId == "hideout_seaside_11" ||
                    Settlement.CurrentSettlement.Hideout.StringId == "hideout_seaside_13" ||
                    Settlement.CurrentSettlement.Hideout.StringId == "hideout_seaside_14"))
                    mission.AddMissionBehavior(new FindRelicsHideoutMissionBehavior(_findMapJournalLog));
            });

        }
        private bool isOwlOnPlayerParty => PartyBase.MainParty.MemberRoster.GetTroopRoster().Any(x => x.Character.HeroObject?.StringId == "the_owl_hero");
        private void HourTick()
        {
            if (_findMapJournalLog?.CurrentProgress == 3 && !hasTalkedToOwl2 && lastHideoutTime != CampaignTime.Never && lastHideoutTime.ElapsedHoursUntilNow >= 2 && !PlayerEncounter.InsideSettlement)
            {
                if(!isOwlOnPlayerParty)
                {
                    Clan clan = Clan.FindFirst(x => x.StringId == "clan_empire_north_7");
                    Hero hero;
                    if (!Hero.AllAliveHeroes.Any(x => x.StringId == "the_owl_hero"))
                    {
                        
                        hero = HeroCreator.CreateSpecialHero(
                        MBObjectManager.Instance.GetObject<CharacterObject>(_theOwlId), QuestGiver.HomeSettlement, clan,
                        clan, 30);
                        hero.StringId = "the_owl_hero";
                        
                    }
                    else
                        hero = Hero.AllAliveHeroes.First(x => x.StringId == "the_owl_hero");

                    MobileParty mobileParty = MobileParty.All.FirstOrDefault(x => x.StringId == "owl_party") ?? LordPartyComponent.CreateLordParty("owl_party", hero,
                        MobileParty.MainParty.Position2D, 1f, QuestGiver.HomeSettlement, hero);

                    mobileParty.InitializeMobilePartyAroundPosition(QuestGiver.Clan.DefaultPartyTemplate, MobileParty.MainParty.Position2D, 1f, 0, 80);
                    mobileParty.StringId = "owl_party";
                    mobileParty.Ai.SetMoveEngageParty(MobileParty.MainParty);
                }
                else
                {
                    Campaign.Current.ConversationManager.OpenMapConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(PartyBase.MainParty.MemberRoster.GetTroopRoster().First(x => x.Character.HeroObject?.StringId == "the_owl_hero").Character, null));
                }
                lastHideoutTime = CampaignTime.Never;
                anoritLordConversationTime = CampaignTime.Never;
            }
            if(anoritLordConversationTime != CampaignTime.Never && anoritLordConversationTime.ElapsedHoursUntilNow >= 40 && !PlayerEncounter.InsideSettlement && CampaignTime.Now.IsNightTime)
            {
                GameStateManager.Current.PushState(GameStateManager.Current.CreateState<RFNotificationState>(GameTexts.FindText("rf_kidnapped_text").ToString(), 40, () => { OpenPrisonBreak();  }));
                anoritLordConversationTime = CampaignTime.Never;
                Campaign.Current.CampaignBehaviorManager.AddBehavior(new SaveCurrentQuestCampaignBehavior("queen"));
            }
        }
        public void OnHideoutDefeat(Settlement hideout)
        {

            if (hideout.IsHideout && !hideout.Hideout.IsInfested && hasTalkedToOwl)
            {
                Hideout nextHideout;
                switch (hideout.Hideout.StringId)
                {

                    case "hideout_seaside_13":
                        if (_findMapJournalLog.CurrentProgress == 0)
                        {
                            InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_map_not_found_inquiry").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", null, null), true);
                            ActivateHideout("hideout_seaside_13");
                            return;
                        }
                        this.RemoveTrackedObject(hideout);
                        nextHideout = ActivateHideout("hideout_seaside_14");
                        this.AddTrackedObject(nextHideout.Settlement);
                        break;
                    case "hideout_seaside_14":
                        if (_findMapJournalLog.CurrentProgress == 1)
                        {
                            InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_map_not_found_inquiry").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", null, null), true);
                            ActivateHideout("hideout_seaside_14");
                            return;
                        }
                        this.RemoveTrackedObject(hideout);
                        nextHideout = ActivateHideout("hideout_seaside_11");
                        this.AddTrackedObject(nextHideout.Settlement);
                        break;
                    case "hideout_seaside_11":
                        if (_findMapJournalLog.CurrentProgress == 2)
                        {
                            InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("rf_event").ToString(), GameTexts.FindText("rf_map_not_found_inquiry").ToString(), true, false, new TextObject("{=continue}Continue").ToString(), "", null, null), true);
                            ActivateHideout("hideout_seaside_11");
                            return;
                        }
                        lastHideoutTime = CampaignTime.Now;
                        break;
                }
            }
        }

        public override TextObject Title => GameTexts.FindText("rf_second_quest_title");

        public override bool IsRemainingTimeHidden => true;
        public override bool IsSpecialQuest => true;

        protected override void InitializeQuestOnGameLoad()
        {
            this.SetDialogs();

        }
        private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
            if (settlement.IsTown && CampaignMission.Current != null && !hasTalkedToOwl)
            {
                Location location = CampaignMission.Current.Location;
                if (location != null && settlement.StringId == QuestGiver.HomeSettlement.StringId && location.StringId == "tavern")
                {
                    LocationCharacter locationCharacter = CreateTheOwl(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                    location.AddCharacter(locationCharacter);
                }
            }
        }
        private static LocationCharacter CreateTheOwl(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>(_theOwlId);
            int minValue;
        
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(@object, out minValue, out maxValue, "");

            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement");
            AgentData agentData = new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age((int)@object.Age).BodyProperties(@object.GetBodyProperties(@object.Equipment, 0));
            var owl = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_tavern_townsman", true, relation, null, true, false, null, false, false, false);
            owl.PrefabNamesForBones.Add(agentData.AgentMonster.OffHandItemBoneIndex, "kitchen_pitcher_b_tavern");
            return owl;
            //
        }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(TheOwlInterceptDialog);
            Campaign.Current.ConversationManager.AddDialogFlow(TheOwlDialog);
            Campaign.Current.ConversationManager.AddDialogFlow(TheOwlDialogGoodLuck);
            
            Campaign.Current.ConversationManager.AddDialogFlow(AnoritLordDialog);

            
        }


        public void OwlDialogConsequence()
        {
            hasTalkedToOwl = true;
            Hideout hideout = ActivateHideout("hideout_seaside_13");

            _findMapJournalLog = this.AddDiscreteLog(GameTexts.FindText("rf_second_quest_find_map_log"), GameTexts.FindText("rf_second_quest_first_part_log_task"), 0, 3);
            this.AddTrackedObject((ITrackableCampaignObject)hideout.Settlement);
        }

        public static Hideout ActivateHideout(string id)
        {
            Hideout hideout = Hideout.All.Find(x => x.StringId == id);
            if (!hideout.IsInfested)
            {
                for (int i = 0; i < 2 && !hideout.IsInfested; i++)
                {
                    MobileParty bandits = BanditPartyComponent.CreateBanditParty("bandits_quest_" + i, hideout.Settlement.OwnerClan, hideout, i == 2 ? true : false);
                    bandits.InitializeMobilePartyAtPosition(hideout.Settlement.OwnerClan.DefaultPartyTemplate, hideout.Settlement.Position2D);
                    bandits.Ai.SetMoveGoToSettlement(hideout.Settlement);
                    bandits.Ai.RecalculateShortTermAi();
                    bandits.Ai.SetMoveGoToSettlement(hideout.Settlement);
                    EnterSettlementAction.ApplyForParty(bandits, hideout.Settlement);
                }
                AccessTools.Field(typeof(Hideout), "_nextPossibleAttackTime").SetValue(hideout, CampaignTime.Now);
                hideout.Settlement.IsVisible = true;
                hideout.IsSpotted = true;
            }
            else if (!hideout.Settlement.IsVisible) 
                hideout.IsSpotted = true;
            hideout.Settlement.IsVisible = true;
            return hideout;
        }


        public static LocationCharacter locationCharacterQuest = null;

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
            escapedPrison = true;
        }

        private void GoToAnoritLord(Hero owl)
        {
            Hero anoritLord = Hero.FindFirst(x => x.StringId == "lord_WE9_l");
            Settlement settlement = Settlement.Find("town_EW3");



            Vec2 mainPartyPos = settlement.Position2D;

            MobileParty anoritLordParty;

            if (anoritLord.PartyBelongedTo != null)
                anoritLordParty = anoritLord.PartyBelongedTo;
            else
            {
                anoritLordParty = LordPartyComponent.CreateLordParty("anorit_lord_party", anoritLord, mainPartyPos, 1f, settlement, anoritLord);
                anoritLordParty.InitializeMobilePartyAroundPosition(anoritLord.Clan.DefaultPartyTemplate, mainPartyPos, 1f, 0f, 150);
            }

            EnterSettlementAction.ApplyForParty(anoritLordParty, settlement);
            anoritLordParty.Ai.RecalculateShortTermAi();

            

            if (owl != null)
            {
                EnterSettlementAction.ApplyForParty(owl.PartyBelongedTo, settlement);
                Vec2 pos = owl.PartyBelongedTo.Position2D;
                pos.x += 1;
                pos.y += 1;
                MobileParty.MainParty.Position2D = pos;

                owl.PartyBelongedTo.Ai.SetMoveGoToSettlement(settlement);
            }
            else
            {
                if(isOwlOnPlayerParty)
                TeleportHeroAction.ApplyDelayedTeleportToSettlement(Hero.FindFirst(x => x.StringId == "the_owl_hero"), QuestGiver.HomeSettlement);
                Vec2 pos = anoritLordParty.Position2D;
                pos.x += 0.2f;
                pos.x += 0.2f;
                MobileParty.MainParty.Position2D = pos;
            }

            PlayerEncounter.Finish();

            _willTalkToAnoritLord = true;
            hasTalkedToOwl2 = true;
       
        }
        
        private void TickEvent(float delta)
        {
            if(_willTalkToAnoritLord)
            {
                Hero anoritHero = Hero.FindFirst(x => x.StringId == "lord_WE9_l");
                ConversationCharacterData playerData = new(CharacterObject.PlayerCharacter, PartyBase.MainParty);
                ConversationCharacterData anoritData = new(anoritHero.CharacterObject, anoritHero.PartyBelongedTo.Party);
                Campaign.Current.ConversationManager.OpenMapConversation(playerData, anoritData);
                _willTalkToAnoritLord = false;
            }
        }

        private void BetrayQueenConsequence(Hero anoritLord)
        {
            DisableQueen();
            this.CompleteQuestWithBetrayal();
            DeclareWarAction.ApplyByDefault(Clan.PlayerClan.Kingdom ?? (IFaction)Clan.PlayerClan, QuestGiver.Clan.Kingdom);
            ChangeRelationAction.ApplyPlayerRelation(QuestGiver, -40, false);
            PlayerEncounter.Finish();
            new AnoritFindRelicsQuest("anorit_quest", anoritLord, CampaignTime.Never, 50000).StartQuest();
        }
        private void DisableQueen()
        {
            if (QuestGiver.PartyBelongedTo == null)
                LordPartyComponent.CreateLordParty("queen_party", QuestGiver, new Vec2(0, 0), 0f, QuestGiver.HomeSettlement, QuestGiver);
            else
                QuestGiver.PartyBelongedTo.Position2D = new Vec2(0, 0);
        }
        private void NotBetrayQueenConsequence()
        {
            DisableQueen();
            ChangeRelationAction.ApplyPlayerRelation(Hero.FindFirst(x=>x.StringId=="the_owl_hero"), -20, false);
            PlayerEncounter.Finish();
            anoritLordConversationTime = CampaignTime.Now;
            
            //Declarar guerra aos anorit
        }
        private DialogFlow TheOwlDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_1"))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == _theOwlId && !hasTalkedToOwl)
            .NpcLine(GameTexts.FindText("rf_the_owl_text_2"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_3")).NpcLine(GameTexts.FindText("rf_the_owl_text_4"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_5")).NpcLine(GameTexts.FindText("rf_the_owl_text_6"))
            .NpcLine(GameTexts.FindText("rf_the_owl_text_7")).NpcLine(GameTexts.FindText("rf_the_owl_text_8"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_9")).NpcLine(GameTexts.FindText("rf_the_owl_text_10"))
            .PlayerLine(GameTexts.FindText("rf_the_owl_text_11")).NpcLine(GameTexts.FindText("rf_the_owl_text_12")).Consequence(OwlDialogConsequence).CloseDialog();
        private DialogFlow TheOwlDialogGoodLuck => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(GameTexts.FindText("rf_the_owl_text_13")).Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == _theOwlId && hasTalkedToOwl).CloseDialog();


        private DialogFlow TheOwlInterceptDialog => DialogFlow.CreateDialogFlow("start", 125).BeginNpcOptions().NpcOption(GameTexts.FindText("rf_the_owl_intercept_text_4").ToString(),
            () => MobileParty.ConversationParty?.LeaderHero?.StringId == "the_owl_hero" && !hasTalkedToOwl2 && _findMapJournalLog?.CurrentProgress == 3 && !isOwlOnPlayerParty)
            .PlayerLine(GameTexts.FindText("rf_the_owl_intercept_text_2")).NpcLine(GameTexts.FindText("rf_the_owl_intercept_text_3"))
            .Consequence(() =>GoToAnoritLord(isOwlOnPlayerParty ? null : Hero.OneToOneConversationHero)).CloseDialog()
                .NpcOption(GameTexts.FindText("rf_the_owl_intercept_text").ToString(), ()=> Hero.OneToOneConversationHero?.StringId == "the_owl_hero" && !hasTalkedToOwl2 && _findMapJournalLog?.CurrentProgress == 3 
                && isOwlOnPlayerParty).PlayerLine(GameTexts.FindText("rf_the_owl_intercept_text_2")).NpcLine(GameTexts.FindText("rf_the_owl_intercept_text_3"))
            .Consequence(() => GoToAnoritLord(isOwlOnPlayerParty ? null : Hero.OneToOneConversationHero)).CloseDialog().EndNpcOptions();
                
        
        private DialogFlow AnoritLordDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_anorit_lord_text_1")).Condition(() => Hero.OneToOneConversationHero?.StringId == "lord_WE9_l" && _willTalkToAnoritLord).NpcLine(GameTexts.FindText("rf_anorit_lord_text_2")).
            PlayerLine(GameTexts.FindText("rf_anorit_lord_text_3")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_4")).PlayerLine(GameTexts.FindText("rf_anorit_lord_text_5"))
            .NpcLine(GameTexts.FindText("rf_anorit_lord_text_6")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_7")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_8"))
            .PlayerLine(GameTexts.FindText("rf_anorit_lord_text_9")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_10")).PlayerLine(GameTexts.FindText("rf_anorit_lord_text_11"))
            .NpcLine(GameTexts.FindText("rf_anorit_lord_text_12")).PlayerLine(GameTexts.FindText("rf_anorit_lord_text_13")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_14"))
            .PlayerLine(GameTexts.FindText("rf_anorit_lord_text_15")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_16")).BeginPlayerOptions()
            .PlayerOption(GameTexts.FindText("rf_anorit_lord_option_1")).NpcLine(GameTexts.FindText("rf_anorit_lord_text_17")).Consequence(()=>BetrayQueenConsequence(Hero.OneToOneConversationHero)).CloseDialog()
            .PlayerOption(GameTexts.FindText("rf_anorit_lord_option_2")).Consequence(NotBetrayQueenConsequence).CloseDialog().EndPlayerOptions();


        protected override void HourlyTick()
        {
        }
    }
}
