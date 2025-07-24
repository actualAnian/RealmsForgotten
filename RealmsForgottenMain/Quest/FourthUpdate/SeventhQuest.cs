using RealmsForgotten.Quest.UI;
using RealmsForgotten.RFMissionLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;



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

        private const int travelObjectiveTarget = 1;
        private bool IsTravelObjectiveCompleted => travelObjectiveLog?.CurrentProgress >= travelObjectiveTarget;

        public static bool IsVortiakClanSpawned { get; set; } = false;

             
        private Vec2 _lastPlayerPosition = Vec2.Invalid;

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
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);

            // behavior tree for the vortiak witch
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStart);
        }

        private void OnMissionStart(IMission Imission)
        {
            if (Imission is not Mission mission || 
                Settlement.CurrentSettlement == null ||
                mission.Scene?.GetName() != "evil_witch_fight") return;
            if (interceptorDefeatLog?.CurrentProgress != 2 || bossBattleLog?.CurrentProgress != 0) return;
            // add special behaviors for the mission
            mission.AddMissionBehavior(new WitchFightSceneMissionLogic());
            mission.AddMissionBehavior(new RFMissionSoundManager());
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
                    "dwarf_lord_gen_helmet_a",
                    "dwarf_lord_gen_armor_a",
                    "dwarf_lord_gen_gautlets_a",
                    "dwarf_lord_gen_boots_a"
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
                    "demonic_horde"
                );
            })
            .CloseDialog();

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
        }

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
                "Your next objective is to defeat the priest of darkness. Seek out the Vortiak Priestess!",
                null,
                true,
                "demon_fight_scaled"
            );
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool someFlag)
        {
            if (victim != null && victim.StringId == "vortiak_priest_boss")
            {
                OnBossDefeated();
            }
        }

        private void OnBossDefeated()
        {
            if (bossBattleLog != null && bossBattleLog.CurrentProgress < 1)
            {
                bossBattleLog.UpdateCurrentProgress(1);
            }
            InformationManager.ShowInquiry(new InquiryData(
                "Final Trial Conquered",
                "You have defeated the priest of darkness! The land is freed from his blight.",
                true, false, "Continue", null, null, null
            ));
            CompleteQuestWithSuccess();
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
                "demon_hordes_marching"
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
                        "The Elveans have been wiped out. The Vortiaks now rule unchallenged.",
                        true, false, "Continue", null, null, null
                    ));

                    CompleteQuestWithSuccess();
                }
            }
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

        protected override void SetDialogs()
        {
            // Force-add our druid, dwarf king, necromancer flows. 
            // They no longer rely on a settlement condition.
            Campaign.Current.ConversationManager.AddDialogFlow(DruidInteractionDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(DwarfKingDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(NecromancerDialogue, this);
            Campaign.Current.ConversationManager.AddDialogFlow(OwlRebelionDialog, this);
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
