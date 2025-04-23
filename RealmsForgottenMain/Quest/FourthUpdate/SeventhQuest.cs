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

        private const int travelObjectiveTarget = 1;
        private bool IsTravelObjectiveCompleted => travelObjectiveLog?.CurrentProgress >= travelObjectiveTarget;

        public static bool IsVortiakClanSpawned { get; set; } = false;

        private bool _hookedWitchPartyDestroyed = false;
        private Settlement witchHideout => Settlement.Find("vortiak_ruined_temple");

        private static readonly string witchCharacterId = "evil_witch";

        private Vec2 _lastPlayerPosition = Vec2.Invalid;

        private bool _dialogEventsRegistered = false;
        public int DruidInteractionStage => druidInteractionLog?.CurrentProgress ?? -1;

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
        new TroopDetail("vortiak_mounted_necromancer_lord", 1),       
        new TroopDetail("vortiak_warrior", 200),   
        new TroopDetail("vortiak_crossbowman", 250), 
        new TroopDetail("vortiak_swordsman", 300),
        new TroopDetail("cs_nelrog_bandits_chief", 50),
        new TroopDetail("hellbound_boss", 50),
        new TroopDetail("hellbound_chief", 100)    
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
            // 🔹 Check if the druid conversation should trigger
            if (druidInteractionLog != null && druidInteractionLog.CurrentProgress == 0)
            {
                if (!_druidConversationTriggered && FirstTreeSettlement != null)
                {
                    float distance = MobileParty.MainParty.Position2D.Distance(FirstTreeSettlement.GatePosition);
                    float threshold = 50f; // Distance threshold for druid conversation

                    if (distance <= threshold)
                    {
                        _druidConversationTriggered = true;

                        // Force conversation on campaign map with "elvean_first_tree_druid_quest"
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

           
            if (_shouldTriggerOwlDialogue)
            {
               
                if (_lastPlayerPosition.IsValid)
                {
                    float movedDistance = MobileParty.MainParty.Position2D.Distance(_lastPlayerPosition);

                    if (movedDistance > 20f) // ✅ Adjust distance as needed
                    {
                        _shouldTriggerOwlDialogue = false; // ✅ Prevent re-triggering
                       
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
            CheckTempleProximity();
            
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
                    "sk_dwarf_erebor_boots_med_b"
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
            // 1) Get origin settlement and culture
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

            // 2) Create Vortiak hero
            Hero vortiakHero = HeroCreator.CreateSpecialHero(
                CharacterObject.PlayerCharacter,
                originSettlement,
                null,
                null
            );
            vortiakHero.Culture = sturgiaCulture;
            vortiakHero.ChangeState(Hero.CharacterStates.Active);
            vortiakHero.SetName(new TextObject("Vortiak Lord"), new TextObject("Vortiak Lord"));

            // 3) Create clan
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

            // 4) Create party with spawn parameters
            MobileParty vortiakParty = LordPartyComponent.CreateLordParty(
                "vortiak_army",
                vortiakHero,
                MobileParty.MainParty.Position2D, // Spawn near the player
                10f, 
                originSettlement,               // Home settlement
                vortiakHero                     // Party owner
            );

            // 5) Add troops
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


            if (MobileParty.MainParty.Army != null)
            {
                vortiakParty.Army = MobileParty.MainParty.Army;
                InformationManager.DisplayMessage(new InformationMessage("Vortiak Party has joined your army!"));
            }
                      
            vortiakParty.Ai.SetMoveEscortParty(MobileParty.MainParty);

            InformationManager.DisplayMessage(new InformationMessage("Vortiak party has joined your army!"));
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

            if (_witchFinalArmy != null && party == _witchFinalArmy && priestessArmyLog?.CurrentProgress == 0)
            {
                priestessArmyLog.UpdateCurrentProgress(1);
                ShowWitchDefeatedNotification();

                // ✅ Remove the Witch troop from any PrisonRoster
                CharacterObject evilWitchTroop = CharacterObject.All.FirstOrDefault(c => c.StringId == "evil_witch");
                if (evilWitchTroop != null)
                {
                    foreach (MobileParty p in MobileParty.All)
                    {
                        if (p.PrisonRoster?.Contains(evilWitchTroop) == true)
                        {
                            int count = p.PrisonRoster.GetTroopCount(evilWitchTroop);
                            p.PrisonRoster.RemoveTroop(evilWitchTroop, count);
                        }
                    }
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

        private DialogFlow WitchFinalEncounterDialogue => DialogFlow.CreateDialogFlow("witch_final_encounter", 120)
            .NpcLine("The air crackles with doom… the Witch’s final host approaches!")
            .Condition(() => PlayerEncounter.EncounteredParty == _witchFinalArmy.Party)
            .BeginPlayerOptions()
                .PlayerLine("I will not yield!")
                    .NpcLine("Then face oblivion!")
                    .Consequence(() =>
                    {
                        // Optional: Set quest flags or start battle
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

            QuestUIManager.ShowNotification(
                "The Witch has fled! She gathers her final army — stop her before it's too late!",
                null, true, "medieval_horseman_ride"
            );

            SpawnFinalWitchArmy();
        }

        private void SpawnFinalWitchArmy()
        {
            Settlement ruinedTemple = Settlement.FindFirst(s => s.StringId == "vortiak_ruined_temple");
            if (ruinedTemple == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Ruined Temple not found.", Colors.Red));
                return;
            }

            Vec2 spawnPos = ruinedTemple.Position2D + new Vec2(10f, 10f);

            var roster = TroopRoster.CreateDummyTroopRoster();
            var troops = new List<TroopDetail>
    {
        new TroopDetail("vortiak_warrior", 250),
        new TroopDetail("vortiak_crossbowman", 300),
        new TroopDetail("cs_daimo_raiders_raider", 50),
        new TroopDetail("cs_bark_raiders_raider", 50),
          new TroopDetail("cs_nurh_raiders_raider", 50),
            new TroopDetail("cs_nurh_raiders_bandit", 50),
              new TroopDetail("cs_daimo_raiders_raider", 50),
              new TroopDetail("cs_sillok_raiders_raider", 50),
              new TroopDetail("cs_devils_bandits_chief", 50),
        new TroopDetail("evil_witch", 1) // Assuming boss is a CharacterObject
    };

            foreach (var detail in troops)
            {
                var troop = CharacterObject.Find(detail.TroopId);
                if (troop != null)
                    roster.AddToCounts(troop, detail.Quantity);
            }

            _witchFinalArmy = BanditPartyComponent.CreateBanditParty("vortiaks", Clan.BanditFactions.First(), null, true);
            _witchFinalArmy.SetCustomName(new TextObject("Vortiak Priestess Army"));
            _witchFinalArmy.InitializeMobilePartyAroundPosition(roster, TroopRoster.CreateDummyTroopRoster(), spawnPos, 5f, 5f);
            _witchFinalArmy.Aggressiveness = 100f;
            _witchFinalArmy.Ai.SetMoveEngageParty(MobileParty.MainParty);

            InformationManager.DisplayMessage(new InformationMessage("The Witch's final army has appeared!"));
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
                "In a twisted blurry shape it rose up above you, a strange light within it staring at you as flickering eyes. She is not dead,” said your friend the Owl. “Her spirit prevails.",
                ShowOwlNotification,
                true,
                "watchtheshadow"
            );
        }

        private void ShowOwlNotification()
        {
            QuestUIManager.ShowNotification(
                "From within the shadow, a dark winged creature takes shape. With incredible speed, the creature flies towards the highest mountain peak.",
                ShowFinalNotification,
                true,
                "flyingshadow"
            );
        }

        private void ShowFinalNotification()
        {
            QuestUIManager.ShowNotification(
                "As it desappears into the horizon, a sense of fulfillment lay mixed in your heart with an underlying feeling of despair. Now you celebrate, casue you have defeated this evil. But for how long?",
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

            QuestUIManager.ShowNotification(
                "You have chosen to ally with the Vortiaks! Your new objective: Destroy the Elveans.",
                null,
                true,
                "huntthewitch"
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
                        "The Elveans have been wiped out. The Vortiaks have been avenged.",
                        true, false, "Continue", null, null, null
                    ));

                    FinalizeSeventhQuestSuccess();
                }
            }
        }

        private void FinalizeSeventhQuestSuccess()
        {
            CreateNewVortiaksKingdom();

            if (_playerChoseToDestroyElveans)
            {
                // ⚫ DARK PATH
                InformationManager.ShowInquiry(new InquiryData(
                    "Dark Reign Begins",
                    "The Elveans have been wiped out. The Vortiaks rise to dominate Aeurth under your leadership. The balance is forever broken.",
                    true, false, "Continue", null, null, null
                ));

                Hero.MainHero.Clan.Renown += 350;
                InformationManager.DisplayMessage(new InformationMessage(
                    "You embrace the power of darkness. (+350 Renown)", Colors.Red));
                ApplyDarkPathConsequences();
            }
            else
            {
                // ⚪ GOOD PATH
                InformationManager.ShowInquiry(new InquiryData(
                      "Peace Restored",
                    "The Priestess has been slain, and her army lies in ruins. The Dreadking, in solemn tribute, embraced the Owl beneath his banner and granted him a kingdom and a castle to rule.\n\n" +
                    "Knowing that his bloodline is now secured brought the Dreadking a moment of peace — but also a renewed resolve to guard it with all his might. Yet the Owl, unwilling to spend his days in stillness upon a throne, chose to remain by your side as a loyal companion.\n\n" +
                    "For now, peace returns to Aeurth.",
                    true, false, "Continue", null, null, null
                ));

                Hero.MainHero.Clan.Renown += 1000;
                Clan.PlayerClan.Influence += 1000f;
                Hero.MainHero.Gold += 500000;

                InformationManager.DisplayMessage(new InformationMessage(
                    "You are celebrated as a hero of the realm! (+100 Renown, +10000 Influence, +5000 Gold)", Colors.Yellow));
            }

            // Add final completion log (optional)
            AddLog(GameTexts.FindText("rf_seventh_quest_completed_log"));

            // End quest
            CompleteQuestWithSuccess();
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

        // ------------------------------------------------------
        // UTILITIES: TROOPS & ARMOR
        // ------------------------------------------------------
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
            var sturgiaCulture = MBObjectManager.Instance.GetObjectTypeList<CultureObject>()
                .FirstOrDefault(c => c.StringId == "sturgia");

            if (sturgiaCulture == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Could not find Sturgian culture."));
                return;
            }

            Hero theOwl = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == "rf_the_owl");
            if (theOwl == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Could not find 'rf_the_owl'."));
                return;
            }

            // 🔥 Detach The Owl from existing clan & kingdom
            if (theOwl.Clan != null)
            {
                if (theOwl.Clan.Heroes.Contains(theOwl))
                {
                    theOwl.Clan.Heroes.Remove(theOwl);
                }

                if (theOwl.Clan.Kingdom != null && theOwl.Clan.Kingdom.Clans.Contains(theOwl.Clan))
                {
                    theOwl.Clan.Kingdom.Clans.Remove(theOwl.Clan);
                }

                theOwl.Clan = null; // Hard detach
            }

            theOwl.SetName(new TextObject("The Owl"), new TextObject("The Owl"));

            uint primaryColor = 0xff0B0C11;
            uint secondaryColor = 0xffCEDAE7;
            string bannerKey = "19.35.116.1836.1836.768.788.1.0.-30.347.143.116.240.240.768.788.1.1.0.457.143.116.204.204.581.566.1.1.0.213.144.116.200.200.948.1001.1.0.0";
            Banner banner = new Banner(bannerKey, primaryColor, secondaryColor);

            Clan newClan = MBObjectManager.Instance.CreateObject<Clan>("clan_newvortiaks");
            TextObject clanName = new TextObject("Clan of the Vortiaks");
            newClan.InitializeClan(clanName, clanName, sturgiaCulture, banner, new Vec2(0, 0), false);
            newClan.SetLeader(theOwl);

            Settlement homeland = Settlement.Find("castle_EN1");
            if (homeland == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Could not find homeland settlement 'castle_EN1'."));
                return;
            }

            Kingdom newKingdom = MBObjectManager.Instance.CreateObject<Kingdom>("newvortiaks");
            TextObject kingdomName = new TextObject("Kingdom of the Vortiaks");
            newKingdom.InitializeKingdom(
                kingdomName,
                kingdomName,
                sturgiaCulture,
                banner,
                primaryColor,
                secondaryColor,
                homeland,
                new TextObject("High Chieftain"),
                new TextObject("High Chieftain"),
                new TextObject("Vortiaks"));

            // ✅ Register kingdom
            Campaign.Current.Kingdoms.Add(newKingdom);

            newKingdom.RulingClan = newClan;
            newClan.Kingdom = newKingdom;

            // ✅ Assign settlements
            List<Settlement> settlements = new List<Settlement>
    {
        Settlement.Find("castle_S8")
    };

            foreach (var settlement in settlements)
            {
                if (settlement != null)
                {
                    // Properly transfers ownership (sets OwnerClan internally)
                    ChangeOwnerOfSettlementAction.ApplyByDefault(theOwl, settlement);

                    InformationManager.DisplayMessage(new InformationMessage($"✅ {settlement.Name} granted to The Owl."));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ One of the settlements was null."));
                }
            }

            InformationManager.DisplayMessage(new InformationMessage("🏰 The Vortiaks rise again under The Owl's banner!"));
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

