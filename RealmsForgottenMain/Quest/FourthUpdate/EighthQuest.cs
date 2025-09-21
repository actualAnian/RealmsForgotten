using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
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
using RealmsForgotten.Quest.MissionBehaviors;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class EighthQuest : QuestBase
    {
        // ======================= CAMPOS DE ESTADO DA QUEST =======================
        [SaveableField(100)] private JournalLog _talkToOwlLog;
        [SaveableField(101)] private bool _pendingStartSacredObjectDialogue = false;

        [SaveableField(0)] private JournalLog _meetPriestessLog;
        [SaveableField(1)] internal JournalLog _findSacredObjectLog;
        [SaveableField(2)] internal JournalLog _returnSacredObjectLog;
        [SaveableField(3)] private CampaignTime _leftHideoutTime = CampaignTime.Never;
        [SaveableField(4)] private bool _hasTriggeredPriestessReturnDialogue = false;
        [SaveableField(5)] private bool _owlSacredObjectDialogueStarted = false;
        [SaveableField(6)] private bool _ambushTriggered = false;
        [SaveableField(7)] private CampaignTime _ambushCheckStartTime = CampaignTime.Never;
        [SaveableField(8)] private bool _shouldTriggerPostAmbushOwlDialogue = false;

        [SaveableField(9)] private JournalLog _investigateMagesLog;
        [SaveableField(10)] private JournalLog _mageSiteLog;
        [SaveableField(11)] private JournalLog _orcTrailLog;
        [SaveableField(12)] private bool _mageInquiryTriggered = false;
        [SaveableField(13)] private bool _mageSiteMarked = false;
        [SaveableField(14)] private bool _mageBossDefeated = false;
        [SaveableField(15)] private bool _survivorPendingAfterExit = false;
        [SaveableField(16)] private bool _mageSurvivorDialogueDone = false;
        [SaveableField(17)] private bool _ninthQuestStarted = false;

        [NonSerialized] private bool _eventsRewiredAfterLoad;

        // ======================= IDs E REFERÊNCIAS =======================
        private const string RF_MAGE_SITE = "mage_hideout";
        private const string RF_MAGE_BOSS_AGENT = "rf_mage_boss_agent";
        private const string RF_MAGE_SURVIVOR_HERO = "rf_mage_survivor";
        private const string FIRST_TREE_TOWN = "town_FirstTree";
        private const string OWL_HERO_ID = "rf_the_owl";
        private const string PRIESTESS_CHAR_ID = "elvean_first_tree_druid_quest";
        private const string SACRED_OBJECT_ID = "sacred_object";

        private Hero TheOwl => Hero.FindFirst(h => h.StringId == OWL_HERO_ID);
        private Settlement QuestHideoutSettlement => Settlement.Find("hideout_mountain_13");
        private static Settlement FirstTreeSettlement => Settlement.Find(FIRST_TREE_TOWN);
        private static Settlement MageInvestigationSpot => Settlement.Find("castle_EN3");
        private static Settlement MageSite => Settlement.Find(RF_MAGE_SITE);

        public EighthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold) { }

        // ======================= CICLO DE VIDA DA QUEST =======================
        protected override void OnStartQuest()
        {
            SetDialogs();
            RegisterEvents();

            // ✅ CORRIGIDO: O método AddLog foi chamado corretamente.
            _talkToOwlLog = AddLog(new TextObject("The recent reports of deformed creatures have caused concern. The Owl wishes to speak with you about the matter. Find him in your party to discuss the next steps."));

            if (TheOwl != null) AddTrackedObject(TheOwl);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            ReinforceQuestLogs();
        }

        public void PostLoadRewire()
        {
            if (_eventsRewiredAfterLoad) return;
            _eventsRewiredAfterLoad = true;

            SetDialogs();
            RegisterEvents();
            ReinforceQuestLogs();

            if (_findSacredObjectLog != null)
            {
                InitializeEighthQuestHideout();
            }
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnLeaveSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnHideoutMissionEnded);
        }

        protected override void OnTimedOut()
        {
            CompleteQuestWithFail();
        }

        protected override void OnFinalize()
        {
            if (QuestHideoutSettlement != null) RemoveTrackedObject(QuestHideoutSettlement);
            if (MageSite != null) RemoveTrackedObject(MageSite);
            if (TheOwl != null) RemoveTrackedObject(TheOwl);
        }

        public override TextObject Title => new TextObject("Eighth Quest: Call of the First Tree");
        public override bool IsSpecialQuest => true;

        // ✅ CORRIGIDO: A propriedade obrigatória foi adicionada novamente.
        public override bool IsRemainingTimeHidden => true;

        // ======================= MANIPULADORES DE EVENTOS =======================

        private void OnTick(float dt)
        {
            if (_pendingStartSacredObjectDialogue)
            {
                _pendingStartSacredObjectDialogue = false;
                if (!_owlSacredObjectDialogueStarted)
                {
                    _owlSacredObjectDialogueStarted = true;
                    StartSacredObjectDialogue();
                }
            }

            if (_shouldTriggerPostAmbushOwlDialogue)
            {
                _shouldTriggerPostAmbushOwlDialogue = false;
                if (TheOwl != null)
                {
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(TheOwl.CharacterObject, PartyBase.MainParty)
                    );
                }
            }
        }

        private void OnHideoutMissionEnded(IMission iMission)
        {
            if (Settlement.CurrentSettlement?.IsHideout != true || Settlement.CurrentSettlement.StringId != "hideout_mountain_13")
                return;

            if (_owlSacredObjectDialogueStarted || _findSacredObjectLog?.CurrentProgress != 0)
                return;

            _pendingStartSacredObjectDialogue = true;
        }

        private void OnLeaveSettlement(MobileParty party, Settlement settlement)
        {
            if (!party.IsMainParty) return;

            if (settlement?.Hideout != null && !settlement.Hideout.IsInfested &&
                _findSacredObjectLog?.CurrentProgress == 0 && !_owlSacredObjectDialogueStarted)
            {
                _owlSacredObjectDialogueStarted = true;
                InformationManager.DisplayMessage(new InformationMessage("✅ Hideout cleared! Triggering Owl conversation (fallback OnLeaveSettlement)."));
                StartSacredObjectDialogue();
                return;
            }

            if (settlement.StringId == RF_MAGE_SITE && _mageBossDefeated && !_mageSurvivorDialogueDone)
            {
                if (_survivorPendingAfterExit)
                {
                    _survivorPendingAfterExit = false;
                    TriggerMageSurvivorConversation();
                }
            }
        }

        protected override void HourlyTick()
        {
            OnHourlyTick_MageInvestigation();
        }

        private void OnDailyTick()
        {
            CheckPriestessProximity();

            if (!_ambushTriggered && _ambushCheckStartTime != CampaignTime.Never)
            {
                if ((CampaignTime.Now - _ambushCheckStartTime).ToDays >= 1f)
                {
                    _ambushTriggered = true;
                    _ambushCheckStartTime = CampaignTime.Never;
                    TriggerAmbush();
                }
            }
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent) return;

            MobileParty ambushParty = null;
            var attackerParty = mapEvent.AttackerSide.LeaderParty?.MobileParty;
            var defenderParty = mapEvent.DefenderSide.LeaderParty?.MobileParty;

            if (attackerParty != null && attackerParty != MobileParty.MainParty &&
                attackerParty.StringId.StartsWith("rf_deformed_ambush_"))
            {
                ambushParty = attackerParty;
            }
            else if (defenderParty != null && defenderParty != MobileParty.MainParty &&
                     defenderParty.StringId.StartsWith("rf_deformed_ambush_"))
            {
                ambushParty = defenderParty;
            }

            if (ambushParty != null)
            {
                _ambushTriggered = true;
                _ambushCheckStartTime = CampaignTime.Never;
                ForceRemoveSacredObject();
                _returnSacredObjectLog?.UpdateCurrentProgress(1);
                _shouldTriggerPostAmbushOwlDialogue = true;
                AddDiscreteLog(
                    new TextObject("The Vessel Was Taken"),
                    new TextObject("You were ambushed and the vessel is gone. Speak with the Owl, then return to the Priestess."),
                    0, 1
                );
            }
        }

        private void OnMissionStarted(IMission imission)
        {
            if (Settlement.CurrentSettlement != null &&
                Settlement.CurrentSettlement.StringId == RF_MAGE_SITE &&
                !_mageBossDefeated)
            {
                if (imission is Mission mission)
                {
                    mission.AddMissionBehavior(new RecordDamageMissionLogic((victim, attacker, damage) =>
                    {
                        if (victim?.Character != null && victim.Character.StringId == RF_MAGE_BOSS_AGENT && damage >= victim.Health)
                        {
                            _mageBossDefeated = true;
                            if (MobileParty.MainParty.CurrentSettlement?.StringId == RF_MAGE_SITE)
                            {
                                _survivorPendingAfterExit = true;
                            }
                            _mageSiteLog?.UpdateCurrentProgress(1);
                            if (MageSite != null) RemoveTrackedObject(MageSite);
                        }
                    }));
                }
            }
        }

        // ======================= LÓGICA DA QUEST =======================

        private void CheckPriestessProximity()
        {
            if (_hasTriggeredPriestessReturnDialogue || FirstTreeSettlement == null ||
                _meetPriestessLog == null || _meetPriestessLog.CurrentProgress != 1)
                return;

            float distance = MobileParty.MainParty.Position.Distance(FirstTreeSettlement.GatePosition);
            if (distance <= 50f)
            {
                var priestess = CharacterObject.Find(PRIESTESS_CHAR_ID);
                if (priestess != null)
                {
                    _hasTriggeredPriestessReturnDialogue = true;
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(priestess)
                    );
                }
            }
        }

        private void TriggerAmbush()
        {
            try
            {
                Clan deformedClan = Clan.FindFirst(c => c.StringId == "deformed_villagers");
                if (deformedClan == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Deformed clan not found."));
                    return;
                }

                var troopPool = new Dictionary<string, int>
                {
                    { "deformed_villager_bandit", 30 }, { "deformed_villager_raider", 15 },
                    { "deformed_villager_chief", 5 }, { "deformed_villager_boss", 1 }
                };

                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                foreach (var kv in troopPool)
                {
                    var character = CharacterObject.Find(kv.Key);
                    if (character != null) troopRoster.AddToCounts(character, kv.Value);
                }

                string uniqueId = $"rf_deformed_ambush_{MBRandom.RandomInt(10000)}";
                MobileParty ambushParty = BanditPartyComponent.CreateBanditParty(uniqueId, deformedClan, null, true);
                if (ambushParty == null) return;

                ambushParty.InitializeMobilePartyAroundPosition(troopRoster, TroopRoster.CreateDummyTroopRoster(), MobileParty.MainParty.Position, 0f, 0f);
                ambushParty.Aggressiveness = 100f;
                ambushParty.SetMoveEngageParty(MobileParty.MainParty, MobileParty.NavigationType.Default);
                ambushParty.Party.SetCustomName(new TextObject("Deformed Ambushers"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Ambush spawn error: {ex.Message}"));
            }
        }

        private void OnHourlyTick_MageInvestigation()
        {
            if (_investigateMagesLog == null || _mageInquiryTriggered || MageInvestigationSpot == null)
                return;

            float distance = MobileParty.MainParty.Position.Distance(MageInvestigationSpot.GatePosition);
            if (distance <= 50f)
            {
                _mageInquiryTriggered = true;
                InformationManager.ShowInquiry(new InquiryData(
                    "Strange Tracks", "Your scouts notice drag marks and a faint magical residue leading into the mountains.",
                    true, false, "Investigate", null,
                    () =>
                    {
                        _mageSiteLog ??= AddDiscreteLog(new TextObject("Mage Site"), new TextObject("Follow the tracks and investigate the suspicious site where the mages are hiding."), 0, 1);
                        if (MageSite != null && !_mageSiteMarked)
                        {
                            AddTrackedObject(MageSite);
                            _mageSiteMarked = true;
                        }
                    }, null));
            }
        }

        private void TriggerMageSurvivorConversation()
        {
            CharacterObject survivorChar = CharacterObject.Find(RF_MAGE_SURVIVOR_HERO);
            if (survivorChar != null)
            {
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(survivorChar, PartyBase.MainParty)
                );
            }
            else
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Survivor", "They forced us to drink foul liquids that numbed and transformed some... Many were taken to the lands of the Urkhai.",
                    true, false, "Understood", null,
                    () =>
                    {
                        _orcTrailLog ??= AddDiscreteLog(new TextObject("Trail to the Orcs"), new TextObject("The survivor revealed that captives were taken into Urkhai territory. Travel there to continue your investigation."), 0, 1);
                        _mageSurvivorDialogueDone = true;
                        StartNinthQuest();
                    }, null));
            }
        }

        // ======================= DIÁLOGOS =======================
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(IntroductoryOwlDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(SacredObjectDialogue(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessReturnDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessFinalDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(MageSurvivorDialog(), this);
        }

        private DialogFlow IntroductoryOwlDialog()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .NpcLine(new TextObject("My friend, we just received a messenger from the First Tree Priestess. She's concerned about reports of strange mutations attacking villagers."))
                .Condition(() => Hero.OneToOneConversationHero == TheOwl && _talkToOwlLog != null && _talkToOwlLog.CurrentProgress == 0)
                .PlayerLine(new TextObject("Mutations? We've already faced zombies and demons. What else could there possibly be?"))
                .NpcLine(new TextObject("She wants us to find out. And to be honest... that worries me."))
                .PlayerLine(new TextObject("Let’s not jump to conclusions just yet. We haven’t even seen them. Did she mention where we could find a trail?"))
                .NpcLine(new TextObject("The message says the latest sightings occurred near the Tremerid Kingdom. I've marked a hideout on your map where the activity seems concentrated."))
                .PlayerLine(new TextObject("Then the Mages... could they be involved? Let's investigate."))
                .Consequence(() =>
                {
                    _talkToOwlLog.UpdateCurrentProgress(1);
                    if (TheOwl != null) RemoveTrackedObject(TheOwl);

                    _findSacredObjectLog = AddDiscreteLog(
                        new TextObject("Investigate the Hideout"),
                        new TextObject("Investigate the rumours and search for clues at the marked hideout."),
                        0, 1
                    );

                    InitializeEighthQuestHideout();
                })
                .CloseDialog();
        }

        private DialogFlow SacredObjectDialogue()
        {
            return DialogFlow.CreateDialogFlow("start", 125)
                .PlayerLine(new TextObject("What the hell were those things? They looked human... but twisted. And I found this among the bodies."))
                .Condition(() => Hero.OneToOneConversationHero?.StringId == OWL_HERO_ID && _findSacredObjectLog?.CurrentProgress == 0)
                .NpcLine(new TextObject("It looks like some kind of vessel."))
                .PlayerLine(new TextObject("The Priestess needs to see this. Perhaps it's the clue we were looking for."))
                .Consequence(() =>
                {
                    CleanupEighthQuestHideout();
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(SACRED_OBJECT_ID);
                    if (item != null)
                    {
                        MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
                        InformationManager.DisplayMessage(new InformationMessage("✅ Sacred Object added to your inventory."));
                        _findSacredObjectLog?.UpdateCurrentProgress(1);
                        _returnSacredObjectLog = AddDiscreteLog(new TextObject("Return the Sacred Object"), new TextObject("Return the sacred object to the First Tree Priestess."), 0, 1);
                        _ambushCheckStartTime = CampaignTime.Now;
                        InformationManager.DisplayMessage(new InformationMessage("⚠️ You feel like you are being watched..."));
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("❌ Sacred Object (Item ID) not found.", Colors.Red));
                    }
                })
                .CloseDialog();
        }

        private DialogFlow PriestessReturnDialog() => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("What was that? They came out of nowhere!"))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.HeroObject?.StringId == OWL_HERO_ID && _returnSacredObjectLog?.CurrentProgress == 1 && !PlayerHasSacredObject())
            .NpcLine(new TextObject("Not even our scouts saw them. It could be sorcery."))
            .PlayerLine(new TextObject("The vessel... I lost it in the fight."))
            .NpcLine(new TextObject("Or it was stolen. If magic is involved, perhaps they were tracking us. The ambush might have been just a distraction."))
            .PlayerLine(new TextObject("It's possible. But we have no leads."))
            .NpcLine(new TextObject("Unless the mages are involved. Perhaps they can track it—if they are willing."))
            .PlayerLine(new TextObject("You're right. We have no other leads."))
            .Consequence(() =>
            {
                ForceRemoveSacredObject();
                _meetPriestessLog = AddDiscreteLog(new TextObject("Find the Priestess"), new TextObject("Return to the Priestess at the First Tree and report what happened."), 0, 1);
                _meetPriestessLog.UpdateCurrentProgress(1);
            })
            .CloseDialog();

        private DialogFlow PriestessFinalDialog() => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("Priestess, we were ambushed. The vessel was taken."))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == PRIESTESS_CHAR_ID && _meetPriestessLog != null && _meetPriestessLog.CurrentProgress == 1 && !PlayerHasSacredObject())
            .NpcLine(new TextObject("Ancient magic is stirring... This was no accident. I suspect the mages, but I have no proof."))
            .PlayerLine(new TextObject("Then I will investigate them."))
            .Consequence(() =>
            {
                _investigateMagesLog = AddDiscreteLog(new TextObject("Investigate the Mages"), new TextObject("Travel near their lands and search for clues about their involvement."), 0, 1);
            })
            .CloseDialog();

        private DialogFlow MageSurvivorDialog() => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("Easy now. You are safe. What happened here?"))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == RF_MAGE_SURVIVOR_HERO && _mageBossDefeated && !_mageSurvivorDialogueDone)
            .NpcLine(new TextObject("They forced us to drink foul liquids... Little by little, some went mad, others... changed."))
            .PlayerLine(new TextObject("Who are 'they'?"))
            .NpcLine(new TextObject("Mages. Or something worse, using them. I saw symbols... And I saw prisoners being taken east, to the lands of the Urkhai."))
            .PlayerLine(new TextObject("Urkhai... Then that is where I must look."))
            .NpcLine(new TextObject("If you go, be quick. Those who are taken there... rarely return."))
            .Consequence(() =>
            {
                _orcTrailLog = AddDiscreteLog(new TextObject("Trail to the Orcs"), new TextObject("The survivor revealed that captives were taken into Urkhai territory. Travel there to continue your investigation."), 0, 1);
                _mageSurvivorDialogueDone = true;
                StartNinthQuest();
            })
            .CloseDialog();

        // ======================= MÉTODOS UTILITÁRIOS =======================

        private void InitializeEighthQuestHideout()
        {
            if (QuestHideoutSettlement?.Hideout != null)
            {
                QuestLibrary.InitializeHideoutIfNeeded(QuestHideoutSettlement.Hideout);
                AddTrackedObject(QuestHideoutSettlement);
            }
        }

        private void CleanupEighthQuestHideout()
        {
            if (QuestHideoutSettlement != null) RemoveTrackedObject(QuestHideoutSettlement);
        }

        private void StartSacredObjectDialogue()
        {
            if (TheOwl != null)
            {
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(TheOwl.CharacterObject, PartyBase.MainParty)
                );
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Não foi possível encontrar o Owl para iniciar a conversa!", Colors.Red));
            }
        }

        private void ReinforceQuestLogs()
        {
            if (_talkToOwlLog == null && _findSacredObjectLog == null)
            {
                // ✅ CORRIGIDO: O método AddLog foi chamado corretamente.
                _talkToOwlLog = AddLog(new TextObject("The Owl wishes to speak with you about the recent reports of deformed creatures."));
            }

            if (_returnSacredObjectLog == null && _findSacredObjectLog?.CurrentProgress == 1 && PlayerHasSacredObject())
            {
                _returnSacredObjectLog = AddDiscreteLog(new TextObject("Return the Sacred Object"), new TextObject("Return the sacred object to the First Tree Priestess."), 0, 1);
            }
        }

        private bool PlayerHasSacredObject()
        {
            var item = MBObjectManager.Instance.GetObject<ItemObject>(SACRED_OBJECT_ID);
            return item != null && MobileParty.MainParty.ItemRoster.GetItemNumber(item) > 0;
        }

        private void ForceRemoveSacredObject()
        {
            var sacredObject = MBObjectManager.Instance.GetObject<ItemObject>(SACRED_OBJECT_ID);
            if (sacredObject == null) return;

            int count = MobileParty.MainParty.ItemRoster.GetItemNumber(sacredObject);
            if (count > 0)
            {
                MobileParty.MainParty.ItemRoster.AddToCounts(sacredObject, -count);
            }
        }

        private void StartNinthQuest()
        {
            if (_ninthQuestStarted) return;
            _ninthQuestStarted = true;
            new NinthQuest("rf_ninth_quest", QuestGiver, CampaignTime.Never, 0).StartQuest();
        }
    }
}