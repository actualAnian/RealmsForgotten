using RealmsForgotten.AiMade.RF_Diplomacy;
using RealmsForgotten.Quest.UI;
using RealmsForgotten.RFMissionLogic;
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

        [SaveableField(14)]
        private JournalLog? priestessArmyLog;

        [SaveableField(15)]
        private MobileParty? _witchFinalArmy;

        [SaveableField(16)]
        private JournalLog? vortiakLairLog;

        // ======================= CAMPOS REMOVIDOS =======================
        // [SaveableField(17)] private bool deformedSpawningEnabled -> MOVIDO PARA O BEHAVIOR
        // [SaveableField(18)] private CampaignTime _nextDeformedSpawnTime -> MOVIDO PARA O BEHAVIOR
        // [SaveableField(19)] private int _deformedPartySpawnCount -> MOVIDO PARA O BEHAVIOR
        // [SaveableField(20)] private bool _eighthQuestStarted -> MOVIDO PARA O BEHAVIOR
        // [SaveableField(23)] private JournalLog eighthPriestessLog -> REMOVIDO (não é mais necessário)
        // ================================================================

        [SaveableField(21)]
        private bool _witchConversationCompleted = false;

        [SaveableField(24)]
        private bool _priestessConversationTriggered;

        [SaveableField(25)]
        private bool _owlDialogueAttempted;

        private const int travelObjectiveTarget = 1;
        private bool IsTravelObjectiveCompleted => travelObjectiveLog?.CurrentProgress >= travelObjectiveTarget;

        public static bool IsVortiakClanSpawned { get; set; } = false;

        private Settlement witchHideout => Settlement.Find("vortiak_ruined_temple");

        private static readonly string witchCharacterId = "evil_witch";

        private CampaignVec2 _lastPlayerPosition = CampaignVec2.Invalid;

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
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnLeaveSettlement);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStart);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty
                || settlement.StringId != "vortiak_ruined_temple"
                || interceptorDefeatLog?.CurrentProgress != 2
                || bossBattleLog?.CurrentProgress != 0) return;
            Campaign.Current.SaveHandler.SaveAs("rfAutosave");
        }

        private void OnMissionStart(IMission Imission)
        {
            if (Imission is not Mission mission ||
                Settlement.CurrentSettlement == null ||
                mission.Scene?.GetName() != "evil_witch_fight") return;
            if (interceptorDefeatLog?.CurrentProgress != 2 || bossBattleLog?.CurrentProgress != 0) return;
            mission.AddMissionBehavior(new WitchFightSceneMissionLogic());
            mission.AddMissionBehavior(new RFMissionSoundManager());
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            RegisterEvents();
            _shouldShowPopups = true;
            _druidConversationTriggered = false;
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
            // A lógica de interação com a Sacerdotisa para a OITAVA quest foi removida
            // pois o DeformedSpawningBehavior cuidará disso.

            if (druidInteractionLog != null && druidInteractionLog.CurrentProgress == 0)
            {
                if (!_druidConversationTriggered && FirstTreeSettlement != null)
                {
                    float distance = MobileParty.MainParty.Position.Distance(FirstTreeSettlement.GatePosition);
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

            if (_shouldTriggerOwlDialogue)
            {
                if (_lastPlayerPosition.IsValid())
                {
                    float movedDistance = MobileParty.MainParty.Position.Distance(_lastPlayerPosition);
                    if (movedDistance > 20f)
                    {
                        _shouldTriggerOwlDialogue = false;
                        return;
                    }
                }
                _lastPlayerPosition = MobileParty.MainParty.Position;
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
            CheckElveanDestructionProgress();
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent)
                return;

            if (MobileParty.MainParty.PrisonRoster != null)
            {
                RemoveEvilWitchFromPrisoners();
            }
        }

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
                    OnPopupClosed,
                    null
                )
            );
        }

        private void OnPopupClosed()
        {
            druidInteractionLog = AddLog(GameTexts.FindText("rf_seventh_quest_druid_objective"));
            druidInteractionLog.UpdateCurrentProgress(0);
        }

        public static void OnWitchDefeated()
        {
            QuestBase? quest = Campaign.Current.QuestManager.Quests.FirstOrDefault(q => q.StringId == "rf_seventh_quest");
            if (quest == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Seventh Quest not found!"));
                return;
            }
            SeventhQuest seventhQuest = (SeventhQuest)quest;
            seventhQuest.bossBattleLog?.UpdateCurrentProgress(1);
        }

        private void OnLeaveSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (bossBattleLog?.CurrentProgress == 1 && settlement == witchHideout)
            {
                PlayerEncounter.Finish(true);
                bossBattleLog.UpdateCurrentProgress(2);
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(CharacterObject.Find(witchCharacterId)));
            }
        }

        public void SpawnAllWitchLords()
        {
            foreach (WitchLord lord in WitchLords.Values)
            {
                var party = CreateWitchLordParty(lord.CharacterId, lord.ClanId, lord.SpawnSettlement, lord.TroopDetails);
                _witchFinalArmy = party;
            }

            InformationManager.DisplayMessage(
                new InformationMessage("The Vortiak Witch was spotted around Nippura!", Colors.Magenta));
        }

        public MobileParty? CreateWitchLordParty(string witchLordCharacterId, string clanId, Settlement spawnSettlement, List<TroopDetail> troopDetails)
        {
            // ... (Este método permanece inalterado)
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
                Settlement fixedSpawnSettlement = Settlement.Find("town_S2");
                CampaignVec2 spawnPosition = fixedSpawnSettlement?.Position ?? spawnSettlement.Position;

                PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
                MobileParty party = BanditPartyComponent.CreateBanditParty($"vortiak_witch_party_{witchLordCharacterId}", clan, null, true, looterTemplate, spawnPosition); //@TODO

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
                    DestroyPartyAction.Apply(null, party);
                    return null;
                }

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
                 .OrderBy(p => p.Position.DistanceSquared(party.Position))
                   .FirstOrDefault();

                if (target != null)
                {
                    party.SetMoveEngageParty(target, MobileParty.NavigationType.All);
                    InformationManager.DisplayMessage(new InformationMessage($"Witch Lord party is attacking {target.Name}."));
                }
                else
                {
                    party.SetMovePatrolAroundPoint(spawnSettlement.Position, MobileParty.NavigationType.All); // fallback
                }

                party.ActualClan = clan;
                party.Party.SetCustomName(new TextObject($"Vortiak Coven ({leaderCharacter.Name})"));
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

        private DialogFlow DruidInteractionDialog => DialogFlow.CreateDialogFlow("start", 125)
            // ... (Este método permanece inalterado)
            .PlayerLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_1"))
            .Condition(() =>
                druidInteractionLog != null
                && druidInteractionLog.CurrentProgress == 0
                && CharacterObject.OneToOneConversationCharacter?.StringId == "elvean_first_tree_druid_quest"
            )
            .NpcLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_seventh_quest_druid_dialog_6"))
            .Consequence(() =>
            {
                druidInteractionLog.UpdateCurrentProgress(1);
                GivePlayerTroops(new List<(string troopId, int troopCount)>
                {
                    ("first_tree_ranger", 100)
                });
                dwarfKingLog = AddLog(GameTexts.FindText("rf_seventh_quest_dwarf_king_objective"));
                dwarfKingLog.UpdateCurrentProgress(0);
            })
           .CloseDialog();

        private DialogFlow DwarfKingDialog => DialogFlow.CreateDialogFlow("start", 125)
            // ... (Este método permanece inalterado)
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
                GivePlayerArmorSet(new List<string>
                {
                    "sk_dwarf_erebor_helmet_plate_elite_a",
                    "sk_dwarf_erebor_chest_plate_elite_a",
                    "sk_dwarf_erebor_bracers_elite_a",
                    "sk_dwarf_erebor_boots_med_b",
                    "sk_dwarf_erebor_pauldron_plate_elite_a"
                });
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

        private void CheckTempleProximity()
        {
            // ... (Este método permanece inalterado)
            if (dwarfKingLog == null || dwarfKingLog.CurrentProgress < 1)
                return;

            var sacredGrove = Settlement.FindFirst(s => s.StringId == "vortiak_ruined_temple");
            if (sacredGrove != null)
            {
                float distance = MobileParty.MainParty.Position.Distance(sacredGrove.Position);
                float proximityThreshold = 50f;
                if (distance <= proximityThreshold)
                {
                    if (travelObjectiveLog != null && travelObjectiveLog.CurrentProgress < travelObjectiveTarget)
                    {
                        travelObjectiveLog.UpdateCurrentProgress(travelObjectiveTarget);
                        InformationManager.DisplayMessage(new InformationMessage("You are nearing the temple."));
                    }
                    if (!hasBeenIntercepted)
                    {
                        CreateInterceptorParty("rf_interceptor_default", sacredGrove);
                        hasBeenIntercepted = true;
                    }
                }
            }
        }

        private void CreateInterceptorParty(string key, Settlement nearSettlement)
        {
            // ... (Este método permanece inalterado)
            interceptorDefeatLog?.UpdateCurrentProgress(0);
            try
            {
                if (!InterceptorArmies.TryGetValue(key, out var troopDetails))
                    throw new Exception("Interceptor configuration not found.");

                var enemyClan = Clan.All.FirstOrDefault(c => c.StringId == "vortiaks")
                                    ?? Clan.All.First();

                PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
                MobileParty interceptorParty = BanditPartyComponent.CreateBanditParty(enemyClan.StringId, enemyClan, null, true, looterTemplate,nearSettlement.Position); //@TODO

                if (interceptorParty == null)
                    throw new Exception("Failed to create interceptor party.");

                interceptorParty.Party.SetCustomName(new TextObject("Vortiak Army"));

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

                interceptorParty.InitializeMobilePartyAroundPosition(
                    troopRoster,
                    TroopRoster.CreateDummyTroopRoster(),
                    nearSettlement.Position,
                    10f,
                    10f
                );
                interceptorParty.Aggressiveness = 15f;
                interceptorParty.SetMoveEngageParty(MobileParty.MainParty, MobileParty.NavigationType.All);

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
            // ... (Este método permanece inalterado)
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

                MobileParty.MainParty.MemberRoster.AddToCounts(troop, detail.Quantity, false);
                InformationManager.DisplayMessage(new InformationMessage($"Added {detail.Quantity} {troop.Name} to your party (non-counted)."));
            }
        }

        private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyer)
        {
            // ... (Este método permanece inalterado)
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
                ShowWitchDefeatedNotification();
                if (priestessArmyLog != null && priestessArmyLog.CurrentProgress == 0)
                {
                    priestessArmyLog.UpdateCurrentProgress(1);
                }
            }
        }

        private DialogFlow InterceptorEncounterDialogue => DialogFlow.CreateDialogFlow("interceptor_encounter", 120)
            // ... (Este método permanece inalterado)
            .NpcLine("Steel your hearts! The Vortiaks spring their ambush!")
            .Condition(() => PlayerEncounter.EncounteredParty == _interceptorParty.Party)
            .BeginPlayerOptions()
                .PlayerOption("Stand and fight!")
                    .NpcLine("So be it—blood will flow!")
                     .Consequence(() =>
                     {
                     })
            .EndPlayerOptions()
            .CloseDialog();

        private void StartNecromancerDialogue()
        {
            // ... (Este método permanece inalterado)
            CharacterObject necroChar = CharacterObject.Find("vortiak_mounted_necromancer_lord");
            if (necroChar != null)
            {
                Campaign.Current.ConversationManager.AddDialogFlow(NecromancerDialogue, this);
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
            // ... (Este método permanece inalterado)
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
            // ... (Este método permanece inalterado)
            .NpcLine(GameTexts.FindText("rf_seventh_quest_owl_dialog_1")).Condition(() => owlRebellionlLog?.CurrentProgress == 0 &&
                Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "rf_the_owl")
            .BeginPlayerOptions()
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
            // ... (Este método permanece inalterado)
            Hero owlHero = TheOwl;

            if (owlHero == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("[ERROR] The Owl character is missing or not in the player's party!", Colors.Red));
                return;
            }

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
         .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_1"))
         .Condition(() => bossBattleLog?.CurrentProgress == 2 && CharacterObject.OneToOneConversationCharacter?.StringId == "evil_witch")
         .PlayerLine(GameTexts.FindText("rf_seventh_quest_witch_final_2"))
         .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_3"))
         .PlayerLine(GameTexts.FindText("rf_seventh_quest_witch_final_4"))
         .NpcLine(GameTexts.FindText("rf_seventh_quest_witch_final_5"))
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
                "postwitchfight"
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
                            SpawnDarkElveanKingdom();
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

        // ======================= MÉTODO PRINCIPAL AJUSTADO =======================
        private void FinalizeSeventhQuestSuccess()
        {
            CreateNewVortiaksKingdom();

            // ✅ INICIA A LÓGICA DE SPAWN NO NOVO BEHAVIOR PERSISTENTE
            RealmsForgotten.Quest.FourthUpdate.DeformedSpawningBehavior.Instance?.BeginSpawning();

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

            // ✅ REMOVE A LIGAÇÃO COM O ANTIGO MÉTODO DE TICK PÓS-QUEST
            // O listener de DailyTick da quest será removido automaticamente ao completá-la.
        }

        // ======================= MÉTODOS REMOVIDOS =======================
        // DeformedPostQuestTick() -> REMOVIDO
        // ApplyDarkPathConsequences() -> REMOVIDO (mas você pode querer movê-lo para o Behavior se for parte do resultado do spawn)
        // SpawnDeformedParties() -> REMOVIDO
        // SpawnDeformedParty() -> REMOVIDO
        // ShowEighthQuestPrompt() -> REMOVIDO
        // PriestessEighthQuestDialog -> REMOVIDO
        // StartEighthQuest() -> REMOVIDO
        // SyncData() -> REMOVIDO (não salva mais dados de spawn)
        // ================================================================

        private void ApplyDarkPathConsequences()
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                string cultureId = kingdom.Culture.StringId;

                if (cultureId != "sturgia" && cultureId != "aserai" && cultureId != "urkhai")
                {
                    if (kingdom.Leader != null)
                    {
                        Hero.MainHero.SetPersonalRelation(kingdom.Leader, -200);
                    }

                    if (!FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, kingdom))
                    {
                        FactionManager.DeclareWar(Hero.MainHero.MapFaction, kingdom);
                    }
                }
            }

            InformationManager.DisplayMessage(new InformationMessage(
                "You have declared war on the realms of Aeurth (except for Sturgia, Aserai, and Urkhai). Relations have worsened.", Colors.Red));

        }

        private void CleanupDevilsAndNelrogParties()
        {
            var partiesToRemove = MobileParty.All
                .Where(p =>
                    p != null &&
                    p.IsActive &&
                    !p.IsDisbanding &&
                    p.MapEvent == null &&
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
                    DestroyPartyAction.Apply(null, party);
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
            // ... (Este método permanece inalterado)
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
            TextObject clanName = new("Clan of the Vortiaks");
            newClan.Culture = sturgiaCulture;
            newClan.Banner = banner;
            newClan.SetLeader(theOwl);
            newClan.Heroes.Add(theOwl);
            theOwl.Clan = newClan;

            newClan.Kingdom = sturgiaKingdom;
            if (!sturgiaKingdom.Clans.Contains(newClan))
                sturgiaKingdom.Clans.Add(newClan);

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
            // ... (Este método permanece inalterado)
            try
            {
                CharacterObject darkElveanTemplate = CharacterObject.Find("dark_elvean_lord");
                if (darkElveanTemplate == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Could not find 'dark_elvean_lord' template.", Colors.Red));
                    return;
                }

                Hero darkElveanHero = HeroCreator.CreateSpecialHero(
                    darkElveanTemplate,
                    Settlement.FindFirst(x => x.IsTown),
                    null, null,
                    30
                );

                darkElveanHero.ChangeState(Hero.CharacterStates.Active);
                darkElveanHero.SetName(new TextObject("Lord Varakar"), new TextObject("Lord Varakar of the Dark Elveans"));

                string customBannerKey = "35.116.100.1140.1445.779.774.1.0.-91.434.121.116.240.240.920.955.1.0.0.407.121.116.248.248.630.578.1.0.0";
                uint primaryColor = 0xff332c4d;
                uint secondaryColor = 0xffFDE217;
                Banner customBanner = new(customBannerKey, primaryColor, secondaryColor);

                Clan darkElveanClan = Clan.CreateClan("clan_dark_elveans");
                darkElveanClan.ChangeClanName(new TextObject("Dark Elveans"), new TextObject("Dark Elveans"));
                darkElveanClan.Culture = darkElveanHero.Culture;
                darkElveanClan.Banner = customBanner;
                darkElveanClan.SetLeader(darkElveanHero);
                darkElveanHero.Clan = darkElveanClan;

                for (int i = 0; i < 2; i++)
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
            Campaign.Current.ConversationManager.AddDialogFlow(DruidInteractionDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(DwarfKingDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(NecromancerDialogue, this);
            Campaign.Current.ConversationManager.AddDialogFlow(OwlRebelionDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(PostBattleWitchDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(InterceptorEncounterDialogue, this);
            Campaign.Current.ConversationManager.AddDialogFlow(WitchFinalEncounterDialogue, this);

            // ✅ REMOVIDO: O PriestessEighthQuestDialog não pertence mais a esta quest.
            // Campaign.Current.ConversationManager.AddDialogFlow(PriestessEighthQuestDialog, this);
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