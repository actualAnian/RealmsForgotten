using RealmsForgotten.Quest.MissionBehaviors;
using RealmsForgotten.Quest.SecondUpdate;
using RealmsForgotten.Quest.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;
using static TaleWorlds.Core.ViewModelCollection.CharacterViewModel;



namespace RealmsForgotten.Quest.FourthUpdate
{
    public class SeventhQuest : QuestBase
    {
        [SaveableField(0)]
        protected JournalLog? druidInteractionLog;

        [SaveableField(1)]
        private JournalLog? travelObjectiveLog;

        [SaveableField(2)]
        private bool hasBeenIntercepted;

        [SaveableField(3)]
        private JournalLog? interceptorDefeatLog;

        [SaveableField(4)]
        private JournalLog? bossBattleLog;

        [SaveableField(5)]
        private MobileParty? _interceptorParty; // Reference to our spawned interceptor party

        [SaveableField(6)]
        private JournalLog? dwarfKingLog;

        // This flag ensures the two-step popup sequence only shows after one in-game day passes.
        [SaveableField(7)]
        private bool _shouldShowPopups;

        // Once we open the druid conversation on the campaign map, we set this true so it doesn't repeat.
        [SaveableField(8)]
        private bool _druidConversationTriggered;

        [SaveableField(10)]
        private bool _playerChoseToDestroyElveans;

        [SaveableField(11)]
        private JournalLog? elveanDestructionObjectiveLog;

        [SaveableField(12)]
        private bool _shouldTriggerOwlDialogue = false;

        [SaveableField(13)]
        private JournalLog? owlRebellionlLog;

        [SaveableField(14)]
        private JournalLog? priestessArmyLog;

        [SaveableField(15)]
        private MobileParty? _witchFinalArmy;

        [SaveableField(16)]
        private JournalLog? vortiakLairLog;

        [SaveableField(17)]
        private bool deformedSpawningEnabled = false;

        [SaveableField(18)]
        private CampaignTime _nextDeformedSpawnTime = CampaignTime.Zero;

        [SaveableField(19)]
        private int _deformedPartySpawnCount = 0;

        [SaveableField(20)]
        private bool _eighthQuestStarted = false;

        [SaveableField(21)]
        private bool _witchConversationCompleted = false;

        [SaveableField(22)] 
        private bool _shouldOpenEighthQuestDialog = false;

        [SaveableField(23)]
        private JournalLog eighthPriestessLog;

        [SaveableField(24)] // Use the next available number
        private bool _priestessConversationTriggered = false;

        private const int travelObjectiveTarget = 1;
        private bool IsTravelObjectiveCompleted => travelObjectiveLog?.CurrentProgress >= travelObjectiveTarget;

        public static bool IsVortiakClanSpawned { get; set; } = false;

        private Settlement witchHideout => Settlement.Find("vortiak_ruined_temple");

        private static readonly string witchCharacterId = "evil_witch";

        private Vec2 _lastPlayerPosition = Vec2.Invalid;

        private bool _dialogEventsRegistered = false;
        public int DruidInteractionStage => druidInteractionLog?.CurrentProgress ?? -1;

        private Hero? DeformedLeaderHero = null;

           

        // Interceptor army configuration
        private static readonly Dictionary<string, List<TroopDetail>> InterceptorArmies =
            new Dictionary<string, List<TroopDetail>>
            {
                ["rf_interceptor_default"] = new List<TroopDetail>
                {
                    new TroopDetail("vortiak_mounted_necromancer_lord", 1),
                    new TroopDetail("vortiak_warrior", 1),
                    new TroopDetail("vortiak_crossbowman", 1),
                    new TroopDetail("vortiak_swordsman", 2),
                    new TroopDetail("cs_nelrog_bandits_chief", 1),
                    new TroopDetail("vortiak_crossbowman", 1),
                    new TroopDetail("hellbound_boss", 1),
                    new TroopDetail("hellbound_chief", 2)
                }
            };

        private static readonly Dictionary<string, List<TroopDetail>> VortiakArmies =
    new Dictionary<string, List<TroopDetail>>
    {
        ["rf_vortiak_army"] = new List<TroopDetail>
    {
        new TroopDetail("sturgian_warrior", 200),
        new TroopDetail("sturgian_woodsman", 250),
        new TroopDetail("sturgian_veteran_warrior", 300),
        new TroopDetail("druzhinnik", 150),
        new TroopDetail("druzhinnik_champion", 50),
        new TroopDetail("sturgian_brigand", 100)
    }
    };
        private static readonly Dictionary<string, List<TroopDetail>> WitchArmies =
    new Dictionary<string, List<TroopDetail>>
    {
        ["rf_witch_final"] = new List<TroopDetail>
        {
            new TroopDetail("vortiak_warrior",           250),
            new TroopDetail("vortiak_crossbowman",       300),
            new TroopDetail("cs_daimo_raiders_raider",    50),
            new TroopDetail("cs_bark_raiders_raider",     50),
            new TroopDetail("cs_nurh_raiders_raider",     50),
            new TroopDetail("cs_nurh_raiders_bandit",     50),
            new TroopDetail("cs_sillok_raiders_raider",   50),
            new TroopDetail("cs_devils_bandits_chief",    50),
            new TroopDetail("evil_witch",                  1)
        }
  
    };
        private static readonly Dictionary<string, List<TroopDetail>> DeformedArmies =
    new Dictionary<string, List<TroopDetail>>
    {
        ["rf_deformed_army"] = new List<TroopDetail>
        {
            new TroopDetail("orc_base_infantry", 80)
          
        }
    };

        public static Hero TheOwl
        {
            get
            {
                if (Hero.OneToOneConversationHero?.StringId == "rf_the_owl")
                    return Hero.OneToOneConversationHero; // Return the hero if we are already talking to him

                return Hero.FindFirst(x => x.StringId == "rf_the_owl");
            }
        }
        private static Settlement FirstTreeSettlement
            => Settlement.FindFirst(s => s.StringId == "town_FirstTree");

        public SeventhQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
     : base(questId, questGiver, duration, rewardGold)
        {

        }
        public override TextObject Title => GameTexts.FindText("rf_seventh_quest_title");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => true;

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            // Add a daily tick listener.
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

