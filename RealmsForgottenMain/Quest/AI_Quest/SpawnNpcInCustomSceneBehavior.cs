using System;
using System.Collections.Generic;
using RealmsForgotten.Quest.MissionBehaviors;

using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static HarmonyLib.Code;

namespace RealmsForgotten.Quest.AI_Quest
{
    public class BecomeKnightQuestBehavior : CampaignBehaviorBase
    {
        private static readonly string QuestGiverId = "arcane_library_maester";
        private static readonly string LordsHallLocationId = "lordshall";  // Scene ID for the lord's hall
        
        public override void RegisterEvents()
        {
            // Register events for mission started and game loaded events
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnGameLoadFinished()
        {
                  
            LocationCharactersAreReadyToSpawn();

            SetDialogs();
        }
        private void OnMissionStarted(IMission imission)
        {
            // Cast IMission to Mission to access Scene and AddMissionBehavior
            Mission mission = imission as Mission;

            if (mission != null && mission.Scene?.GetName() == LordsHallLocationId)
            {
                // Add the custom mission behavior for the lord's hall scene
                mission.AddMissionBehavior(new SpawnKnightInLordsHallMissionBehavior());
            }
            
            LocationCharactersAreReadyToSpawn();  
        }

       
        private void LocationCharactersAreReadyToSpawn()
        {
            try
            {
                if (CampaignMission.Current != null && CampaignMission.Current.Location != null)
                {
                    Location location = CampaignMission.Current.Location;

                    // Check if the player is in the Lord's Hall
                    if (location.StringId == LordsHallLocationId)
                    {
                        Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement;
                        if (settlement != null && settlement.IsTown)
                        {
                            // Always create a new quest giver
                            LocationCharacter locationCharacter = CreateQuestGiver(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                            location.AddCharacter(locationCharacter);

                            InformationManager.DisplayMessage(new InformationMessage("Quest Giver has been spawned in the Lord's Hall."));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in LocationCharactersAreReadyToSpawn: {ex.Message}"));
            }
        }

        // Create the quest giver's LocationCharacter in the scene
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

                // List of possible spawn points in the lord's hall
                List<string> possibleLordHallSpawnPoints = new List<string>
                {
                    "sp_lordshall_hero",         // Main hall
                    "sp_lordshall_throne_left",   // Near the left side of the throne
                    "sp_lordshall_throne_right",  // Near the right side of the throne
                };

                string randomSpawnPoint = possibleLordHallSpawnPoints[MBRandom.RandomInt(possibleLordHallSpawnPoints.Count)];

                LocationCharacter locationCharacter = new LocationCharacter(
                    agentData,
                    new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors),
                    randomSpawnPoint,  // Use the randomized lord's hall spawn point
                    true,
                    relation,
                    null,
                    true,
                    false,
                    null,
                    false,
                    false,
                    true);

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
            Campaign.Current.ConversationManager.AddDialogFlow(CreateBecomeKnightQuestDialogFlow());
        }
        private DialogFlow CreateBecomeKnightQuestDialogFlow()
        {
            return DialogFlow.CreateDialogFlow("lord_start", 125)
                .NpcLine("Greetings, traveler. I have a task of great importance. Will you assist me in retrieving the Sacred Weapon?", null)
                .Condition(() => IsPlayerEligibleForQuest())  // Condition to offer the quest
                .BeginPlayerOptions()
                .PlayerOption("Yes, I will take on this task.")
                .Consequence(() => StartBecomeKnightQuest())  // Accept quest and start it
                .CloseDialog()
                .PlayerOption("No, I must decline your offer.")
                .CloseDialog()
                .EndPlayerOptions();
        }


        // Check if the player is eligible to start the quest
        private bool IsPlayerEligibleForQuest()
        {
            return BecomeKnightQuest.Instance == null;
        }

        // Start the quest
        private void StartBecomeKnightQuest()
        {
            Hero questGiver = Hero.FindFirst(hero => hero.StringId == QuestGiverId);

            if (questGiver != null)
            {
                CampaignTime duration = CampaignTime.DaysFromNow(30);  // Example duration
                int rewardGold = 1000;  // Example reward

                BecomeKnightQuest quest = new BecomeKnightQuest("become_knight_quest", questGiver, duration, rewardGold);
                quest.StartQuest();
            }
        }

        // MissionBehavior to trigger NPC conversation in the lord's hall
        private class SpawnKnightInLordsHallMissionBehavior : MissionBehavior
        {
            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

            public override void OnRenderingStarted()
            {
                base.OnRenderingStarted();
                if (CampaignMission.Current?.Location != null && CampaignMission.Current.Location.StringId == LordsHallLocationId)
                {
                    // Check if the quest is ongoing and if the player can interact with the NPC
                    if (BecomeKnightQuest.Instance != null && Hero.MainHero != null)
                    {
                        // Trigger the conversation with the quest giver if conditions are met
                        Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(
                            Mission.Current.Agents.Find(a => a.Character.StringId.Contains(QuestGiverId)), true);
                    }
                }
            }
        }
    }
}
internal class BecomeKnightQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog talkToKnightLog;
        [SaveableField(1)]
        private JournalLog takeSacredWeaponLog;
        [SaveableField(2)]
        private JournalLog deliverToKnightLog;
        [SaveableField(3)]
        private JournalLog _startQuestLog;

    private const string SacredWeaponId = "ancient_elvish_polearm";  // Equivalent to shield
        private const string WeaponFightCharacter = "allkhuur_goddess";   // Entity to fight for the weapon
        private Hero AnoriteMaester = Hero.FindFirst(hero => hero.Name.ToString() == "Anorite Maester");
        public override TextObject Title => GameTexts.FindText("rf_quest_knight_part_one");
        public override bool IsRemainingTimeHidden => true;
        public override bool IsSpecialQuest => true;
        public static BecomeKnightQuest Instance { get; private set; }

        private Agent _weaponFightWinner;

        public BecomeKnightQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold)
        {
            Instance = this;
        }

       
    protected override void InitializeQuestOnGameLoad()
        {
            // Restore state when loading the game
            SetDialogs();
            Instance = this;
        }

