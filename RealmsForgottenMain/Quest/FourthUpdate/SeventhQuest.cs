using RealmsForgotten.AiMade.RF_Diplomacy;
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
using TaleWorlds.CampaignSystem.GameState;
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
using static RealmsForgotten.Quest.FourthUpdate.SeventhQuest;
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
           

        [SaveableField(23)]
        private JournalLog eighthPriestessLog;

        [SaveableField(24)]
        private bool _priestessConversationTriggered;

        [SaveableField(25)]
        private bool _owlDialogueAttempted;

        private const int travelObjectiveTarget = 1;
        private bool IsTravelObjectiveCompleted => travelObjectiveLog?.CurrentProgress >= travelObjectiveTarget;

        public static bool IsVortiakClanSpawned { get; set; } = false;

        private Settlement witchHideout => Settlement.Find("vortiak_ruined_temple");

        private static readonly string witchCharacterId = "evil_witch";

        private Vec2 _lastPlayerPosition = Vec2.Invalid;

        private bool _dialogEventsRegistered = false;
        public int DruidInteractionStage => druidInteractionLog?.CurrentProgress ?? -1;

        private Hero? DeformedLeaderHero = null;
        public record TroopDetail(string TroopId, int Quantity);

        static Dictionary<string, WitchLord>? _witchLords;

        private bool _shouldCleanupPartiesNextTick = false;

        public static SeventhQuest? ActiveSeventhQuestInstance;

        private bool _postQuestEventsRegistered = false;
        public record WitchLord(string CharacterId, string ClanId, List<TroopDetail> TroopDetails, Settlement SpawnSettlement);
        // Interceptor army configuration
        private static readonly Dictionary<string, List<TroopDetail>> InterceptorArmies =
            new Dictionary<string, List<TroopDetail>>
            {
                ["rf_interceptor_default"] = new List<TroopDetail>
                {
                    new TroopDetail("vortiak_mounted_necromancer_lord", 1),
                    new TroopDetail("vortiak_warrior", 150),
                    new TroopDetail("vortiak_crossbowman", 200),
                    new TroopDetail("vortiak_swordsman", 300),
                    new TroopDetail("cs_nelrog_bandits_chief", 150),
                    new TroopDetail("hellbound_boss", 50),
                    new TroopDetail("hellbound_chief", 100)
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

        public static Dictionary<string, WitchLord> WitchLords
        {
            get
            {
                _witchLords ??= new()
                {
                    ["vortiak_coven"] = new WitchLord(
                        "vortiak_witch_boss",    // Character ID of the first Witch Lord leader
                        "vortiak_witch",             // ** The required Clan ID **
                        new List<TroopDetail> {      // Troop composition
                        new TroopDetail("vortiak_witch_bandit",700),
                        new TroopDetail("vortiak_witch_raider", 600),
                        new TroopDetail("vortiak_witch_chief", 450),
                        new TroopDetail("cs_devils_bandits_bandit", 450),
                        new TroopDetail("vortiak_boss", 100)
                        },
                        Settlement.FindFirst(s => s.StringId == "vortiak_ruined_temple") // Example spawn settlement ID (Sargot)
                    ),

                };

                return _witchLords;
            }
        }    
       

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
            ActiveSeventhQuestInstance = this;
        }
        public override TextObject Title => GameTexts.FindText("rf_seventh_quest_title");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => true;

        protected override void InitializeQuestOnGameLoad()
        {
            ActiveSeventhQuestInstance = this;
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
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            RegisterEvents();
            _shouldShowPopups = true;  // Wait one day, then show the “letter from the Elven King”
            _druidConversationTriggered = false; // We haven't forced the conversation yet
        }

        private void OnTick(float dt)
        {
            if (_shouldCleanupPartiesNextTick)
            {
                _shouldCleanupPartiesNextTick = false;
                CleanupDevilsAndNelrogParties();
            }
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
                _nextDeformedSpawnTime = CampaignTime.DaysFromNow(3);
            }

            // 🔥 Show the Eighth Quest inquiry once when spawn count hits 3
            if (_deformedPartySpawnCount >= 3 && !_eighthQuestStarted)
            {
                ShowEighthQuestPrompt(); // the inquiry
            }

            if (_deformedPartySpawnCount >= 3 && !_eighthQuestStarted && !_owlDialogueAttempted)
            {
                _owlDialogueAttempted = true;
                TryStartOwlConversation();
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
        private void TryStartOwlConversation()
        {
            Hero owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");
            if (owl != null && Game.Current?.GameStateManager?.ActiveState is MapState)
            {
                CharacterObject owlChar = owl.CharacterObject;
                if (owlChar != null)
                {
                    Campaign.Current.ConversationManager.AddDialogFlow(PriestessEighthQuestDialog, this);
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(owlChar)
                    );
                    return;
                }
            }

            // Fallback retry on next DailyTick if conversation failed
            _owlDialogueAttempted = false;
        }

        public void SpawnAllWitchLords()
        {
            foreach (WitchLord lord in WitchLords.Values)
            {
                var party = CreateWitchLordParty(lord.CharacterId, lord.ClanId, lord.SpawnSettlement, lord.TroopDetails);
                _witchFinalArmy = party; // ✅ Track the one coven as the final witch army
            }

            InformationManager.DisplayMessage(
                new InformationMessage("The Vortiak Witch was spotted around Nippura!", Colors.Magenta));
        }


        public MobileParty? CreateWitchLordParty(string witchLordCharacterId, string clanId, Settlement spawnSettlement, List<TroopDetail> troopDetails)
        {
            if (spawnSettlement == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Spawn settlement is null for Witch Lord '{witchLordCharacterId}'.", Colors.Red));
                return null;
            }

            try
            {
                Clan clan = Clan.FindFirst(x => x.StringId == clanId);
                if (clan == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Error: Clan '{clanId}' not found for Witch Lord '{witchLordCharacterId}'.", Colors.Red));
                    return null;
                }

                if (!clan.IsBanditFaction && !clan.IsMinorFaction && !FactionManager.IsAtWarAgainstFaction(clan, Hero.MainHero.MapFaction))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Warning: Witch Lord Clan '{clanId}' is not hostile to the player.", Colors.Yellow));
                }

                // ✅ Create bandit-style party like Demon Lords
                MobileParty party = BanditPartyComponent.CreateBanditParty($"vortiak_witch_party_{witchLordCharacterId}", clan, null, true);
                if (party == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Error: Failed to create party for Witch Lord clan '{clanId}'.", Colors.Red));
                    return null;
                }

                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                CharacterObject leaderCharacter = CharacterObject.Find(witchLordCharacterId);
                if (leaderCharacter == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Error: Character '{witchLordCharacterId}' not found for Witch Lord.", Colors.Red));
                    party.RemoveParty();
                    return null;
                }

                // ✅ DO NOT assign party leader (matches Demon Lords)
                troopRoster.AddToCounts(leaderCharacter, 1);

                foreach (var troopDetail in troopDetails)
                {
                    CharacterObject troop = CharacterObject.Find(troopDetail.TroopId);
                    if (troop == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Warning: Troop '{troopDetail.TroopId}' not found for Witch Lord party '{witchLordCharacterId}'.", Colors.Yellow));
                        continue;
                    }
                    troopRoster.AddToCounts(troop, troopDetail.Quantity);
                }

                if (troopRoster.GetTroopCount(leaderCharacter) == 0)
                    troopRoster.AddToCounts(leaderCharacter, 1);

                Settlement fixedSpawnSettlement = Settlement.Find("town_S2");
                Vec2 spawnPosition = fixedSpawnSettlement?.Position2D ?? spawnSettlement.Position2D;

                party.InitializeMobilePartyAroundPosition(
                    troopRoster,
                    TroopRoster.CreateDummyTroopRoster(),
                    spawnPosition,
                    30f,
                    10f
                );

                MobileParty? target = MobileParty.All
                .Where(p =>
                    p != party &&
                    p.IsActive &&
                    (p.IsLordParty || p.IsCaravan || p.IsBandit || p.IsMilitia || p.IsVillager) &&
                    p.MapFaction != null &&
                    FactionManager.IsAtWarAgainstFaction(party.MapFaction, p.MapFaction)
                )
                 .OrderBy(p => p.Position2D.DistanceSquared(party.Position2D))
                   .FirstOrDefault();

                if (target != null)
                {
                    party.Ai.SetMoveEngageParty(target);
                    InformationManager.DisplayMessage(new InformationMessage($"Witch Lord party is attacking {target.Name}."));
                }
                else
                {
                    party.Ai.SetMovePatrolAroundPoint(spawnSettlement.Position2D); // fallback
                }

                party.ActualClan = clan;
                party.SetCustomName(new TextObject($"Vortiak Coven ({leaderCharacter.Name})"));
                party.Aggressiveness = 10f;
                party.Party.SetVisualAsDirty();
               
                InformationManager.DisplayMessage(new InformationMessage($"A Vortiak Witch Lord coven led by {leaderCharacter.Name} has been sighted near {spawnSettlement.Name}.", Colors.Red));
                return party;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Exception creating Witch Lord Party ({witchLordCharacterId}): {ex.Message}", Colors.Red));
                return null;
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
         .NpcLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_11_b"))
            .Consequence(() =>
            {
                owlRebellionlLog?.UpdateCurrentProgress(1);
                StartBossBattleObjective();
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
            SpawnAllWitchLords();
            bossBattleLog?.UpdateCurrentProgress(3);
        }

        private DialogFlow WitchFinalEncounterDialogue => DialogFlow.CreateDialogFlow("start", 125)
     .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_6"))
     .Condition(() => !_witchConversationCompleted
           && priestessArmyLog?.CurrentProgress == 0
           && PlayerEncounter.EncounteredParty?.MobileParty?.StringId.StartsWith("vortiak_witch_party") == true)
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
         })
            .CloseDialog()
          .EndPlayerOptions()
           .CloseDialog();

       
        private void RemoveEvilWitchFromPrisoners()
        {
            var prison = MobileParty.MainParty.PrisonRoster;
            if (prison == null) return;

            var witch = CharacterObject.Find("vortiak_witch_boss");
            if (witch == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Evil Witch character not found."));
                return;
            }

            int witchCount = prison.GetTroopCount(witch);
            if (witchCount > 0)
            {
                prison.AddToCounts(witch, -witchCount);
                InformationManager.DisplayMessage(new InformationMessage("The Evil Witch has escaped from your prisoners."));
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
            CreateNewVortiaksKingdom(); 
                
            _deformedPartySpawnCount = 0;
            deformedSpawningEnabled = true;
            _nextDeformedSpawnTime = CampaignTime.DaysFromNow(3);
            if (Campaign.Current.GetCampaignBehavior<AlignmentWarBehavior>() is { } behavior)
                behavior.StartGlobalAlignmentWar();
            _shouldCleanupPartiesNextTick = true;
           
            if (_playerChoseToDestroyElveans)
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Dark Path Unlocked",
                    "You have chosen to destroy the Elveans. Their downfall now depends on your actions!",
                    true, false, "Continue", null, null, null
                ));
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "The Vortiak Mystery Has Been Unveiled!",
                    "The defeat of the Vortiak Priestess has not brought peace to the land as expected. All kingdoms are now aware of the Vortiaks’ and the Elvish kind’s shared past, and the war that followed. As a result, the Elveans have lost prestige, and their claim to rightful inheritance of the land they rule is now in question. That triggered a war between kingdoms, some decided to backup the Elveans while others took the side of the Dreadrealms. The Dreadking took the Owl under his banner and granted him a castle, making him a lord of his own clan. But only you and the Owl know the truth — that the Vortiak Priestess still lives, somewhere. You chose to remain silent, leaving her fate to the gods, uncertain if or when she will return.",
                    true, false, "Continue", null, null, null
                ));

                Hero.MainHero.Clan.Renown += 1000;
                Clan.PlayerClan.Influence += 1000f;
                Hero.MainHero.Gold += 500000;
            }

            AddLog(GameTexts.FindText("rf_seventh_quest_completed_log"));
            priestessArmyLog?.UpdateCurrentProgress(2);
           
            CompleteQuestWithSuccess();
            CampaignEvents.DailyTickEvent.ClearListeners(this);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DeformedPostQuestTick);
        }
        private void DeformedPostQuestTick()
        {
            if (_shouldCleanupPartiesNextTick)
            {
                _shouldCleanupPartiesNextTick = false; // prevent repeating
                CleanupDevilsAndNelrogParties(); // ← your cleanup logic
            }
            if (_deformedPartySpawnCount >= 3)
            {
                // Optional: remove if you want it to end after 3
                CampaignEvents.DailyTickEvent.ClearListeners(this);
                return;
            }

            if (CampaignTime.Now >= _nextDeformedSpawnTime)
            {
                SpawnDeformedParties();
                _nextDeformedSpawnTime = CampaignTime.DaysFromNow(60);
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

        private void SpawnDeformedParties()
        {
            try
            {
                Clan deformedClan = Clan.FindFirst(c => c.StringId == "deformed_villagers");
                if (deformedClan == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Deformed clan not found."));
                    return;
                }

                var hideouts = Hideout.All
                    .Where(h => h.IsInfested && h.Settlement != null)
                    .OrderBy(_ => MBRandom.RandomInt())
                    .Take(10);

                foreach (var hideout in hideouts)
                {
                    SpawnDeformedParty(hideout, deformedClan);
                }

                _deformedPartySpawnCount++;

                InformationManager.ShowInquiry(new InquiryData(
                    "Reports of attacks are growing!",
                    "Twisted hosts have been spotted attacking villagers and caravans.",
                    true, false, "Close", null, null, null
                ));

                InformationManager.DisplayMessage(new InformationMessage($"✅ Deformed Villager parties spawned. Total spawns: {_deformedPartySpawnCount}"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Exception spawning Deformed parties: {ex.Message}", Colors.Red));
            }
        }

        private void SpawnDeformedParty(Hideout hideout, Clan clan)
        {
            try
            {
                string partyId = $"deformed_party_{hideout.Settlement.StringId}_{MBRandom.RandomInt(10000, 99999)}";
                MobileParty party = BanditPartyComponent.CreateBanditParty(partyId, clan, hideout, true);

                if (party == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"❌ Failed to create party for hideout: {hideout.Settlement.Name}."));
                    return;
                }

                TroopRoster roster = TroopRoster.CreateDummyTroopRoster();

                CharacterObject boss = CharacterObject.Find("deformed_villager_boss");
                if (boss != null)
                    roster.AddToCounts(boss, 1);

                CharacterObject troop = CharacterObject.Find("deformed_villager_bandit");
                if (troop != null)
                    roster.AddToCounts(troop, 80);

                party.InitializeMobilePartyAroundPosition(
                    roster,
                    TroopRoster.CreateDummyTroopRoster(),
                    hideout.Settlement.Position2D,
                    100f,
                    10f
                );

                party.SetCustomName(new TextObject("Deformed Villagers"));
                party.Aggressiveness = 100f;
                party.SetPartyObjective(MobileParty.PartyObjective.Aggressive);
                party.Ai.SetDoNotMakeNewDecisions(false);

                var target = MobileParty.All
                    .Where(p => p != party && p.IsActive && p.MapFaction != null && party.MapFaction != null && p.MapFaction.IsAtWarWith(party.MapFaction))
                    .OrderBy(p => party.Position2D.DistanceSquared(p.Position2D))
                    .FirstOrDefault();

                if (target != null)
                    party.Ai.SetMoveEngageParty(target);
                else
                    party.Ai.SetMovePatrolAroundPoint(hideout.Settlement.Position2D);

                InformationManager.DisplayMessage(new InformationMessage($"✅ Deformed party spawned at {hideout.Settlement.Name}."));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Error spawning Deformed party: {ex.Message}"));
            }
        }

        public void ShowEighthQuestPrompt()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "The Forest Whispers",
                "The Priestess of the First Tree sent you a messenger to inform you about recent attacks on innocent people. She asks your help to unveil the reason.",
                true, false,
                "Continue", null,
                () =>
                {
                    eighthPriestessLog = AddLog(GameTexts.FindText("rf_seventh_quest_outro"));

                    Hero owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");

                    if (owl?.CharacterObject != null)
                    {
                        // ✅ Register the dialog BEFORE conversation
                        Campaign.Current.ConversationManager.AddDialogFlow(PriestessEighthQuestDialog, this);

                        // ✅ Then start the conversation (this sets OneToOneConversationHero automatically)
                        CampaignMapConversation.OpenConversation(
                            new ConversationCharacterData(CharacterObject.PlayerCharacter),
                            new ConversationCharacterData(owl.CharacterObject)
                        );
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("❌ Could not find 'The Owl' character for dialogue!", Colors.Red));
                    }
                },
                null
            ));
        }


        private DialogFlow PriestessEighthQuestDialog => DialogFlow.CreateDialogFlow("start", 125)
     .NpcLine(new TextObject("My friend, we just received a messenger from the First Tree Priestess. She's concerned about reports of strange mutations attacking villagers."))
     .Condition(() =>
    CharacterObject.OneToOneConversationCharacter?.StringId == "rf_the_owl"
    && eighthPriestessLog != null
    && eighthPriestessLog.CurrentProgress == 0)
     .PlayerLine(new TextObject("Mutations? We've already faced zombies and demons. What else could there possibly be?"))
     .NpcLine(new TextObject("She wants us to find out. And to be honest... that worries me."))
     .PlayerLine(new TextObject("Let’s not jump to conclusions just yet. We haven’t even seen them. Did she mention where we could find a trail?"))
     .NpcLine(new TextObject("The message says the latest sightings occurred near the Tremerid Kingdom."))
     .PlayerLine(new TextObject("Then the Mages... could they be involved? Let's investigate."))
     .Consequence(() =>
     {
         eighthPriestessLog.UpdateCurrentProgress(1);
         StartEighthQuest();
         InformationManager.DisplayMessage(
             new InformationMessage("🌳 You accepted the Owl’s call. The Eighth Quest has begun!")
         );
     })
     .CloseDialog();

        private void StartEighthQuest()
        {
            if (_eighthQuestStarted)
                return;

            EighthQuest newChapter = new EighthQuest("rf_seventh_quest_outro", QuestGiver, CampaignTime.DaysFromNow(999), 20000);
            newChapter.StartQuest();
        }

        // ------------------------------------------------------
        // UTILITIES: TROOPS & ARMOR
        // ------------------------------------------------------

        private void CleanupDevilsAndNelrogParties()
        {
            var partiesToRemove = MobileParty.All
                .Where(p =>
                    p != null &&
                    p.IsActive &&
                    !p.IsDisbanding &&
                    p.MapEvent == null &&  // 🔒 Never remove if it's in a battle
                    (
                        (p.Name?.ToString().Contains("Devils Party") == true) ||
                        (p.Name?.ToString().Contains("Demon Lord") == true) ||
                        (p.StringId?.Contains("nelrogs") == true)
                    )
                )
                .ToList();

            foreach (var party in partiesToRemove)
            {
                try
                {
                    party.RemoveParty();
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"❌ Removed party: {party.Name} ({party.StringId})"));
                }
                catch (Exception ex)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"⚠️ Failed to remove {party?.Name} ({party?.StringId}): {ex.Message}", Colors.Red));
                }
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

            // Cleanly detach the Owl from any previous clan or kingdom
            if (theOwl.Clan != null && theOwl.Clan.Heroes.Contains(theOwl))
                theOwl.Clan.Heroes.Remove(theOwl);

            if (theOwl.Clan?.Kingdom != null)
                theOwl.Clan.Kingdom.Clans.Remove(theOwl.Clan);

            theOwl.Clan = null;

            theOwl.SetName(new TextObject("The Owl"), new TextObject("The Owl"));

            uint primaryColor = 0xff0B0C11;
            uint secondaryColor = 0xffCEDAE7;
            string bannerKey = "3.116.41.1140.1445.779.774.1.0.-91.116.22.116.203.203.920.955.1.0.0.306.21.116.248.248.630.578.1.0.0";
            Banner banner = new Banner(bannerKey, primaryColor, secondaryColor);

            Clan newClan = MBObjectManager.Instance.CreateObject<Clan>("clan_newvortiaks");
            TextObject clanName = new TextObject("Clan of the Vortiaks");
            newClan.InitializeClan(clanName, clanName, sturgiaCulture, banner, new Vec2(0, 0), false);

            // Set the Owl as leader and register hero properly
            newClan.SetLeader(theOwl);
            newClan.Heroes.Add(theOwl);
            theOwl.Clan = newClan;

            // Assign to Sturgia
            newClan.Kingdom = sturgiaKingdom;
            if (!sturgiaKingdom.Clans.Contains(newClan))
                sturgiaKingdom.Clans.Add(newClan);

            // Optional: give settlement ownership
            var settlement = Settlement.Find("castle_S8");
            if (settlement != null)
            {
                ChangeOwnerOfSettlementAction.ApplyByDefault(theOwl, settlement);
                InformationManager.DisplayMessage(new InformationMessage($"✅ {settlement.Name} granted to The Owl."));
            }

            InformationManager.DisplayMessage(new InformationMessage("🛡️ The Owl now leads the Clan of the Vortiaks under the banner of Sturgia."));
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