            // We'll rely on HourlyTick to check distance & possibly open the conversation
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnLeaveSettlement);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            RegisterEvents();
            _shouldShowPopups = true;  // Wait one day, then show the “letter from the Elven King”
            _druidConversationTriggered = false; // We haven't forced the conversation yet
        }

        protected override void HourlyTick()
        {
            // 🔹 1. FIRST PRIESTESS interaction (original quest flow)
            if (druidInteractionLog != null && druidInteractionLog.CurrentProgress == 0)
            {
                if (!_druidConversationTriggered && FirstTreeSettlement != null)
                {
                    float distance = MobileParty.MainParty.Position2D.Distance(FirstTreeSettlement.GatePosition);
                    if (distance <= 50f)
                    {
                        _druidConversationTriggered = true;

                        CharacterObject druidCharacter = CharacterObject.Find("elvean_first_tree_druid_quest");
                        if (druidCharacter != null)
                        {
                            CampaignMapConversation.OpenConversation(
                                new ConversationCharacterData(CharacterObject.PlayerCharacter),
                                new ConversationCharacterData(druidCharacter)
                            );
                        }
                        else
                        {
                            InformationManager.DisplayMessage(new InformationMessage("Druid character not found!"));
                        }
                    }
                }
            }

            // 🔹 2. SECOND PRIESTESS interaction (for Eighth Quest launch)
            if (eighthPriestessLog != null && eighthPriestessLog.CurrentProgress == 0 && !_priestessConversationTriggered)
            {
                if (FirstTreeSettlement != null)
                {
                    float distance = MobileParty.MainParty.Position2D.Distance(FirstTreeSettlement.GatePosition);
                    if (distance <= 50f)
                    {
                        // Force second conversation with the SAME Priestess NPC but new dialogue
                        CharacterObject priestessCharacter = CharacterObject.Find("elvean_first_tree_druid_quest_2");
                        if (priestessCharacter != null)
                        {
                            _priestessConversationTriggered = true;
                            CampaignMapConversation.OpenConversation(
                                new ConversationCharacterData(CharacterObject.PlayerCharacter),
                                new ConversationCharacterData(priestessCharacter)
                            );
                        }
                        else
                        {
                            InformationManager.DisplayMessage(new InformationMessage("Priestess character not found!"));
                        }
                    }
                }
            }

            // 🔹 3. OWL trigger (your original)
            if (_shouldTriggerOwlDialogue)
            {
                if (_lastPlayerPosition.IsValid)
                {
                    float movedDistance = MobileParty.MainParty.Position2D.Distance(_lastPlayerPosition);
                    if (movedDistance > 20f)
                    {
                        _shouldTriggerOwlDialogue = false;
                        return;
                    }
                }
                _lastPlayerPosition = MobileParty.MainParty.Position2D;
            }
        }


        private void OnDailyTick()
        {
            if (_shouldShowPopups)
            {
                _shouldShowPopups = false;
                ShowSeventhQuestNotification();
            }

            if (deformedSpawningEnabled && CampaignTime.Now > _nextDeformedSpawnTime)
            {
                SpawnDeformedParties();
                _nextDeformedSpawnTime = CampaignTime.DaysFromNow(5);
            }

            // 🔥 Show the Eighth Quest inquiry once when spawn count hits 3
            if (_deformedPartySpawnCount >= 3 && !_eighthQuestStarted && !_shouldOpenEighthQuestDialog)
            {
                ShowEighthQuestPrompt(); // the inquiry
            }

            CheckTempleProximity();
            CheckElveanDestructionProgress();
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return;

            if (!mapEvent.IsPlayerMapEvent)
                return;

            if (MobileParty.MainParty.PrisonRoster != null)
            {
                RemoveEvilWitchFromPrisoners();
            }
        }


        // Show the "letter from the Elven King" after 1 day
        private void ShowSeventhQuestNotification()
        {
            string title = "A Mysterious Presence";
            string description =
                "A massive eagle flies overhead, dropping a parchment bearing a strange seal. " +
                "Inside it, you see an urgent request to visit the First Tree Temple";

            InformationManager.ShowInquiry(
                new InquiryData(
                    title,
                    description,
                    true,
                    false,
                    "Continue",
                    null,
                    OnPopupClosed, // Callback after user clicks "Continue"
                    null
                )
            );
        }

        // Once the user closes the pop-up, we create the "talk to druid" objective
        private void OnPopupClosed()
        {
            druidInteractionLog = AddLog(GameTexts.FindText("rf_seventh_quest_druid_objective"));
            druidInteractionLog.UpdateCurrentProgress(0);

        }

        private void OnMissionStarted(IMission imission)
        {
            if (imission is Mission mission)
            {
                if (Settlement.CurrentSettlement == witchHideout && mission.SceneName == "evil_witch_fight" && bossBattleLog?.CurrentProgress == 0)
                {
                    mission.AddMissionBehavior(new RecordDamageMissionLogic((victim, attacker, damage) =>
                    {
                        if (victim.Character?.StringId == "evil_witch" && damage >= victim.Health)
                            bossBattleLog.UpdateCurrentProgress(1);

                    }));
                }
            }
        }
        private void OnLeaveSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (bossBattleLog?.CurrentProgress == 1 && settlement == witchHideout)
            {
                PlayerEncounter.Finish();
                bossBattleLog.UpdateCurrentProgress(2);
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(CharacterObject.Find(witchCharacterId)));

            }
        }

      
        // ------------------------------------------------------
        // DRUID DIALOG - no Settlement check, since we open it from the campaign map
        // ------------------------------------------------------
        private DialogFlow DruidInteractionDialog => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_1"))
            .Condition(() =>
                // Must have the log assigned and must be at stage 0
                druidInteractionLog != null
                && druidInteractionLog.CurrentProgress == 0
                // Must be talking to the correct druid
                && CharacterObject.OneToOneConversationCharacter?.StringId == "elvean_first_tree_druid_quest"
            )
            .NpcLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_6"))
            .Consequence(() =>
            {
                // Mark the druid conversation as complete
                druidInteractionLog.UpdateCurrentProgress(1);

                // The druid also gives you some troops (example)
                GivePlayerTroops(new List<(string troopId, int troopCount)>
                {
                    ("first_tree_ranger", 100)
                });

                // Next objective: talk to the dwarf king
                dwarfKingLog = AddLog(GameTexts.FindText("rf_seventh_quest_dwarf_king_objective"));
                dwarfKingLog.UpdateCurrentProgress(0);

            })
           .CloseDialog();

        // ------------------------------------------------------
        // DWARF KING DIALOG (unchanged)
        // ------------------------------------------------------
        private DialogFlow DwarfKingDialog => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(new TextObject(
                "So you've come seeking our aid? We, the Dugrast folk, have long prepared for these dark times."
            ))
            .Condition(() =>
                dwarfKingLog != null &&
                dwarfKingLog.CurrentProgress == 0 &&
                Hero.OneToOneConversationHero?.StringId == "lord_dwarf_faction_1"
            )
            .PlayerLine(new TextObject(
                "I have been told your people hold the oldest memories. My path has led me into dark times indeed. I came here after counseling with teh Pristess of the First Tree, from the High Kingdom of the Elveans."
            ))
            .NpcLine(new TextObject(
                "Very well. She has been at the side of life and have helped my people in the past. We own her loyalty."
            ))
            .PlayerLine(new TextObject(
                "What can you tell us about the Vortiaks? ?"
            ))
            .NpcLine(new TextObject(
                "They were once a noble people, and rulers of the greatest empire in Aeurth, the Vortiak Empire.They ended up in war with the elvean ancestors, the elvish kin that arrived from beyond the sea of mists. They settled themselves on a territory sacred to the Vortiaks and asked for ownership of it. Of course the Vortiaks did not accepted, and demanded their leave.  The rest was war, a conflict that last for one hundred years. The Vortiaks have been defeated and those who survived came into our doors asking for shelter."
            ))
            .PlayerLine(new TextObject(
                "What a history... So they indeed survived the war."
            ))
            .NpcLine(new TextObject(
                "Some of them yes, enough to keep their bloodline alive. For years we shared our bread and roof we learned much from each other. But they turned to the deep forces of the mountain, and against our advice led themselves fall for it. There was no choice them to expell them, to not bring harm to our kin. And to the wild lands of winter they went, to find a new lair among the cold mountains of the east."
            ))
            .PlayerLine(new TextObject(
                "And where would that be?"
            ))
              .NpcLine(new TextObject(
                "You have to travel beyond the territory of the Urkhai, into the east. At the north of the Dradrealms you will find the Black Mountains, a territory no living being dare to step in. There laid much of the Vortiak sacred sites in the ancient times."
            ))
            .PlayerLine(new TextObject(
                "Thank you my king, that is of a great help. You honour us with your knowledge."
            ))
              .NpcLine(new TextObject(
                "I will not let any of our people to get involved on that. We lived for ages under the earth and we can always go back if needed. But i will grant you with equipment forged from our better smiths. May this help you on your cause."
            ))
            .PlayerLine(new TextObject(
                "Thats a great gift milord, thank you very much."
            ))
            .Consequence(() =>
            {
                dwarfKingLog.UpdateCurrentProgress(1);

                // Example: dwarven armor set
                GivePlayerArmorSet(new List<string>
                {
                    "sk_dwarf_erebor_helmet_plate_elite_a",
                    "sk_dwarf_erebor_chest_plate_elite_a",
                    "sk_dwarf_erebor_bracers_elite_a",
                    "sk_dwarf_erebor_boots_med_b",
                    "sk_dwarf_erebor_pauldron_plate_elite_a"
                });


                // Add the travel objective log: "Go to the Ruined Temple"
                travelObjectiveLog = AddDiscreteLog(
                    GameTexts.FindText("rf_seventh_quest_travel_log"),
                    GameTexts.FindText("rf_seventh_quest_travel_task"),
                    0, travelObjectiveTarget
                );

                QuestUIManager.ShowNotification(
                    "You have received dwarven armor and learned the temple’s location. Head to the Vortiak Lair!",
                    null,
                    true,
                    "huntthewitch"
                );
            })
            .CloseDialog();

        private void StartFindVortiakLairObjective()
        {
            vortiakLairLog = AddDiscreteLog(
                GameTexts.FindText("rf_seventh_quest_lair_log"),
                GameTexts.FindText("rf_seventh_quest_lair_task"),
                0, 1
            );

        }

        // ------------------------------------------------------
        // Checks distance to Ruined Temple, spawns interceptor, etc.
        // ------------------------------------------------------
        private void CheckTempleProximity()
        {
            // Only proceed if the dwarf king conversation is complete
            if (dwarfKingLog == null || dwarfKingLog.CurrentProgress < 1)
                return;

            var sacredGrove = Settlement.FindFirst(s => s.StringId == "vortiak_ruined_temple");
            if (sacredGrove != null)
            {
                float distance = MobileParty.MainParty.Position2D.Distance(sacredGrove.Position2D);
                float proximityThreshold = 50f; // Adjust as needed.
                if (distance <= proximityThreshold)
                {
                    // Mark traveling objective complete
                    if (travelObjectiveLog != null && travelObjectiveLog.CurrentProgress < travelObjectiveTarget)
                    {
                        travelObjectiveLog.UpdateCurrentProgress(travelObjectiveTarget);
                        InformationManager.DisplayMessage(new InformationMessage("You are nearing the temple."));
                    }
                    // Spawn an interceptor party if not yet done
                    if (!hasBeenIntercepted)
                    {
                        CreateInterceptorParty("rf_interceptor_default", sacredGrove);
                        hasBeenIntercepted = true;
                    }


                }
            }
        }

        // ------------------------------------------------------
        // INTERCEPTOR, NECROMANCER, BOSS LOGIC
        // ------------------------------------------------------
        private void CreateInterceptorParty(string key, Settlement nearSettlement)
        {
            interceptorDefeatLog?.UpdateCurrentProgress(0);
            try
            {
                if (!InterceptorArmies.TryGetValue(key, out var troopDetails))
                    throw new Exception("Interceptor configuration not found.");

                var enemyClan = Clan.All.FirstOrDefault(c => c.StringId == "vortiaks")
                                    ?? Clan.All.First();
                var interceptorParty = BanditPartyComponent.CreateBanditParty(enemyClan.StringId, enemyClan, null, true);
                if (interceptorParty == null)
                    throw new Exception("Failed to create interceptor party.");

                interceptorParty.SetCustomName(new TextObject("Vortiak Army"));

                _interceptorParty = interceptorParty;
                var troopRoster = TroopRoster.CreateDummyTroopRoster();

                foreach (var detail in troopDetails)
                {
                    var troop = CharacterObject.Find(detail.TroopId);
                    if (troop == null)
                    {
                        InformationManager.DisplayMessage(
                            new InformationMessage($"Troop with ID {detail.TroopId} not found.")
                        );
                        continue;
                    }
                    troopRoster.AddToCounts(troop, detail.Quantity);
                }

                // Now spawn around the settlement instead of the player's party
                interceptorParty.InitializeMobilePartyAroundPosition(
                    troopRoster,
                    TroopRoster.CreateDummyTroopRoster(),
                    nearSettlement.Position2D, // use settlement coords
                    10f,
                    10f
                );
                interceptorParty.Aggressiveness = 15f;
                interceptorParty.Ai.SetMoveEngageParty(MobileParty.MainParty);

                InformationManager.DisplayMessage(
                    new InformationMessage("An enemy interceptor army has ambushed you near the temple!"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"Exception while creating interceptor party: {ex.Message}")
                );
            }
        }

        private void AddVortiakTroopsToPlayer()
        {
            if (!VortiakArmies.TryGetValue("rf_vortiak_army", out var vortiakTroops))
            {
                InformationManager.DisplayMessage(new InformationMessage("Vortiak army configuration not found.", Colors.Red));
                return;
            }

            foreach (var detail in vortiakTroops)
            {
                var troop = CharacterObject.Find(detail.TroopId);
                if (troop == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Troop with ID {detail.TroopId} not found.", Colors.Red));
                    continue;
                }

                // The 'false' parameter ensures these troops do not count towards the party's size limit.
                MobileParty.MainParty.MemberRoster.AddToCounts(troop, detail.Quantity, false);
                InformationManager.DisplayMessage(new InformationMessage($"Added {detail.Quantity} {troop.Name} to your party (non-counted)."));
            }
        }

        private void SpawnVortiakClanParty()
        {
            Settlement originSettlement = Settlement.Find("town_S1") ?? Settlement.FindFirst(s => s.IsTown);
            if (originSettlement == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Origin settlement not found.", Colors.Red));
                return;
            }

            CultureObject sturgiaCulture = MBObjectManager.Instance.GetObject<CultureObject>("sturgia");
            if (sturgiaCulture == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Sturgia culture not found.", Colors.Red));
                return;
            }

            Kingdom sturgiaKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "sturgia");

            Hero vortiakHero = HeroCreator.CreateSpecialHero(
                CharacterObject.PlayerCharacter, // Optional: Should probably create a better template later
                originSettlement,
                null,
                null
            );
            vortiakHero.Culture = sturgiaCulture;
            vortiakHero.ChangeState(Hero.CharacterStates.Active);
            vortiakHero.SetName(new TextObject("Vortiak Lord"), new TextObject("Vortiak Lord"));

            // Correct: create clan
            Clan vortiakClan = Clan.CreateClan(
                "vortiak",
                originSettlement,
                vortiakHero,
                sturgiaKingdom,
                sturgiaCulture,
                new TextObject("Vortiak Clan"),
                4,
                0
            );

            // ✅ Correct: assign hero to clan
            vortiakHero.Clan = vortiakClan;
            vortiakClan.Heroes.Add(vortiakHero);

            // Create party correctly
            MobileParty vortiakParty = LordPartyComponent.CreateLordParty(
                "vortiak_army",
                vortiakHero,
                MobileParty.MainParty.Position2D + new Vec2(2f, 2f),
                10f,
                originSettlement,
                vortiakHero
            );

            // ✅ Fill troops
            if (VortiakArmies.TryGetValue("rf_vortiak_army", out var vortiakTroops))
            {
                foreach (var detail in vortiakTroops)
                {
                    CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>(detail.TroopId);
                    if (troop != null)
                    {
                        vortiakParty.MemberRoster.AddToCounts(troop, detail.Quantity);
                    }
                }
            }

            // ✅ Escort player directly
            vortiakParty.Ai.SetMoveEscortParty(MobileParty.MainParty);
            vortiakParty.Ai.SetDoNotMakeNewDecisions(true);
            vortiakParty.Ai.SetInitiative(1.0f, 0.5f, 0.1f);
            vortiakParty.IsDisbanding = false;

            // ✅ Food and morale
            AddFoodToParty(vortiakParty);
            vortiakParty.RecentEventsMorale = 40f;
            vortiakParty.PartyTradeGold = 5000;

            InformationManager.DisplayMessage(new InformationMessage("✅ Vortiak Party spawned successfully and is escorting you!"));
        }


        void AddFoodToParty(MobileParty party)
        {
            string[] foodItemIds = { "grain", "meat", "butter", "cheese" };
            foreach (string itemId in foodItemIds)
            {
                ItemObject food = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                if (food != null)
                    party.ItemRoster.AddToCounts(food, 10); // 10 units of each
            }
        }
        private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyer)
        {
            if (_interceptorParty != null && party == _interceptorParty)
            {
                interceptorDefeatLog = AddLog(GameTexts.FindText("rf_seventh_quest_interceptor_defeated_log"));
                interceptorDefeatLog.UpdateCurrentProgress(2);
                InformationManager.DisplayMessage(new InformationMessage("The interceptor army has been defeated."));
                if (owlRebellionlLog == null)
                {
                    owlRebellionlLog = AddLog(GameTexts.FindText("rf_seventh_quest_owl_log"));
                }
                owlRebellionlLog.UpdateCurrentProgress(0);

                if (destroyer != null && destroyer.LeaderHero == Hero.MainHero)
                {
                    if (Clan.PlayerClan != null)
                    {
                        Clan.PlayerClan.Renown += 75;
                        InformationManager.DisplayMessage(new InformationMessage("Your deeds are becoming known across the land. (+75 Renown)", Colors.Green));
                    }
                }
                OpenOwlRebelionDialogue();
            }

            if (_witchFinalArmy != null && party == _witchFinalArmy)
            {
                // Always show the Witch defeated notification, no matter the path!
                ShowWitchDefeatedNotification();

                if (priestessArmyLog != null && priestessArmyLog.CurrentProgress == 0)
                {
                    priestessArmyLog.UpdateCurrentProgress(1);
                }
            }
        }

        private DialogFlow InterceptorEncounterDialogue => DialogFlow.CreateDialogFlow("interceptor_encounter", 120)
    .NpcLine("Steel your hearts! The Vortiaks spring their ambush!")
    .Condition(() => PlayerEncounter.EncounteredParty == _interceptorParty.Party)
    .BeginPlayerOptions()
        .PlayerOption("Stand and fight!")
            .NpcLine("So be it—blood will flow!")
             .Consequence(() =>
             {
                 // Optional: Set quest flags or start battle
                 // e.g., PlayerEncounter.StartBattle();
             })
    .EndPlayerOptions()
    .CloseDialog();

       private void StartNecromancerDialogue()
        {

            CharacterObject necroChar = CharacterObject.Find("vortiak_mounted_necromancer_lord");
            if (necroChar != null)
            {
                // Register the necromancer flow
                Campaign.Current.ConversationManager.AddDialogFlow(NecromancerDialogue, this);

                // Force conversation on campaign map
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(necroChar)
                );

            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("The necromancer leader is not available."));
            }
        }

        private DialogFlow NecromancerDialogue => DialogFlow.CreateDialogFlow("start", 125)
     .NpcLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_1"))
     .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "vortiak_mounted_necromancer_lord")

         .PlayerLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_2"))
             .NpcLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_3"))
             .PlayerLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_4"))
             .NpcLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_5"))
             .PlayerLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_6"))
             .NpcLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_8"))
             .PlayerLine(GameTexts.FindText("rf_seventh_quest_necromancer_dialog_9"))
             .Consequence(() =>
             {
                 interceptorDefeatLog?.UpdateCurrentProgress(1);
                 InformationManager.DisplayMessage(new InformationMessage("Fight the Vortiaks!"));

             })
             .CloseDialog();


        private DialogFlow OwlRebelionDialog => DialogFlow.CreateDialogFlow("start", 115)
          .NpcLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_1")).Condition(() => owlRebellionlLog?.CurrentProgress == 0 &&
            Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "rf_the_owl")
            .BeginPlayerOptions()
            // 🏹 Player chooses to fight the Vortiaks (Original Quest Path)
            .PlayerOption(GameTexts.FindText("rf_seventh_quest_owl_dialog_2"))
            .NpcLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_3"))
            .PlayerLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_4"))
            .NpcLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_5"))
             .PlayerLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_6"))
            .Consequence(() =>
            {
                owlRebellionlLog?.UpdateCurrentProgress(1);
                StartBossBattleObjective();
            })
          .CloseDialog()
        // ⚔️ Player chooses to betray the Elveans and join the Vortiaks
        .PlayerOption(GameTexts.FindText("rf_seventh_quest_owl_dialog_7"))
        .NpcLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_8"))
        .PlayerLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_9"))
        .NpcLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_10"))
        .PlayerLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_11"))

            .Consequence(() =>
            {
                _playerChoseToDestroyElveans = true;
                owlRebellionlLog?.UpdateCurrentProgress(1);
                SpawnVortiakClanParty();
                StartDestroyElveanObjective();
            })
          .CloseDialog()
          .EndPlayerOptions()
           .CloseDialog();
        private void OpenOwlRebelionDialogue()
        {
            Hero owlHero = TheOwl;

            if (owlHero == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("[ERROR] The Owl character is missing or not in the player's party!", Colors.Red));
                return;
            }

            // Ensure dialogue is added before conversation opens
            Campaign.Current.ConversationManager.AddDialogFlow(OwlRebelionDialog, this);

            CampaignMapConversation.OpenConversation(
                new ConversationCharacterData(CharacterObject.PlayerCharacter),
                new ConversationCharacterData(owlHero.CharacterObject)
            );
        }



        private void StartBossBattleObjective()
        {
            bossBattleLog = AddDiscreteLog(
                GameTexts.FindText("rf_seventh_quest_boss_log"),
                GameTexts.FindText("rf_seventh_quest_boss_task"),
                0, 1
            );
            QuestUIManager.ShowNotification(
                "Your next objective is to defeat the Queen of Darkness. Seek out the Vortiak Priestess!",
                null,
                true,
                "huntthewitch"
            );
        }

        private DialogFlow PostBattleWitchDialog => DialogFlow.CreateDialogFlow("start", 125)
         .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_1")) // "You think this ends with me bleeding on the floor?"
         .Condition(() => bossBattleLog?.CurrentProgress == 2 && CharacterObject.OneToOneConversationCharacter?.StringId == "evil_witch")
         .PlayerLine(GameTexts.FindText("rf_seventh_quest_witch_final_2")) // "You’ve lost, Witch. This land will be free of your poison."
         .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_3")) // "Foolish mortal. You’ve only shattered the vessel. The storm is still coming."
         .PlayerLine(GameTexts.FindText("rf_seventh_quest_witch_final_4")) // "We will face whatever comes. And we will win."
         .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_5")) // "Then prepare your grave, champion. My children are not done yet..."
         .Consequence(() =>
         {
             StartFinalWitchArmySequence();
         })
         .CloseDialog();

        private void StartFinalWitchArmySequence()
        {
            priestessArmyLog = AddDiscreteLog(
                GameTexts.FindText("rf_seventh_quest_priestess_army_log"),
                GameTexts.FindText("rf_seventh_quest_priestess_army_task"),
                0, 1
            );
             SpawnFinalWitchArmy();
            bossBattleLog?.UpdateCurrentProgress(3);
        }

        private void SpawnFinalWitchArmy()
        {
            if (_witchFinalArmy != null && _witchFinalArmy.IsActive)
            {
                InformationManager.DisplayMessage(new InformationMessage("Witch army already exists."));
                return;
            }
            try
            {
                // 1) Pull troop list (including the Witch herself)
                if (!WitchArmies.TryGetValue("rf_witch_final", out var troopDetails))
                    throw new Exception("Witch army configuration not found.");

                // 2) Create the bandit-style party
                var clan = Clan.All.FirstOrDefault(c => c.StringId == "vortiaks")
                           ?? Clan.All.First();
                var party = BanditPartyComponent.CreateBanditParty(clan.StringId, clan, null, true)
                            ?? throw new Exception("Failed to create Witch final army.");

              
                _witchFinalArmy = party;

                // 3) Build the roster
                var roster = TroopRoster.CreateDummyTroopRoster();
                // Add the Witch as “lord”
                var witchHeroObj = CharacterObject.Find("evil_witch")
                                  ?? throw new Exception("evil_witch template not found!");
                roster.AddToCounts(witchHeroObj, 1);

                foreach (var d in troopDetails)
                {
                    var troop = CharacterObject.Find(d.TroopId);
                    if (troop != null)
                        roster.AddToCounts(troop, d.Quantity);
                    else
                        InformationManager.DisplayMessage(
                            new InformationMessage($"Troop '{d.TroopId}' not found.", Colors.Red));
                }

                // 4) Spawn around the Ruined Temple
                var temple = Settlement.Find("vortiak_ruined_temple")
                             ?? Settlement.All.First(s => s.IsTown);
                var originPos = temple.Position2D;
                party.InitializeMobilePartyAroundPosition(
                    roster,
                    TroopRoster.CreateDummyTroopRoster(),
                    originPos,
                    10f,
                    10f
                );

                // 5) Name, aggressiveness and patrol-style AI (like Demon Lords)
                party.SetCustomName(new TextObject("Vortiak Witch’s Host"));
                party.Aggressiveness = 100f;
                party.Ai.SetMovePatrolAroundPoint(temple.Position2D);
                party.Ai.SetMoveEngageParty(MobileParty.MainParty);                              
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"Exception spawning Witch army: {ex.Message}", Colors.Red)
                );
            }
           
        }

        private DialogFlow WitchFinalEncounterDialogue => DialogFlow.CreateDialogFlow("start", 125)
     .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_6"))
     .Condition(() => !_witchConversationCompleted
           && priestessArmyLog?.CurrentProgress == 0
           && CharacterObject.OneToOneConversationCharacter?.StringId == "evil_witch")
     // 🧠 Persuasion Fork: Begin Choices
     .BeginPlayerOptions()
     .PlayerOption(GameTexts.FindText("rf_seventh_quest_witch_final_7"))
     .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_8"))
     .PlayerLine(GameTexts.FindText("rf_seventh_quest_witch_final_9"))
     .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_10"))
     .Consequence(() =>
     {
         _witchConversationCompleted = true;
         _playerChoseToDestroyElveans = true;
         SpawnVortiakClanParty();
         StartDestroyElveanObjective();
        

     })
         .CloseDialog()

      // 🛡 Option 2: Reject her offer and remain honorable
      .PlayerOption(GameTexts.FindText("rf_seventh_quest_witch_final_11"))
     .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_12"))
     .PlayerLine(GameTexts.FindText("rf_seventh_quest_witch_final_13"))
     .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_14"))
         .Consequence(() =>
         {
             _witchConversationCompleted = true;
             SpawnVortiakClanParty();
         })
            .CloseDialog()
          .EndPlayerOptions()
           .CloseDialog();

        private void RemoveEvilWitchFromPrisoners()
        {
            var prison = MobileParty.MainParty.PrisonRoster;
            if (prison == null) return;

            var witch = CharacterObject.Find(witchCharacterId);
            if (witch == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Evil Witch character not found."));
                return;
            }

            int witchCount = prison.GetTroopCount(witch);
            if (witchCount > 0)
            {
                prison.AddToCounts(witch, -witchCount);
                InformationManager.DisplayMessage(new InformationMessage("The Evil Witch has been removed from your prisoners."));
            }
        }
        private void ShowWitchDefeatedNotification()
        {
            QuestUIManager.ShowNotification(
                   "The corpses amount by thousands, and the sky seemed to be red reflecting the blood on the ground. As the ones survived prayed in gratitude, a strange shadow formed amidst the dead bodies.",
                ShowTreatyNotification,
                true,
                "postwitchfight"   // your custom sprite
            );
        }

        private void ShowTreatyNotification()
        {
            QuestUIManager.ShowNotification(
                "In a twisted blurry shape it rose up above you, a strange light within it staring at you as flickering eyes. She is not dead,” said the Owl. “Her spirit prevails.",
                ShowOwlNotification,
                true,
                "watchtheshadow"
            );
        }

        private void ShowOwlNotification()
        {
            QuestUIManager.ShowNotification(
                "But from within the shadow, a dark winged creature took shape. With incredible speed, it flew towards the highest mountain peak, fading from sight.",
                ShowFinalNotification,
                true,
                "flyingshadow"
            );
        }

        private void ShowFinalNotification()
        {
            QuestUIManager.ShowNotification(
                "As it desappears into the horizon, a sense of fulfillment lay mixed in your heart with an underlying feeling of despair. Now you celebrate, cause you have defeated this evil. But for how long?",
                FinalizeSeventhQuestSuccess,
                true,
                "witchdefeat"
            );
        }

        private void StartDestroyElveanObjective()
        {
            elveanDestructionObjectiveLog = AddDiscreteLog(
            GameTexts.FindText("rf_seventh_quest_elven_destruction_log"),
            GameTexts.FindText("rf_seventh_quest_elven_destruction_task"),
            0, 1
        );
            InformationManager.DisplayMessage(new InformationMessage("You have chosen to destroy the Elveans!"));

            _shouldTriggerOwlDialogue = true;
        }

        private void CheckElveanDestructionProgress()
        {
            if (_playerChoseToDestroyElveans && elveanDestructionObjectiveLog != null)
            {
                var elveanFaction = Kingdom.All.FirstOrDefault(k => k.Culture.StringId == "battania");
                if (elveanFaction != null && elveanFaction.Clans.All(c => c.IsEliminated))
                {
                    elveanDestructionObjectiveLog.UpdateCurrentProgress(1);

                    InformationManager.ShowInquiry(new InquiryData(
                        "Elveans Destroyed!",
                        "The Elveans have been wiped out. The Vortiaks are now free to rise!",
                        true, false, "Continue", null, null, () =>
                        {
                            // 🔥 Now that they are truly dead, spawn the Dark Kingdom
                            SpawnDarkElveanKingdom();

                            // Finish the dark path
                            InformationManager.ShowInquiry(new InquiryData(
                                "Revenge Successfull",
                                "The Vortiaks have been avenged under your leadership!",
                                true, false, "Continue", null, null, null
                            ));

                            Hero.MainHero.Clan.Renown += 450;
                            Clan.PlayerClan.Influence += 1000f;
                            Hero.MainHero.Gold += 500000;
                            ApplyDarkPathConsequences();
                        }
                    ));
                }
            }
        }

        private void FinalizeSeventhQuestSuccess()
        {
            CreateNewVortiaksKingdom(); // always create this (The Owl faction)
            deformedSpawningEnabled = true;
            _nextDeformedSpawnTime = CampaignTime.DaysFromNow(3);
            if (_playerChoseToDestroyElveans)
            {
                // ❗ IMPORTANT: DO NOT spawn Dark Elvean Kingdom here anymore!
                // Only inform player that the next goal is to destroy Elveans
                InformationManager.ShowInquiry(new InquiryData(
                    "Dark Path Unlocked",
                    "You have chosen to destroy the Elveans. Their downfall now depends on your actions!",
                    true, false, "Continue", null, null, null
                ));

                // Keep Deformed spawning and tracking
              

                // 🔥 DO NOT call SpawnDarkElveanKingdom() here
            }
            else
            {
                // ⚪ Good path
                InformationManager.ShowInquiry(new InquiryData(
                    "Peace Restored",
                    "You brought peace back to the land. Well done, champion!",
                    true, false, "Continue", null, null, null
                ));

                Hero.MainHero.Clan.Renown += 1000;
                Clan.PlayerClan.Influence += 1000f;
                Hero.MainHero.Gold += 500000;
            }

            AddLog(GameTexts.FindText("rf_seventh_quest_completed_log"));
            priestessArmyLog?.UpdateCurrentProgress(2);
            CleanupDevilsAndNelrogParties();
            CompleteQuestWithSuccess();
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DeformedPostQuestTick);
            _nextDeformedSpawnTime = CampaignTime.Now; // Or DaysFromNow(3)
        }
        private void DeformedPostQuestTick()
        {
            if (_deformedPartySpawnCount >= 3)
            {
                // Optional: remove if you want it to end after 3
                CampaignEvents.DailyTickEvent.ClearListeners(this);
                return;
            }

            if (CampaignTime.Now >= _nextDeformedSpawnTime)
            {
                SpawnDeformedParties();
                _nextDeformedSpawnTime = CampaignTime.DaysFromNow(5);
            }
        }
        private void ApplyDarkPathConsequences()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                string cultureId = kingdom.Culture.StringId;

                if (cultureId != "sturgia" && cultureId != "aserai" && cultureId != "urkhai")
                {
                    // Reduce relation with their leaders
                    if (kingdom.Leader != null)
                    {
                        Hero.MainHero.SetPersonalRelation(kingdom.Leader, -200);
                    }

                    // Declare war
                    if (!FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, kingdom))
                    {
                        FactionManager.DeclareWar(Hero.MainHero.MapFaction, kingdom);
                    }
                }
            }

            InformationManager.DisplayMessage(new InformationMessage(
                "You have declared war on the realms of Aeurth (except for Sturgia, Aserai, and Urkhai). Relations have worsened.", Colors.Red));
            
        }

        private Hero GetOrCreateDeformedOwner()
        {
            if (DeformedLeaderHero != null)
                return DeformedLeaderHero;

            var template = CharacterObject.Find("orc_base_infantry");
            if (template == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Template 'orc_base_infantry' not found!", Colors.Red));
                return null;
            }

            var spawnSettlement = Settlement.Find("town_S1") ?? Settlement.All.FirstOrDefault(s => s.IsTown);
            var hero = HeroCreator.CreateSpecialHero(template, spawnSettlement);
            hero.ChangeState(Hero.CharacterStates.Active);
            hero.SetName(new TextObject("Deformed Overlord"), new TextObject("Deformed Overlord"));
            hero.HeroDeveloper.SetInitialLevel(20);
            hero.Clan = Clan.FindFirst(c => c.StringId == "vortiaks");
            DeformedLeaderHero = hero;
            return hero;
        }


        private void SpawnDeformedParties()
        {
            try
            {
                Random rnd = new Random();

                Clan deformedClan = Clan.FindFirst(c => c.StringId == "vortiaks");
                if (deformedClan == null)
                {
                    Debug.PrintError("❌ Error: Deformed clan 'vortiaks' not found.");
                    return;
                }

                foreach (Clan clan in Clan.All)
                {
                    if (clan != deformedClan && !clan.IsEliminated)
                    {
                        FactionManager.DeclareWar(deformedClan, clan);
                    }
                }

                List<Hideout> randomHideouts = Hideout.All
                    .Where(h => h != null && h.IsInfested && h.Settlement != null)
                    .OrderBy(h => rnd.Next())
                    .Take(10)
                    .ToList();

                if (!DeformedArmies.TryGetValue("rf_deformed_army", out var troopDetails))
                {
                    Debug.PrintError("❌ Error: Deformed army 'rf_deformed_army' not found in dictionary.");
                    return;
                }

                foreach (var hideout in randomHideouts)
                {
                    TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                    foreach (var detail in troopDetails)
                    {
                        var troop = CharacterObject.Find(detail.TroopId);
                        if (troop != null)
                        {
                            troopRoster.AddToCounts(troop, detail.Quantity);
                        }
                        else
                        {
                            Debug.PrintError($"⚠️ Troop {detail.TroopId} not found.");
                        }
                    }

                    string partyId = $"deformed_{hideout.StringId}_{MBRandom.RandomInt(10000, 99999)}";

                    MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, deformedClan, hideout, true);

                    if (party == null)
                    {
                        Debug.PrintError($"❌ Failed to create Deformed party '{partyId}' at {hideout.Settlement.Name}.");
                        continue;
                    }

                    party.SetCustomName(new TextObject("Deformed Villager Party"));

                    party.InitializeMobilePartyAroundPosition(
                        troopRoster,
                        TroopRoster.CreateDummyTroopRoster(),
                        hideout.Settlement.Position2D,
                        150f,
                        10f
                    );

                    party.Aggressiveness = 100f;
                    party.SetPartyObjective(MobileParty.PartyObjective.Aggressive);
                    party.Ai.SetDoNotMakeNewDecisions(false);

                    MobileParty closestTarget = MobileParty.All
                        .Where(p =>
                            p != party &&
                            p.IsActive &&
                            p.MapFaction != null &&
                            party.MapFaction != null &&
                            p.MapFaction.IsAtWarWith(party.MapFaction)
                        )
                        .OrderBy(p => party.Position2D.DistanceSquared(p.Position2D))
                        .FirstOrDefault();

                    if (closestTarget != null)
                    {
                        party.Ai.SetMoveEngageParty(closestTarget);
                    }

                    InformationManager.DisplayMessage(new InformationMessage(
                        $"✅ Spawned Deformed Party: {party.StringId} at position (X: {party.Position2D.X:0}, Y: {party.Position2D.Y:0})"
                    ));
                }

                InformationManager.ShowInquiry(new InquiryData(
                    "⚠️ Abominations Emerging!",
                    "Rumors spread across the land: deformed, twisted troops have been spotted raiding and attacking settlements. Beware their growing numbers!",
                    true, false, "Close", null, null, null
                ));

                InformationManager.DisplayMessage(new InformationMessage($"✅ Deformed Villager parties refreshed this month."));
            }
            catch (Exception ex)
            {
                Debug.PrintError($"❌ Exception in SpawnDeformedParties: {ex}");
                InformationManager.DisplayMessage(new InformationMessage($"❌ Exception spawning Deformed parties: {ex.Message}", Colors.Red));
            }

            _deformedPartySpawnCount++;
            InformationManager.DisplayMessage(new InformationMessage($"Deformed Parties Spawned Times: {_deformedPartySpawnCount}"));

            if (_deformedPartySpawnCount >= 3 && !_eighthQuestStarted)
            {
                ShowEighthQuestPrompt();
            }
        }
        private void ShowEighthQuestPrompt()
        {
            _shouldOpenEighthQuestDialog = true;

            InformationManager.ShowInquiry(new InquiryData(
                "The Forest Whispers",
                "The Priestess of the First Tree senses something dark and twisted growing. She requests your presence.",
                true, false,
                "Continue", null,
                () =>
                {
                    eighthPriestessLog = AddLog(GameTexts.FindText("rf_seventh_quest_outro"));
                   _shouldOpenEighthQuestDialog = false;
                },
                null
            ));
        }

        private DialogFlow PriestessEighthQuestDialog => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(new TextObject("🌳 The winds whisper of darkness once again. You must go forth, hero."))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "elvean_first_tree_druid_quest_2" && 
                            eighthPriestessLog != null && eighthPriestessLog.CurrentProgress == 0 && _priestessConversationTriggered)
            .PlayerLine(new TextObject("I shall heed the call of the First Tree."))
            .Consequence(() =>
            {
             eighthPriestessLog.UpdateCurrentProgress(1);
             StartEighthQuest();
             InformationManager.DisplayMessage(new InformationMessage("🌳 You accepted the Priestess's call. Quest started."));
            })
            .CloseDialog();

        private void StartEighthQuest()
        {
            if (_eighthQuestStarted)
                return;

            string questId = "eighth_quest_" + MBRandom.RandomInt(10000, 99999);

            var quest = new EighthQuest(questId, Hero.MainHero, CampaignTime.Days(30), 5000);
            quest.StartQuest();

            _eighthQuestStarted = true;
            InformationManager.DisplayMessage(new InformationMessage("🌳 The Eighth Quest has begun!"));
        }

        // ------------------------------------------------------
        // UTILITIES: TROOPS & ARMOR
        // ------------------------------------------------------

        private void CleanupDevilsAndNelrogParties()
        {
            var partiesToRemove = MobileParty.All
                .Where(p => p.IsActive
                    && (
                        (p.Name != null && (
                            p.Name.ToString().Contains("Devils Party") ||
                            p.Name.ToString().Contains("Demon Lord")
                        )) ||
                        (p.StringId != null && p.StringId.Contains("nelrogs"))
                    ))
                .ToList();

            foreach (var party in partiesToRemove)
            {
                party.RemoveParty();
                InformationManager.DisplayMessage(new InformationMessage($"❌ Removed party: {party.Name} ({party.StringId})"));
            }
        }
        private void GivePlayerTroops(List<(string troopId, int troopCount)> troops)
        {
            foreach (var (troopId, troopCount) in troops)
            {
                var troop = CharacterObject.Find(troopId);
                if (troop != null)
                {
                    MobileParty.MainParty.AddElementToMemberRoster(troop, troopCount);
                    InformationManager.DisplayMessage(
                        new InformationMessage($"You have received {troopCount} {troop.Name}.")
                    );
                }
                else
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Troop with ID '{troopId}' was not found.")
                    );
                }
            }
        }

        private void GivePlayerArmorSet(List<string> itemIds)
        {
            foreach (var itemId in itemIds)
            {
                var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                if (itemObject != null)
                {
                    MobileParty.MainParty.ItemRoster.Add(new ItemRosterElement(itemObject, 1));
                    InformationManager.DisplayMessage(
                        new InformationMessage($"You have received {itemObject.Name}.")
                    );
                }
                else
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage($"Item with ID '{itemId}' was not found.")
                    );
                }
            }
        }

        private void CreateNewVortiaksKingdom()
        {
            var sturgiaCulture = MBObjectManager.Instance.GetObject<CultureObject>("sturgia");
            var sturgiaKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "sturgia");

            if (sturgiaCulture == null || sturgiaKingdom == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Could not find Sturgian culture or kingdom."));
                return;
            }

            Hero theOwl = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == "rf_the_owl");
            if (theOwl == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Could not find 'rf_the_owl'."));
                return;
            }

            // 🔥 Detach The Owl from existing clan/kingdom
            if (theOwl.Clan != null)
            {
                if (theOwl.Clan.Heroes.Contains(theOwl))
                    theOwl.Clan.Heroes.Remove(theOwl);

                if (theOwl.Clan.Kingdom != null && theOwl.Clan.Kingdom.Clans.Contains(theOwl.Clan))
                    theOwl.Clan.Kingdom.Clans.Remove(theOwl.Clan);

                theOwl.Clan = null;
            }

            theOwl.SetName(new TextObject("The Owl"), new TextObject("The Owl"));

            uint primaryColor = 0xff0B0C11;
            uint secondaryColor = 0xffCEDAE7;
            string bannerKey = "3.116.41.1140.1445.779.774.1.0.-91.116.22.116.203.203.920.955.1.0.0.306.21.116.248.248.630.578.1.0.0";
            Banner banner = new Banner(bannerKey, primaryColor, secondaryColor);

            Clan newClan = MBObjectManager.Instance.CreateObject<Clan>("clan_newvortiaks");
            TextObject clanName = new TextObject("Clan of the Vortiaks");
            newClan.InitializeClan(clanName, clanName, sturgiaCulture, banner, new Vec2(0, 0), false);
            newClan.SetLeader(theOwl);

            // ✅ Assign to Sturgia
            newClan.Kingdom = sturgiaKingdom;
            sturgiaKingdom.Clans.Add(newClan);

            // ✅ Optional: grant them a settlement
            var settlement = Settlement.Find("castle_S8");
            if (settlement != null)
            {
                ChangeOwnerOfSettlementAction.ApplyByDefault(theOwl, settlement);
                InformationManager.DisplayMessage(new InformationMessage($"✅ {settlement.Name} granted to The Owl."));
            }

            InformationManager.DisplayMessage(new InformationMessage("🏰 The Vortiaks rise again — under the Kingdom of Sturgia!"));
        }
        private void SpawnDarkElveanKingdom()
        {
            try
            {
                // 1. Find the NPC template first
                CharacterObject darkElveanTemplate = CharacterObject.Find("dark_elvean_lord");
                if (darkElveanTemplate == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Could not find 'dark_elvean_lord' template.", Colors.Red));
                    return;
                }

                // 2. Create the main Hero from Template
                Hero darkElveanHero = HeroCreator.CreateSpecialHero(
                    darkElveanTemplate,
                    Settlement.FindFirst(x => x.IsTown),
                    null, null,
                    30 // starting age
                );

                darkElveanHero.ChangeState(Hero.CharacterStates.Active);
                darkElveanHero.SetName(new TextObject("Lord Varakar"), new TextObject("Lord Varakar of the Dark Elveans"));

                // 3. Create a custom Banner (you can paste your own!)
                string customBannerKey = "35.116.100.1140.1445.779.774.1.0.-91.434.121.116.240.240.920.955.1.0.0.407.121.116.248.248.630.578.1.0.0"; // <<< change this manually!
                uint primaryColor = 0xff332c4d;
                uint secondaryColor = 0xffFDE217;
                Banner customBanner = new Banner(customBannerKey, primaryColor, secondaryColor);

                // 4. Create the Clan
                Clan darkElveanClan = MBObjectManager.Instance.CreateObject<Clan>("clan_dark_elveans");
                darkElveanClan.InitializeClan(
                    new TextObject("Dark Elveans"),
                    new TextObject("Dark Elveans"),
                    darkElveanHero.Culture,
                    customBanner,
                    new Vec2(0, 0),
                    false
                );
                darkElveanClan.SetLeader(darkElveanHero);
                darkElveanHero.Clan = darkElveanClan;

                // 5. Now spawn extra lords AFTER clan creation
                for (int i = 0; i < 2; i++) // Adjust number if you want more
                {
                    Hero newLord = HeroCreator.CreateSpecialHero(
                        darkElveanTemplate,
                        Settlement.FindFirst(x => x.IsTown),
                        null, null,
                        MBRandom.RandomInt(28, 40)
                    );

                    newLord.SetName(new TextObject($"Varakar Lieutenant {i + 1}"), new TextObject($"Varakar Lieutenant {i + 1}"));
                    newLord.Clan = darkElveanClan;
                    newLord.ChangeState(Hero.CharacterStates.Active);

                    darkElveanClan.Heroes.Add(newLord);

                    InformationManager.DisplayMessage(new InformationMessage($"✅ Spawned {newLord.Name} into Dark Elveans Clan!"));
                }

                // 6. Create the Kingdom
                Kingdom darkElveanKingdom = MBObjectManager.Instance.CreateObject<Kingdom>("kingdom_dark_elveans");
                darkElveanKingdom.InitializeKingdom(
                    new TextObject("Dark Elvean Dominion"),
                    new TextObject("Dark Elvean Dominion"),
                    darkElveanHero.Culture,
                    customBanner,
                    primaryColor,
                    secondaryColor,
                    Settlement.FindFirst(s => s.Culture.StringId == "battania" && s.IsTown),
                    new TextObject("Dark King"),
                    new TextObject("Dark King"),
                    new TextObject("Dark Elveans")
                );

                Campaign.Current.Kingdoms.Add(darkElveanKingdom);

                darkElveanKingdom.RulingClan = darkElveanClan;
                darkElveanClan.Kingdom = darkElveanKingdom;

                // 7. Capture former Elvean settlements (Battanian towns and castles)
                var battaniaSettlements = Settlement.All
                    .Where(s => s.Culture.StringId == "battania" && (s.IsTown || s.IsCastle))
                    .ToList();

                foreach (var settlement in battaniaSettlements)
                {
                    if (settlement.OwnerClan != null)
                    {
                        ChangeOwnerOfSettlementAction.ApplyByDefault(darkElveanHero, settlement);
                        InformationManager.DisplayMessage(new InformationMessage($"🏰 {settlement.Name} seized by the Dark Elveans!"));
                    }
                }

                InformationManager.DisplayMessage(new InformationMessage("✅ Dark Elvean Dominion has risen under Lord Varakar!"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Exception spawning Dark Elvean Kingdom: {ex.Message}", Colors.Red));
            }
        }

        protected override void SetDialogs()
        {
            // Force-add our druid, dwarf king, necromancer flows. 
            // They no longer rely on a settlement condition.
            Campaign.Current.ConversationManager.AddDialogFlow(DruidInteractionDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(DwarfKingDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(NecromancerDialogue, this);
            Campaign.Current.ConversationManager.AddDialogFlow(OwlRebelionDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(PostBattleWitchDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(InterceptorEncounterDialogue, this);
            Campaign.Current.ConversationManager.AddDialogFlow(WitchFinalEncounterDialogue, this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessEighthQuestDialog, this);
        }
    }

    // Helper class for spawning interceptor parties
    public class TroopDetail
    {
        public string TroopId { get; }
        public int Quantity { get; }

        public TroopDetail(string troopId, int quantity)
        {
            TroopId = troopId;
            Quantity = quantity;
        }
    }
}


