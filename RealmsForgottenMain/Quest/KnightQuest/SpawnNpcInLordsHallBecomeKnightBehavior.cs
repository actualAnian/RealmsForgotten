using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using RealmsForgotten.Quest.KnightQuest;

namespace RealmsForgotten.Quest
{

    public class SpawnNpcInLordsHallBecomeKnightBehavior : CampaignBehaviorBase
    {
        [SaveableField(1)]
        private BecomeKnightQuest? quest;
        [SaveableField(2)]
        private int daysMercenary = 0;

        public static readonly string questSettlement = "town_EN1";
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
            quest?.IncrementDaysAsMercenary();
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

                            InformationManager.DisplayMessage(new InformationMessage("Knight Guild Master is currently in the Lord's Chambers."));
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
                .NpcLine("Greetings sir, I suppose you are another aspiring knight... Do you wish to prove yourself worthy?", null)
                .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "south_realm_knight_maester"
                                && Settlement.CurrentSettlement != null
                                && Settlement.CurrentSettlement.StringId == questSettlement
                                && (quest == null || !quest.IsOngoing && !quest.IsFinalized))
                .PlayerLine("What must I do to become a knight?")
                .NpcLine("You must successfully endure tasks of bravery, honor, loyalty, and mastery in combat to prove your valor. Do you find yourself ready to accept such challenges?")
                .BeginPlayerOptions()

                .PlayerOption("How do I start?", null)
                .NpcLine("First, you have to go through the trial of bravery. You have to retrieve the sword of justice, a sacred relic, symbolizing honor and courage.")
                .BeginPlayerOptions()
                    .PlayerOption("Where do I find it?", null)
                    .NpcLine("You will have to go to the monastery of the Anorites.")
                    .BeginPlayerOptions()
                        .PlayerOption("I will retrieve the sword.")
                        .Consequence(() => OnStartQuest())
                        .CloseDialog()
                        .PlayerOption("No, I guess I am not up to this task right now.")
                        .CloseDialog()
                    .EndPlayerOptions()
                .EndPlayerOptions()

                .EndPlayerOptions();
        }
        private DialogFlow CreateAfterQuestDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine("Congratulations on becoming a knight!")
                .Condition(() => quest != null
                                 && quest.IsFinalized
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
            quest?.SyncData(dataStore);
        }
    }
}






