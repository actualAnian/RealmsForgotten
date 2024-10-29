using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Conversation;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameMenus;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using RealmsForgotten.Quest.KnightQuest;

namespace RealmsForgotten.Quest
{

    public class SpawnNpcInLordsHallBecomeKnightBehavior : CampaignBehaviorBase
    {
        [SaveableField(1)]
        private BecomeKnightQuest quest;
        [SaveableField(2)]
        private int daysMercenary = 0;

        public static readonly string questSettlement = "town_ES1";
        private static readonly string QuestGiverId = "south_realm_knight_maester";
        private static readonly string LordsHallLocationId = "lordshall";
        private static readonly string QuestItemId = "rfmisc_western_2hsword_t3_fire";

        private static SpawnNpcInLordsHallBecomeKnightBehavior Instance { set; get; }

        public SpawnNpcInLordsHallBecomeKnightBehavior()
        {
            Instance = this;
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnDailyTick()
        {
            quest.IncrementDaysAsMercenary();
        }
        private void OnMissionStarted(IMission imission)
        {
            LocationCharactersAreReadyToSpawn();

            if (imission is Mission mission)
            {
                mission.AddMissionBehavior(new SpawnNpcInLordsHallBecomeKnightMissionBehavior());
            }
        }

        private class SpawnNpcInLordsHallBecomeKnightMissionBehavior : MissionBehavior
        {
            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
            public override void OnMissionStateActivated()
            {
                base.OnMissionStateActivated();
            }

            public override void AfterStart()
            {
                base.AfterStart();
            }
        }
        private void LocationCharactersAreReadyToSpawn()
        {
            try
            {
                if (CampaignMission.Current != null && CampaignMission.Current.Location != null)
                {
                    Location location = CampaignMission.Current.Location;
                    Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement;
                    if (location.StringId == LordsHallLocationId && settlement != null && settlement.StringId == questSettlement)
                    {
                        if (settlement.IsTown)
                        {
                            LocationCharacter locationCharacter = CreateQuestGiver(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                            location.AddCharacter(locationCharacter);

                            InformationManager.DisplayMessage(new InformationMessage("Knight Guild Master has been spawned in the Lord's Hall."));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LocationCharactersAreReadyToSpawn: {ex.Message}"));
            }
        }
        private static LocationCharacter CreateQuestGiver(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            try
            {
                CharacterObject questGiver = MBObjectManager.Instance.GetObject<CharacterObject>(QuestGiverId);

                int minValue, maxValue;
                Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(questGiver, out minValue, out maxValue, "");
                Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(questGiver.Race, "_settlement");

                AgentData agentData = new AgentData(new SimpleAgentOrigin(questGiver, -1, null, default(UniqueTroopDescriptor)))
                                      .Monster(monsterWithSuffix)
                                      .Age(MBRandom.RandomInt(minValue, maxValue));

                LocationCharacter locationCharacter = new LocationCharacter(
                    agentData,
                    new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), "sp_lordshall_hero", true, relation, null, true, false, null, false, false, true);

                return locationCharacter;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in CreateQuestGiver: {ex.Message}"));
                return null;
            }
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            SetDialogs();
        }
        private void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(CreateStartQuestDialogFlow());
            Campaign.Current.ConversationManager.AddDialogFlow(CreateAfterQuestDialogFlow());
        }
        private DialogFlow CreateStartQuestDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Greetings, aspiring knight. Do you wish to prove yourself worthy?", null)
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "south_realm_knight_maester"
                                && Settlement.CurrentSettlement != null
                                && Settlement.CurrentSettlement.StringId == questSettlement
                                && (quest == null || !quest.IsOngoing && !quest.IsFinalized))
                .PlayerLine("What must I do to become a knight?")
                .NpcLine("You must retrieve the Knight's Insignia from a distant temple. Will you accept the challenge?")
                .BeginPlayerOptions()

                .PlayerOption("What is the Knight's Insignia?", null)
                .NpcLine("It is a sacred relic, symbolizing honor and courage.")
                .BeginPlayerOptions()
                    .PlayerOption("What do I gain from this?", null)
                    .NpcLine("You will be granted the title of knight and receive great rewards.")
                    .BeginPlayerOptions()
                        .PlayerOption("I will retrieve the Knight's Insignia.")
                        .Consequence(() => OnStartQuest())
                        .CloseDialog()
                        .PlayerOption("No, I cannot accept this task.")
                        .CloseDialog()
                    .EndPlayerOptions()
                .EndPlayerOptions()

                .PlayerOption("What do I gain from this?", null)
                .NpcLine("You will be granted the title of knight and receive great rewards.")
                .BeginPlayerOptions()
                    .PlayerOption("What is the Knight's Insignia?", null)
                    .NpcLine("It is a sacred relic, symbolizing honor and courage.")
                    .BeginPlayerOptions()
                        .PlayerOption("I will retrieve the Knight's Insignia.")
                        .Consequence(() => OnStartQuest())
                        .CloseDialog()
                        .PlayerOption("No, I cannot accept this task.")
                        .CloseDialog()
                    .EndPlayerOptions()
                .EndPlayerOptions()

                .EndPlayerOptions();
        }
        private DialogFlow CreateAfterQuestDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Congratulations on becoming a knight!")
                .Condition(() => quest.IsFinalized 
                                 && CharacterObject.OneToOneConversationCharacter?.StringId == QuestGiverId
                                && Settlement.CurrentSettlement != null
                                && Settlement.CurrentSettlement.StringId == questSettlement)
                .CloseDialog();
        }
        private void OnStartQuest()
        {
            quest = new BecomeKnightQuest("become_knight_quest", Hero.MainHero, CampaignTime.YearsFromNow(1), 500);
            quest.StartQuest();
            InformationManager.DisplayMessage(new InformationMessage("Quest started: Become a Knight."));
        }
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("quest", ref this.quest);
            if (quest != null)
            {
                quest.SyncData(dataStore);
            }
        }
    }
}