    protected override void OnStartQuest()
    {
        RegisterEvents();
        SetDialogs();
        Instance = this;

        // Adding discrete log for the quest start
        _startQuestLog = AddDiscreteLog(
            new TextObject("The quest to become a knight has started. Retrieve the sacred weapon."),
            new TextObject("Knight Quest"),  // Title of the log entry
            0,  // Initial progress
            1  // Maximum progress
        );
    }

    protected override void HourlyTick() { }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(talkToKnightDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(takeSacredWeaponDialog, this);
        }

    private DialogFlow talkToKnightDialog => DialogFlow.CreateDialogFlow("start", 125)
 .NpcLine(new TextObject("Greetings, traveler. I have been waiting for someone like you. Will you help us retrieve the sacred weapon?"))
 .Condition(() => talkToKnightLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == AnoriteMaester)
 .PlayerLine(new TextObject("I am ready to assist. Tell me what needs to be done."))
 .NpcLine(new TextObject("The weapon is hidden in a faraway place, guarded by powerful forces. Be careful."))
 .PlayerLine(new TextObject("I will retrieve the weapon and return victorious."))
 .NpcLine(new TextObject("May the gods be with you. Bring the weapon back and you shall be greatly rewarded."))
 .PlayerLine(new TextObject("I won't let you down. Farewell for now."))
 .Consequence(() => { /* Add consequences if necessary */ })
 .CloseDialog();

    private DialogFlow takeSacredWeaponDialog => DialogFlow.CreateDialogFlow("start", 125)
     .NpcLine(new TextObject("Have you retrieved the sacred weapon?"))
     .Condition(() => talkToKnightLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == AnoriteMaester)
     .PlayerLine(new TextObject("Yes, I have the weapon. It wasn't easy, but I succeeded."))
     .NpcLine(new TextObject("Well done, warrior. You have proven yourself."))
     .PlayerLine(new TextObject("I hope this weapon serves your people well."))
     .NpcLine(new TextObject("It will. Your efforts will not go unnoticed. Here is your reward."))
     .PlayerLine(new TextObject("Thank you. I am honored."))
     .Consequence(() => { /* Add consequences like updating quest log or giving rewards */ })
     .CloseDialog();

