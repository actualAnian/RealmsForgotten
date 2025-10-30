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
using RealmsForgotten.Quest.MissionBehaviors;
using static RealmsForgotten.Quest.QuestLibrary;
using TaleWorlds.ScreenSystem;
using TaleWorlds.ModuleManager;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class NinthQuest : QuestBase
    {
        // ================== Campos Salvos ==================
        [SaveableField(0)] private JournalLog _travelToUrkhaiLog;
        [SaveableField(1)] private JournalLog _interceptConvoyLog;
        [SaveableField(2)] private JournalLog _captureOrcLordLog;
        [SaveableField(3)] private bool _convoySpawned;
        [SaveableField(4)] private bool _convoyDefeated;
        [SaveableField(5)] private bool _owlDialogueTriggered;
        [SaveableField(6)] private MobileParty _orcConvoyParty;
        [SaveableField(7)] private JournalLog _interrogateLordLog;
        [SaveableField(9)] private Hero _capturedOrcLordForInterrogation;
        [SaveableField(10)] private bool _shouldTriggerInterrogation;
        [SaveableField(19)] private bool _pendingOrcPrisonerDialogue = false;

        [SaveableField(20)] private JournalLog _goToAmbushSiteLog;
        [SaveableField(21)] private bool _ambushSiteMissionEnded = false;
        [SaveableField(22)] private bool _pendingPostAmbushOwlDialogue = false;
        [SaveableField(23)] private JournalLog _captureUrkhaiLordForPriestessLog;
        [SaveableField(24)] private JournalLog _deliverUrkhaiLordLog;
        [SaveableField(25)] private Hero _capturedUrkhaiLordForPriestess;
        [SaveableField(26)] private JournalLog _retrieveSacredWaterLog;
        [SaveableField(27)] private JournalLog _returnSacredWaterLog;

        [SaveableField(13)] private bool _witchDefeatedInMission;
        [SaveableField(14)] private CampaignTime _hordeTimerStart = CampaignTime.Never;
        [SaveableField(15)] private MobileParty _deformedHordeParty;
        [SaveableField(16)] private Hero _hordeLeaderHero;
        [SaveableField(17)] private bool _hordeHasSpawned;
        [SaveableField(18)] private JournalLog _defeatHordeLog;
        [SaveableField(28)] private bool _pendingFinalizeAfterVideo;



        // ================== IDs e Constantes ==================
        private const string URKHAI_TRIGGER_SETTLEMENT_ID = "town_Urk1";
        private const string ORC_CONVOY_PARTY_ID = "rf_orc_convoy_party";
        private const string ORC_PRISONER_CHARACTER_ID = "urkhai_veteran_infantry";
        private const string AMBUSH_SITE_SETTLEMENT_ID = "deformed_bandits_ambush";
        private const string FIRST_TREE_SETTLEMENT_ID = "town_FirstTree";
        private const string PRIESTESS_ID = "elvean_first_tree_druid_quest";
        private const string SACRED_WATER_ITEM_ID = "sacredwater";
        private const string SACRED_WATER_SITE_ID = "beast_hunt_1";
        private const string WITCH_LAIR_SETTLEMENT_ID = "winged_witch_final_scene";

        // ================== Propriedades ==================
        private Settlement TriggerSettlement => Settlement.Find(URKHAI_TRIGGER_SETTLEMENT_ID);
        private Settlement AmbushSite => Settlement.Find(AMBUSH_SITE_SETTLEMENT_ID);
        private Settlement FirstTreeSettlement => Settlement.Find(FIRST_TREE_SETTLEMENT_ID);
        private Settlement SacredWaterSite => Settlement.Find(SACRED_WATER_SITE_ID);
        private Settlement WitchLair => Settlement.Find(WITCH_LAIR_SETTLEMENT_ID);

        private const string AMBUSH_BOSS_ID = "deformed_villager_boss";
        public static NinthQuest ActiveNinthQuestInstance { get; private set; }

        public override TextObject Title => new TextObject("Ninth Quest: The Orc Trail");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => true;

        private static readonly string[] SACRED_WATER_IDS = { "sacredwater" }; // deixe os 2 se tiver dúvida no XML

        private int CountItemInMainPartyById(params string[] ids)
        {
            var roster = MobileParty.MainParty?.ItemRoster;
            if (roster == null) return 0;

            int total = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                var elem = roster.GetElementCopyAtIndex(i);
                var it = elem.EquipmentElement.Item;
                if (it == null) continue;

                for (int j = 0; j < ids.Length; j++)
                {
                    if (it.StringId == ids[j])
                    {
                        total += elem.Amount;
                        break;
                    }
                }
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"DEBUG: SacredWater count by StringId = {total}.", Colors.Yellow));
            return total;
        }
        private bool PlayerHasSacredWater()
        {
            return CountItemInMainPartyById(SACRED_WATER_IDS) > 0;
        }
        private void EnsureReturnWaterLogCreated()
        {
            if (_returnSacredWaterLog == null)
            {
                TextObject body = new TextObject("You have retrieved the Sacred Water. Return to the Priestess at the {SETTLEMENT_NAME}.");
                body.SetTextVariable("SETTLEMENT_NAME", FirstTreeSettlement.Name);

                _returnSacredWaterLog = AddDiscreteLog(
                    new TextObject("Deliver the Sacred Water"),
                    body,
                    0, // começa pendente
                    1  // completa quando entregar
                );

                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Created _returnSacredWaterLog (0/1).", Colors.Green));

                if (SacredWaterSite != null) RemoveTrackedObject(SacredWaterSite);
                if (FirstTreeSettlement != null) AddTrackedObject(FirstTreeSettlement);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"DEBUG: _returnSacredWaterLog já existe. Progress={_returnSacredWaterLog.CurrentProgress}", Colors.Yellow));
            }
        }

        public NinthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold)
        {
            ActiveNinthQuestInstance = this;
        }

        protected override void InitializeQuestOnGameLoad()
        {
            ActiveNinthQuestInstance = this;
            SetDialogs();
        }

        protected override void OnStartQuest()
        {
            ActiveNinthQuestInstance = this;
            SetDialogs();
            RegisterEvents();

            TextObject log = new TextObject("The mage survivor spoke of prisoners being taken east, into Urkhai territory. Travel to the lands near {SETTLEMENT_NAME} to find their trail.");
            log.SetTextVariable("SETTLEMENT_NAME", TriggerSettlement.Name);
            _travelToUrkhaiLog = AddLog(log);
            AddTrackedObject(TriggerSettlement);
            _hordeTimerStart = CampaignTime.Now;
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckPriestessProximity);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);


        }


      
        private void TryCompleteRetrieveSacredWaterStep(string debugReason)
        {
            if (_retrieveSacredWaterLog?.CurrentProgress != 1)
                return; // só fecha se essa etapa ainda estiver pendente (==1)

            if (!PlayerHasSacredWater())
                return; // sem item, não avança

            // ✅ fecha a etapa DISCRETA de “pegar a água”
            _retrieveSacredWaterLog.UpdateCurrentProgress(2);

            // cria (ou garante) o objetivo de entrega, também DISCRETO
            TextObject desc = new TextObject("You have retrieved the Sacred Water. Return to the Priestess at the {SETTLEMENT_NAME}.");
            desc.SetTextVariable("SETTLEMENT_NAME", FirstTreeSettlement.Name);

            if (_returnSacredWaterLog == null)
            {
                _returnSacredWaterLog = AddDiscreteLog(
                    new TextObject("Deliver the Sacred Water"),
                    desc,
                    0, // 0 → ainda não entregue
                    1  // total steps
                );
            }

            // atualiza tracking
            RemoveTrackedObject(SacredWaterSite);
            AddTrackedObject(FirstTreeSettlement);

            InformationManager.DisplayMessage(
                new InformationMessage($"DEBUG: Sacred Water step completed via {debugReason}.", Colors.Green)
            );
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party == null || !party.IsMainParty)
                return;

            // Entrou no local da água sagrada: 0 -> 1
            if (settlement == SacredWaterSite &&
                _retrieveSacredWaterLog != null &&
                _retrieveSacredWaterLog.CurrentProgress == 0)
            {
                _retrieveSacredWaterLog.UpdateCurrentProgress(1);
                InformationManager.DisplayMessage(
                    new InformationMessage("DEBUG: Entered SacredWaterSite → retrieve log set to 1.", Colors.Yellow));
            }
        }

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (party == null || !party.IsMainParty) return;

            // Sacred Water
            if (settlement == SacredWaterSite && _retrieveSacredWaterLog?.CurrentProgress == 1)
            {
                if (PlayerHasSacredWater())
                {
                    _retrieveSacredWaterLog.UpdateCurrentProgress(2);

                    TextObject body = new TextObject("You have retrieved the Sacred Water. Return to the Priestess at the {SETTLEMENT_NAME}.");
                    body.SetTextVariable("SETTLEMENT_NAME", FirstTreeSettlement.Name);

                    if (_returnSacredWaterLog == null)
                        _returnSacredWaterLog = AddDiscreteLog(new TextObject("Deliver the Sacred Water"), body, 0, 1);

                    RemoveTrackedObject(SacredWaterSite);
                    AddTrackedObject(FirstTreeSettlement);
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("You left the Sacred Spring, but you do not have the Sacred Water with you.", Colors.Red));
                }
            }

            // Witch Lair
            if (settlement == WitchLair && _witchDefeatedInMission)
            {
                _witchDefeatedInMission = false; // evita repetir
                InformationManager.DisplayMessage(new InformationMessage("DEBUG: Left Witch Lair after defeating Witch → trigger cutscene.", Colors.Green));
                PlayFinalCutscene();
            }

            // Ambush Site
            if (settlement == AmbushSite && _goToAmbushSiteLog != null && _goToAmbushSiteLog.CurrentProgress == 1 && !_ambushSiteMissionEnded)
            {
                _goToAmbushSiteLog.UpdateCurrentProgress(2);
                _ambushSiteMissionEnded = true;
                _pendingPostAmbushOwlDialogue = true;
                RemoveTrackedObject(AmbushSite);

                if (_captureUrkhaiLordForPriestessLog == null)
                    _captureUrkhaiLordForPriestessLog = AddLog(new TextObject("The ambush was a trap! Speak to the Owl to decide your next move."));
            }
        }


        private void CheckPriestessProximity()
        {
            if (FirstTreeSettlement == null) return;

            float distance = MobileParty.MainParty.Position.Distance(FirstTreeSettlement.GatePosition);

            if (distance > 50f) return; // only trigger when close

            var priestess = CharacterObject.Find(PRIESTESS_ID);
            if (priestess == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Priestess CharacterObject not found.", Colors.Red));
                return;
            }

            // Case 1: Delivering Urkhai Lord
            if (_deliverUrkhaiLordLog != null && _deliverUrkhaiLordLog.CurrentProgress == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Near Priestess → opening Lord delivery dialog.", Colors.Green));

                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(priestess)
                );
            }

            // Case 2: Delivering Sacred Water
            if (_returnSacredWaterLog != null && _returnSacredWaterLog.CurrentProgress == 0 && PlayerHasSacredWater())
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Near Priestess → opening Sacred Water dialog.", Colors.Green));

                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(priestess)
                );
            }
        }

        private void OnMissionEnded(IMission mission)
        {

        }




        private void OnDailyTick()
        {
            if (_hordeHasSpawned || _hordeTimerStart == CampaignTime.Never) return;
            if (_hordeTimerStart.ElapsedDaysUntilNow >= 3)
            {
                _hordeHasSpawned = true;
                ShowPriestessWarningInquiry();
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (this.IsFinalized || _deformedHordeParty == null || !_deformedHordeParty.IsActive) return;
            if (mapEvent.InvolvedParties.Contains(_deformedHordeParty.Party))
            {
                var hordeSide = mapEvent.GetMapEventSide(_deformedHordeParty.Party.Side);
                if (mapEvent.WinningSide == hordeSide.MissionSide)
                {
                    MapEventSide loserSide = (mapEvent.WinningSide == mapEvent.AttackerSide.MissionSide) ? mapEvent.DefenderSide : mapEvent.AttackerSide;
                    int newRecruitsCount = (int)(loserSide.Parties.Sum(p => p.Party.MemberRoster.TotalManCount) * 0.25f);
                    if (newRecruitsCount > 0)
                    {
                        var deformedTroop = CharacterObject.Find("deformed_villager_bandit");
                        if (deformedTroop != null)
                        {
                            _deformedHordeParty.MemberRoster.AddToCounts(deformedTroop, newRecruitsCount);
                            InformationManager.DisplayMessage(new InformationMessage($"The Deformed Horde crushed its enemies and grew by {newRecruitsCount} abominations!", Colors.Red));
                        }
                    }
                }
                else
                {
                    if (_defeatHordeLog != null && _defeatHordeLog.CurrentProgress == 0)
                    {
                        _defeatHordeLog.UpdateCurrentProgress(1);
                        InformationManager.DisplayMessage(new InformationMessage("The Deformed Horde has been vanquished from the lands!", Colors.Green));
                        if (mapEvent.PlayerSide == mapEvent.WinningSide)
                        {
                            Hero.MainHero.Clan.Renown += 250;
                            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 50000, false);
                            InformationManager.ShowInquiry(new InquiryData("A Threat Averted", "You have personally defeated the Deformed Horde and its champion, Ghor'Lag. Your heroic deed will be sung across the realms! You have been rewarded for your valor.", true, false, "Excellent!", null, null, null));
                        }
                    }
                    _deformedHordeParty = null;
                }
            }
        }

        private void OnInterrogationSuccess()
        {
            _interrogateLordLog.UpdateCurrentProgress(1);

            _goToAmbushSiteLog = AddDiscreteLog(
                new TextObject("Investigate the Hideout"),
                new TextObject("The captured Orc Lord revealed the Dark Queen's supposed hideout. Travel there..."),
                0, 2
            );

            AmbushSite.IsVisible = true;
            AddTrackedObject(AmbushSite);
        }

        // ⚠️ Corrija o registro do evento para usar este nome: OnMissionStarted
        private void OnMissionStarted(IMission imission)
        {
            if (imission is Mission mission)
            {
                // Witch Lair (seu código original)
                if (Settlement.CurrentSettlement == WitchLair && _returnSacredWaterLog?.CurrentProgress == 1)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                "DEBUG: Entered Witch Lair → adding WingedWitchFinalMissionLogic.", Colors.Yellow));

                    mission.AddMissionBehavior(new WingedWitchFinalMissionLogic());
                }
                if (Settlement.CurrentSettlement == AmbushSite && _goToAmbushSiteLog?.CurrentProgress == 0)
                {
                    _goToAmbushSiteLog.UpdateCurrentProgress(1);
                }

                // (Opcional) se quiser fechar pelo BOSS derrotado, mantemos o hook:
                if (Settlement.CurrentSettlement == AmbushSite && _goToAmbushSiteLog?.CurrentProgress == 1)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "DEBUG: Added RecordDamageMissionLogic for ambush boss.", Colors.Yellow));

                    mission.AddMissionBehavior(new RecordDamageMissionLogic((victim, attacker, damage) =>
                    {
                       

                        if (victim?.Character != null &&
                            victim.Character.StringId == AMBUSH_BOSS_ID &&
                            damage >= victim.Health)
                        {
                            _ambushSiteMissionEnded = true;
                            _pendingPostAmbushOwlDialogue = true;

                            if (_goToAmbushSiteLog.CurrentProgress < 2)
                                _goToAmbushSiteLog.UpdateCurrentProgress(2);

                            RemoveTrackedObject(AmbushSite);
                            AddLog(new TextObject("The ambush was a trap! Speak to the Owl to decide your next move."));

                            InformationManager.DisplayMessage(new InformationMessage(
                                "DEBUG: Ambush boss defeated → log set to 2, flags set.", Colors.Green));
                        }
                    }));
                }
  
            }
        }



        public static void OnWitchDefeatedInNinthQuest()
        {
            if (ActiveNinthQuestInstance != null)
            {
                ActiveNinthQuestInstance._witchDefeatedInMission = true;

                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Winged Witch defeated → flag set. Quest will complete after leaving the Witch Lair.",
                    Colors.Green));
            }
        }

        private void PlayFinalCutscene()
        {
            try
            {
                var gsm = Game.Current.GameStateManager;
                var video = gsm.CreateState<VideoPlaybackState>();

                string modPath = ModuleHelper.GetModuleFullPath("RealmsForgotten");
                string basePath = System.IO.Path.Combine(modPath, "Videos/Quest_Cutscene");
                string baseName = "rf_final_cutscene"; // seus arquivos .ivf/.ogg/.srt

                string videoPath = System.IO.Path.Combine(basePath, baseName + ".ivf");
                string audioPath = System.IO.Path.Combine(basePath, baseName + ".ogg");
                string subsBase = System.IO.Path.Combine(basePath, baseName);

                video.SetStartingParameters(videoPath, audioPath, subsBase);

                // Delegate chamado quando termina ou pula
                video.SetOnVideoFinisedDelegate(() =>
                {
                    try
                    {
                        gsm.PopState(); // fecha o vídeo, volta pro MapState
                    }
                    catch { }

                    _pendingFinalizeAfterVideo = true; // marca para finalizar quest no Tick
                });

                // ✅ Usa PushState (não CleanAndPush) → mantém MapState abaixo
                gsm.PushState(video);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"DEBUG: Could not play cutscene: {ex.Message}", Colors.Red));

                _pendingFinalizeAfterVideo = true; // fallback
            }
        }
        protected override void HourlyTick()
        {
            if (!_convoySpawned && TriggerSettlement != null && _travelToUrkhaiLog?.CurrentProgress == 0)
            {
                if (MobileParty.MainParty.Position.Distance(TriggerSettlement.GatePosition) <= 70f)
                {
                    // ✅ CORREÇÃO: Completa o log anterior
                    _travelToUrkhaiLog.UpdateCurrentProgress(1);
                    RemoveTrackedObject(TriggerSettlement);
                    _convoySpawned = true;
                    ShowConvoySpottedInquiry();
                }
            }
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (!mapEvent.IsPlayerMapEvent || mapEvent.WinningSide != mapEvent.PlayerSide) return;

            if (_convoySpawned && !_convoyDefeated)
            {
                foreach (MapEventParty defeatedParty in mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Parties)
                {
                    if (defeatedParty.Party?.MobileParty?.StringId == ORC_CONVOY_PARTY_ID)
                    {
                        _convoyDefeated = true;
                        _interceptConvoyLog?.UpdateCurrentProgress(1); // ✅ completa objetivo
                        _pendingOrcPrisonerDialogue = true;

                        if (_orcConvoyParty != null) RemoveTrackedObject(_orcConvoyParty);
                        break;
                    }
                }
            }
        }

        private void OnHeroPrisonerTaken(PartyBase capturerParty, Hero prisoner)
        {
            if (capturerParty != PartyBase.MainParty || !prisoner.IsLord || prisoner.Culture.StringId != "urkhai") return;
            if (_captureOrcLordLog != null && _captureOrcLordLog.CurrentProgress == 0)
            {
                // ✅ CORREÇÃO: Completa o log anterior
                _captureOrcLordLog.UpdateCurrentProgress(1);
                _capturedOrcLordForInterrogation = prisoner;
                _shouldTriggerInterrogation = true;
                _interrogateLordLog = AddDiscreteLog(
           new TextObject("Interrogate the Orc Lord"),
           new TextObject("You have captured an Urkhai Lord. Interrogate him to discover the Witch's whereabouts."),
           0, 1
       );
                return;
            }
            if (_captureUrkhaiLordForPriestessLog != null && _captureUrkhaiLordForPriestessLog.CurrentProgress == 0)
            {
                // ✅ CORREÇÃO: Completa o log anterior
                _captureUrkhaiLordForPriestessLog.UpdateCurrentProgress(1);
                _capturedUrkhaiLordForPriestess = prisoner;
                TextObject logText = new TextObject("You have captured the Urkhai Lord, {LORD_NAME}. Now take him to the Priestess at the {SETTLEMENT_NAME} for questioning.");
                logText.SetTextVariable("LORD_NAME", prisoner.Name);
                logText.SetTextVariable("SETTLEMENT_NAME", FirstTreeSettlement.Name);
                _deliverUrkhaiLordLog = AddLog(logText);
                AddTrackedObject(FirstTreeSettlement);
            }
        }

        private void OnTick(float dt)
        {
           
            if (_pendingOrcPrisonerDialogue)
            {
                _pendingOrcPrisonerDialogue = false;

                CharacterObject orcPrisoner = CharacterObject.Find(ORC_PRISONER_CHARACTER_ID);
                if (orcPrisoner != null)
                {
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(orcPrisoner, PartyBase.MainParty));
                }
            }

            // 🔹 Gatilho para conversa com o Owl após o prisioneiro
            if (_owlDialogueTriggered)
            {
                _owlDialogueTriggered = false;
                Hero owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");
                if (owl != null)
                {
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(owl.CharacterObject, PartyBase.MainParty));
                }
            }

            // 🔹 Gatilho para interrogatório do Lorde capturado
            if (_shouldTriggerInterrogation)
            {
                _shouldTriggerInterrogation = false;
                if (_capturedOrcLordForInterrogation != null)
                {
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(_capturedOrcLordForInterrogation.CharacterObject));
                }
            }

            if (_pendingPostAmbushOwlDialogue)
            {
                _pendingPostAmbushOwlDialogue = false;

                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Trying to open Owl post-ambush conversation.", Colors.Yellow));

                Hero owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");
                if (owl != null)
                {
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(owl.CharacterObject, PartyBase.MainParty));

                    InformationManager.DisplayMessage(new InformationMessage(
                        "DEBUG: Owl found, conversation opened.", Colors.Green));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "DEBUG: Owl NOT found!", Colors.Red));
                }
            }
            if (_retrieveSacredWaterLog?.CurrentProgress == 2 &&
                  _returnSacredWaterLog == null &&
                  PlayerHasSacredWater())
            {
                TextObject logText = new TextObject("You have retrieved the Sacred Water. Return to the Priestess at the {SETTLEMENT_NAME}.");
                logText.SetTextVariable("SETTLEMENT_NAME", FirstTreeSettlement.Name);

                _returnSacredWaterLog = AddDiscreteLog(
                    new TextObject("Deliver the Sacred Water"),
                    logText, 0, 1);

                RemoveTrackedObject(SacredWaterSite);
                AddTrackedObject(FirstTreeSettlement);
            }
            if (_pendingFinalizeAfterVideo)
            {
                _pendingFinalizeAfterVideo = false;

               
                AddLog(new TextObject("With the Witch finally cast down, a fragile peace settles once more over Aeurth. Many were blind to the shadow she wove, yet you and the Owl carry the scars of a tale that few will ever truly grasp. The world itself has changed, for now it knows of the hidden powers that stir in the dark. And though their whispers may rise again, today their voices are silenced… and your victory will echo through the ages."));
                CompleteQuestWithSuccess();
                Hero.MainHero.Clan.Renown += 1500;
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 100000);
                GainKingdomInfluenceAction.ApplyForDefault(Hero.MainHero, 500); 

                
                Kingdom battania = Kingdom.All.FirstOrDefault(k => k.StringId == "battania");
                if (battania != null)
                {
                    foreach (Clan clan in battania.Clans)
                    {
                        foreach (Hero lord in clan.AliveLords)
                        {
                            if (lord != null && lord != Hero.MainHero && lord.IsAlive)
                            {
                                ChangeRelationAction.ApplyPlayerRelation(lord, 50); 
                            }
                        }
                    }
                }

                // ✅ Mostra a janela de vitória apenas uma vez
                InformationManager.ShowInquiry(new InquiryData(
                    "Victory!",
                    "You have defeated the Dark Queen and ended her reign of terror. The kingdoms are safe once more, thanks to your heroism.",
                    true, false, "Done", null, null, null));
            }
        }




        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(OrcPrisonerDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(OwlFollowUpDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(OrcLordInterrogationDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(PostAmbushOwlDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessInterrogationDialog(), this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessFinalRevelationDialog(), this);
        }

        private DialogFlow OrcPrisonerDialog() => DialogFlow.CreateDialogFlow("start", 125)
     .NpcLine(new TextObject("You will pay for this! The Dark Queen will protect us! She will avenge us!"))
     .Condition(() =>
         _convoyDefeated &&
         CharacterObject.OneToOneConversationCharacter != null &&
         CharacterObject.OneToOneConversationCharacter.StringId == ORC_PRISONER_CHARACTER_ID
     )
     .PlayerLine(new TextObject("The Dark Queen? So the rumors are true. Where is she?"))
     .NpcLine(new TextObject("Never! I'd rather die than betray her! Her power grows, and soon, you will all bow!"))
     .Consequence(() => { _owlDialogueTriggered = true; })
     .CloseDialog();



        // ✅ DIÁLOGO ATUALIZADO PARA O PADRÃO DA FOURTHQUEST
        private DialogFlow OwlFollowUpDialog() => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(new TextObject("The 'Dark Queen'... It must be the Witch. It seems she has found powerful allies in the Urkhai."))
            .Condition(() => Hero.OneToOneConversationHero?.StringId == "rf_the_owl" && _interceptConvoyLog?.CurrentProgress == 1 && _captureOrcLordLog == null)
            .PlayerLine(new TextObject("This complicates things. A common soldier won't betray her. We need someone higher up."))
            .NpcLine(new TextObject("Exactly. We must capture an Urkhai Lord. A chieftain might break under... 'persuasion' and reveal her location. Find one, defeat his army, and take him prisoner."))
            .Consequence(() => { _captureOrcLordLog = AddLog(new TextObject("An Urkhai soldier mentioned a 'Dark Queen'. The Owl believes it is the Witch. To find her, you must defeat and capture an Urkhai Lord for interrogation.")); })
            .CloseDialog();

        private DialogFlow OrcLordInterrogationDialog() => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("I have you now. Your soldier spoke of a 'Dark Queen'. Tell me where she is hiding."))
            .Condition(() => Hero.OneToOneConversationHero == _capturedOrcLordForInterrogation && _interrogateLordLog?.CurrentProgress == 0)
            .NpcLine(new TextObject("You think capturing me is a victory? I will tell you nothing, human filth."))
            .PlayerLine(new TextObject("My patience is wearing thin. Your life is in my hands. Her location, now!"))
            .NpcLine(new TextObject("Hah! My life is forfeit to her, not to you. Do your worst!"))
            .PlayerLine(new TextObject("Very well. Perhaps your clan values your life more than you do. I'll send them your head and see."))
            .NpcLine(new TextObject("...Wait. Fine. You win. She has taken refuge in an old, cursed site in the mountains. A place your kind calls {LAIR_NAME}."))
            .Condition(() => { MBTextManager.SetTextVariable("LAIR_NAME", AmbushSite.Name); return true; })
            .PlayerLine(new TextObject("I knew you'd see reason. Your life is spared... for now."))
            .Consequence(OnInterrogationSuccess)
            .CloseDialog();

        private DialogFlow PostAmbushOwlDialog() => DialogFlow.CreateDialogFlow("start", 200)
     .NpcLine(new TextObject("It was a trap! The Witch was never there. The Orc Lord played us for fools."))
     .Condition(() =>
         Hero.OneToOneConversationHero?.StringId == "rf_the_owl"
         && _ambushSiteMissionEnded
         && _goToAmbushSiteLog != null
         && _goToAmbushSiteLog.CurrentProgress >= 2
     )
     .PlayerLine(new TextObject("So we're back to square one. We have no idea where she is."))
     .NpcLine(new TextObject("Not entirely. The Orcs themselves... their unwavering loyalty, their resistance to pain... it's unnatural. The Witch must be controlling them through some dark artifact or spell."))
     .PlayerLine(new TextObject("What are you suggesting?"))
     .NpcLine(new TextObject("We need to bring a high-ranking subject to the Priestess. A common soldier won't do. Capture another Urkhai Lord. The Priestess can perform a ritual to sever the Witch's connection and force the truth from him."))
     .Consequence(() =>
     {
         _captureUrkhaiLordForPriestessLog = AddLog(new TextObject(
             "The hideout was a trap. The Owl believes the Witch is controlling the Orcs. " +
             "Capture another Urkhai Lord and take him to the First Tree Priestess for a ritual."
         ));
         _ambushSiteMissionEnded = false; // evita repetir
     })
     .CloseDialog();





        private DialogFlow PriestessInterrogationDialog() => DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(new TextObject("Priestess, I have brought you the Urkhai Lord as you asked."))
            .Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == PRIESTESS_ID && _deliverUrkhaiLordLog != null && _deliverUrkhaiLordLog.CurrentProgress == 0)
            .NpcLine(new TextObject("You have done well. His mind is a fortress of dark magic, but the First Tree sees all truths. Leave him with us. There is a more urgent matter."))
            .PlayerLine(new TextObject("More urgent? What have you discovered?"))
            .NpcLine(new TextObject("It seems what is behind those aberrations is far more insidious than we imagined. If the Wicth is indeed back, she does not seek to rule with armies alone, but to corrupt all life. She has found a way to poison the very land, to turn all living things into twisted, deformed creatures in her service."))
            .PlayerLine(new TextObject("We must stop her! As fast as we can!"))
            .NpcLine(new TextObject("Finding her is only half the battle. We need a ward against her corruption. The Xilantlacy people speak of a sacred spring, a place where their gods blood flow. You must retrieve its water. It is probably the only thing that can be an antidote and cleanse the land. Search for it, while we extract the Witch's location from this one."))
            .Consequence(() =>
            {
                // ✅ CORREÇÃO: Completa o log anterior
                _deliverUrkhaiLordLog.UpdateCurrentProgress(1);
                if (_capturedUrkhaiLordForPriestess != null && _capturedUrkhaiLordForPriestess.IsPrisoner)
                {
                    TransferPrisonerAction.Apply(_capturedUrkhaiLordForPriestess.CharacterObject, PartyBase.MainParty, FirstTreeSettlement.Party);
                }
                TextObject logText = new TextObject("The Priestess has revealed the Witch's terrifying plan. You must travel to the hidden Sacred Spring and retrieve its water to counter the corruption.");
                _retrieveSacredWaterLog = AddDiscreteLog(logText, new TextObject("Sacred Water Retrieved"), 0, 2);
                if (SacredWaterSite != null)
                {
                    SacredWaterSite.IsVisible = true;
                    AddTrackedObject(SacredWaterSite);
                }
            })
            .CloseDialog();

        private DialogFlow PriestessFinalRevelationDialog() => DialogFlow.CreateDialogFlow("start", 125)
     .PlayerLine(new TextObject("Priestess, I have returned with the Sacred Water."))
     .Condition(() =>
         CharacterObject.OneToOneConversationCharacter?.StringId == PRIESTESS_ID
         && _returnSacredWaterLog != null
         && _returnSacredWaterLog.CurrentProgress == 0 // pendente (0/1)
         && PlayerHasSacredWater()                     // com o item na mochila
     )
     .NpcLine(new TextObject("You have returned just in time. The ritual is complete. The Urkhai Lord's will was broken, and he revealed the Witch's true lair."))
     .PlayerLine(new TextObject("Where is she?"))
     .NpcLine(new TextObject("She hides along the snowy peaks of the Urkhai, following to the east. A region very few have laid foot, and no one today is found alive to tell about it. I believe there is where the last of the Vortiaks forces have managed to hide after the great war... Keep some of the water with you, it will shield you from her worst corruptions and restore your health when necessary. Please, end her, once and for all."))
     .Consequence(() =>
     {
         _returnSacredWaterLog.UpdateCurrentProgress(1); // conclui 1/1

         var item = MBObjectManager.Instance.GetObject<ItemObject>(SACRED_WATER_ITEM_ID);
         if (item != null)
         {
             // remove exatamente 1 frasco (se você quiser remover todos, pegue o count e subtraia)
             MobileParty.MainParty.ItemRoster.AddToCounts(item, -1);
             InformationManager.DisplayMessage(new InformationMessage(
                 "DEBUG: Sacred Water removida do inventário.", Colors.Yellow));
         }

         AddLog(new TextObject("You have delivered the Sacred Water. The Priestess has revealed the Witch's true location. The final confrontation awaits."));

         if (WitchLair != null)
         {
             WitchLair.IsVisible = true;
             AddTrackedObject(WitchLair);
         }

        
     })
     .CloseDialog();

        private void ShowConvoySpottedInquiry()
        {
            InformationManager.ShowInquiry(new InquiryData("Convoy Spotted", "Your scouts report seeing a large Urkhai army escorting a group of prisoners towards a mountain pass. This must be the group the mage survivor mentioned.", true, false, "Intercept Them", null, SpawnAndTrackConvoy, null));
        }

        private void SpawnAndTrackConvoy()
        {
            Clan urkhaiClan = Clan.FindFirst(c => c.StringId == "urkhai_faction_1");
            if (urkhaiClan == null) { InformationManager.DisplayMessage(new InformationMessage("Error: Urkhai clan not found.", Colors.Red)); return; }
            CharacterObject leaderTemplate = CharacterObject.Find("uruk_hai_expert_infantry");
            if (leaderTemplate == null) { InformationManager.DisplayMessage(new InformationMessage("Error: Convoy leader template not found.", Colors.Red)); return; }
            Hero convoyLeader = HeroCreator.CreateSpecialHero(leaderTemplate, null, urkhaiClan, null, 35);
            convoyLeader.SetName(new TextObject("Murgash"), new TextObject("Murgash the Chain-Keeper"));
            TroopRoster convoyRoster = TroopRoster.CreateDummyTroopRoster();
            convoyRoster.AddToCounts(convoyLeader.CharacterObject, 1);
            convoyRoster.AddToCounts(CharacterObject.Find("urkhai_troop"), 40);
            convoyRoster.AddToCounts(CharacterObject.Find("urkhai_warrior_infantry"), 30);
            convoyRoster.AddToCounts(CharacterObject.Find("urkhai_veteran_infantry"), 25);
            convoyRoster.AddToCounts(CharacterObject.Find("urkhai_archer"), 50);
            convoyRoster.AddToCounts(CharacterObject.Find("urkhai_warrior_infantry"), 50);
            TroopRoster prisonerRoster = TroopRoster.CreateDummyTroopRoster();
            prisonerRoster.AddToCounts(CharacterObject.Find("imperial_recruit"), 15);
            PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
            MobileParty _orcConvoyParty = BanditPartyComponent.CreateBanditParty(ORC_CONVOY_PARTY_ID, urkhaiClan, null, true, looterTemplate, MobileParty.MainParty.Position); //@TODO test

            if (_orcConvoyParty == null) { InformationManager.DisplayMessage(new InformationMessage("Error: Failed to create convoy party.", Colors.Red)); return; }
            _orcConvoyParty.InitializeMobilePartyAroundPosition(convoyRoster, prisonerRoster, MobileParty.MainParty.Position, 10f, 5f);
            _orcConvoyParty.ChangePartyLeader(convoyLeader);
            _orcConvoyParty.Party.SetCustomName(new TextObject("Convoy of {LEADER_NAME}").SetTextVariable("LEADER_NAME", convoyLeader.Name));
            Settlement destination = Settlement.Find("town_Urk2");
            _orcConvoyParty.SetMoveGoToSettlement(destination, MobileParty.NavigationType.All, false);
            _orcConvoyParty.Aggressiveness = 0.5f;
            _interceptConvoyLog = AddDiscreteLog(
      new TextObject("Intercept the Convoy"),
      new TextObject("You have spotted the Urkhai convoy. Defeat them and rescue the prisoners."),
      0, 1
  );
            AddTrackedObject(_orcConvoyParty);
            InformationManager.DisplayMessage(new InformationMessage("The Urkhai convoy led by Murgash the Chain-Keeper has been spotted nearby!", Colors.Yellow));
        }     

        private void SpawnDeformedAmbushParty()
        {
            // Este método foi movido para dentro de HourlyTick para simplificar
        }

        private void ShowPriestessWarningInquiry()
        {
            InformationManager.ShowInquiry(new InquiryData("An Urgent Message", "A messenger from the First Tree Priestess arrives, breathless. He reports that a new, massive army of deformed creatures has appeared in the north, led by a fearsome champion. They are destroying everything in their path, and their numbers swell with every victory. This new threat cannot be ignored!", true, false, "Acknowledge", null, SpawnDeformedHorde, null));
        }

        private void SpawnDeformedHorde()
        {
            Clan deformedClan = Clan.FindFirst(c => c.StringId == "deformed_villagers");
            if (deformedClan == null) { InformationManager.DisplayMessage(new InformationMessage("Error: Deformed clan not found.", Colors.Red)); return; }
            Settlement spawnNear = Settlement.Find("town_S2");
            if (spawnNear == null) { InformationManager.DisplayMessage(new InformationMessage("Error: Spawn location for horde not found.", Colors.Red)); return; }
            _hordeLeaderHero = HeroCreator.CreateSpecialHero(CharacterObject.Find("deformed_villager_boss"), null, deformedClan, null, 35);
            _hordeLeaderHero.SetName(new TextObject("Ghor'Lag the Unraveler"), new TextObject("The Blighted One"));
            PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
            MobileParty banditParty = BanditPartyComponent.CreateBanditParty("deformed_horde_party", deformedClan, null, true, looterTemplate, spawnNear.Position); //@TODO

            TroopRoster hordeRoster = TroopRoster.CreateDummyTroopRoster();
            hordeRoster.AddToCounts(CharacterObject.Find("deformed_villager_boss"), 5);
            hordeRoster.AddToCounts(CharacterObject.Find("deformed_villager_chief"), 20);
            hordeRoster.AddToCounts(CharacterObject.Find("deformed_villager_raider"), 50);
            _deformedHordeParty.InitializeMobilePartyAroundPosition(hordeRoster, TroopRoster.CreateDummyTroopRoster(), spawnNear.Position, 100f, 20f);
            _deformedHordeParty.ChangePartyLeader(_hordeLeaderHero);
            _deformedHordeParty.Party.SetCustomName(new TextObject("{=rf_horde_name}Deformed Horde of {LEADER_NAME}").SetTextVariable("LEADER_NAME", _hordeLeaderHero.Name));
            _deformedHordeParty.Aggressiveness = 10f;
            _deformedHordeParty.SetMovePatrolAroundSettlement(spawnNear, MobileParty.NavigationType.All, false);
            TextObject logText = new TextObject("A massive Deformed Horde, led by Ghor'Lag the Unraveler, has appeared near {LOCATION}. It grows stronger with each victory. This threat must be eliminated.");
            logText.SetTextVariable("LOCATION", spawnNear.Name);
            _defeatHordeLog = AddLog(logText);
            AddTrackedObject(_deformedHordeParty);
            TextObject messageText = new TextObject("The Deformed Horde has spawned, menacing the lands around {LOCATION}!");
            messageText.SetTextVariable("LOCATION", spawnNear.Name);
            InformationManager.DisplayMessage(new InformationMessage(messageText.ToString(), Colors.Red));
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();

            CampaignEvents.HourlyTickEvent.ClearListeners(this);
            CampaignEvents.DailyTickEvent.ClearListeners(this);
            CampaignEvents.MapEventEnded.ClearListeners(this);
            CampaignEvents.OnPlayerBattleEndEvent.ClearListeners(this);
            CampaignEvents.HeroPrisonerTaken.ClearListeners(this);
            CampaignEvents.TickEvent.ClearListeners(this);
            CampaignEvents.OnMissionStartedEvent.ClearListeners(this);
            CampaignEvents.OnSettlementLeftEvent.ClearListeners(this);
            CampaignEvents.OnMissionEndedEvent.ClearListeners(this);
            CampaignEvents.SettlementEntered.ClearListeners(this);

            InformationManager.DisplayMessage(new InformationMessage(
                "DEBUG: NinthQuest listeners removed after quest completion.", Colors.Yellow));
        }
    }
}