    private void OnMissionStart(Mission mission)
        {
            // Ensure mission scene and quest stage are correct
            if (mission.Scene?.GetName() == "arcane_keep_interior" && takeSacredWeaponLog.CurrentProgress == 0)
            {
                // Add behavior to handle conversations within the mission
                mission.AddMissionBehavior(new MissionConversationLogic());

                // Add custom logic specific to this quest (handling relic spawning and weapon pickup)
                mission.AddMissionBehavior(new KnightQuestRelicsLogic());
            }
        }

        // Method to start weapon fight, like the shield fight in FifthQuest
        private void StartWeaponFight()
        {
            if (takeSacredWeaponLog.CurrentProgress == 0)
            {
                // Retrieve enemy agent and start the fight (explicit cast to Agent)
                Agent enemyAgent = (Agent)MissionConversationLogic.Current.ConversationManager.ConversationAgents[0];

                // Start the custom fight
                Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(
                    new List<Agent> { Agent.Main },
                    new List<Agent> { enemyAgent },
                    false,
                    false,
                    delegate (bool isPlayerSideWon)
                    {
                        if (_weaponFightWinner == Agent.Main)
                        {
                            OnWeaponFightWin();
                        }
                    });
            }
        }

        
        private void OnWeaponFightWin()
        {
            takeSacredWeaponLog.UpdateCurrentProgress(1);  // Update quest log
            PartyBase.MainParty.ItemRoster.AddToCounts(
                MBObjectManager.Instance.GetObject<ItemObject>(SacredWeaponId), 1);  // Add the weapon to inventory
        }

      
        private void OnDeliverWeaponToKnight()
        {
            deliverToKnightLog.UpdateCurrentProgress(1);  // Update final log
            ShowQuestCompletionDialog();  // Show completion dialog and reward
        }

        private class KnightQuestRelicsLogic : MissionLogic
        {
            public Agent TreasureFightAgent;
            public static KnightQuestRelicsLogic Instance { get; private set; }

            public KnightQuestRelicsLogic()
            {
                Instance = this;
            }

            public override void AfterStart()
            {
                base.AfterStart();
                if (BecomeKnightQuest.Instance.takeSacredWeaponLog.CurrentProgress == 0)
                {
                    // Spawn the sacred weapon in the scene
                    Vec3 position = new Vec3(138.58f, 161.32f, 24.23f);
                    Vec3 rotation = new Vec3(-90.00f, 0.00f, -178.46f);

                    MissionWeapon missionWeapon = new MissionWeapon(
                        MBObjectManager.Instance.GetObject<ItemObject>(SacredWeaponId),
                        new ItemModifier(),
                        Banner.CreateOneColoredEmptyBanner(1));

                    Mission.SpawnWeaponWithNewEntityAux(
                        missionWeapon,
                        Mission.WeaponSpawnFlags.WithStaticPhysics,
                        new MatrixFrame(Mat3.CreateMat3WithForward(rotation), position),
                        0,
                        null,
                        false);
                }
                Mission.Current.OnItemPickUp += OnItemPickup;
            }

            // Handle weapon pickup
            private void OnItemPickup(Agent agent, SpawnedItemEntity spawnedItemEntity)
            {
                if (agent.IsMainAgent && spawnedItemEntity.WeaponCopy.Item?.StringId == SacredWeaponId)
                {
                    // Trigger fight with goddess character after weapon is picked up
                    CharacterObject characterObject = CharacterObject.Find(WeaponFightCharacter);
                    Equipment randomEquipment = Equipment.GetRandomEquipmentElements(characterObject, true);
                    Vec3 initialPosition = new Vec3(138.69f, 157.79f, 23.82f);

                    AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(characterObject))
                        .Equipment(randomEquipment)
                        .InitialPosition(initialPosition);

                    TreasureFightAgent = Mission.Current.SpawnAgent(agentBuildData, true);
                    Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(TreasureFightAgent, false);
                }
            }
        }

        // Final dialog for quest completion
        private void ShowQuestCompletionDialog()
        {
            DialogFlow.CreateDialogFlow("end", 125)
                .NpcLine(GameTexts.FindText("rf_quest_knight_complete_1"))
                .PlayerLine(GameTexts.FindText("rf_quest_knight_complete_2"))
                .Consequence(() => CompleteQuestWithSuccess())
                .CloseDialog();
        }
}





