using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using System;
using System.Threading.Tasks;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.ScreenSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Entry point do mod RF_AIDialog.
    ///
    /// Responsabilidades:
    ///   1. Registrar todos os CampaignBehaviors na campanha.
    ///   2. A cada frame (OnApplicationTick):
    ///      a) Verificar mensagens pendentes vindas das tasks de background.
    ///      b) Detectar F8 para abrir o World Chronicle.
    ///
    /// InformationManager.DisplayMessage/ShowInquiry DEVEM ser chamados da thread principal.
    /// </summary>
    public class RF_AIDialogSubModule : MBSubModuleBase
    {
        public static RF_AIDialogSubModule? Instance { get; private set; }
        private AIDialogBehavior? _dialogBehavior;
        private bool _chronicleKeyWasDown = false;
        private bool _debugKeyWasDown = false;
        private bool _voiceHotkeyWasDown = false;
        private bool _voiceInputArmed = false;
        private bool _voiceTranscriptionInFlight = false;
        private readonly RFVoiceCaptureService _voiceCaptureService = new RFVoiceCaptureService();
        private string? _pendingVoiceTranscript = null;
        private string? _pendingVoiceInfoMessage = null;
        private string? _pendingVoiceErrorMessage = null;
        private GauntletLayer? _voiceInputLayer = null;
        private GauntletMovieIdentifier? _voiceInputMovie = null;
        private RFAIVoiceInputPopupVM? _voiceInputPopupVM = null;
        private Action<string>? _voicePopupSubmit = null;
        private Action? _voicePopupCancel = null;
        private int _voiceSessionToken = 0;
        private bool _voicePopupAllowOutsideConversation = false;
        private string? _voicePopupHotkeyOverride = null;

        // ── Ciclo de vida ─────────────────────────────────────────────────

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Instance = this;
            RFAIDebug.Log("OnSubModuleLoad: start");
            AIMemoryStore.EnsureInitialized();
            new Harmony("RF_AIDialog").PatchAll(typeof(RF_AIDialogSubModule).Assembly);
            RFAIDebug.Log("OnSubModuleLoad: OK");
        }

        protected override void OnSubModuleUnloaded()
        {
            try
            {
                HideVoiceInputPopup();
                _voiceCaptureService.Dispose();
                RFAudioPlaybackManager.Instance.Dispose();
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"OnSubModuleUnloaded voice dispose failed: {ex.GetType().Name}: {ex.Message}");
            }
            Instance = null;
            base.OnSubModuleUnloaded();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is Campaign &&
                starterObject is CampaignGameStarter campaignStarter)
            {
                // NPCContextStore must be registered first — everything reads from it
                campaignStarter.AddBehavior(new NPCContextStore());

                // QuestAtomEngine — tracks VISIT_SETTLEMENT, DEFEAT_PARTY, etc. against real game events
                campaignStarter.AddBehavior(new QuestAtomEngine());

                // WorldHistoryStore must be registered before WorldHistoryBehavior
                campaignStarter.AddBehavior(new WorldHistoryStore());
                campaignStarter.AddBehavior(new WorldHistoryBehavior());
                campaignStarter.AddBehavior(new AIMemorySummaryBehavior());
                campaignStarter.AddBehavior(new AIMessageBehavior());

                // NPC initiative — evaluates daily conditions for all lords/notables
                campaignStarter.AddBehavior(new NPCInitiativeBehavior());

                // Betrayal reactions — injects narrative when a clan defects
                campaignStarter.AddBehavior(new BetrayalReactionBehavior());

                // Player reputation — tracks honor, mercy, aggression across the campaign
                campaignStarter.AddBehavior(new PlayerReputationStore());

                // Settlement scars — records conquest history for each fortification
                campaignStarter.AddBehavior(new SettlementScarStore());

                _dialogBehavior = new AIDialogBehavior();
                campaignStarter.AddBehavior(_dialogBehavior);
                _dialogBehavior.AddDialogs(campaignStarter);
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            if (!AIConfig.BattleShoutsEnabled)
                return;

            try
            {
                mission.AddMissionBehavior(new BattleShoutMissionBehavior());
                RFAIDebug.Log("BattleShoutMissionBehavior added to mission.");
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"Failed to add BattleShoutMissionBehavior: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ── Tick principal (toda frame, thread principal) ─────────────────

        protected override void OnApplicationTick(float dt)
        {
            // Drain facial-animation / lip-sync engine calls queued from audio
            // background threads (must run on the main thread).
            RFAudioPlaybackManager.PumpMainThread();

            // ── AI response notification ──────────────────────────────────
            if (_dialogBehavior != null && _dialogBehavior.ResponseJustArrived)
            {
                _dialogBehavior.ResponseJustArrived = false;

                var cm = Campaign.Current?.ConversationManager;
                if (cm != null && cm.IsConversationInProgress)
                {
                    string npcName = _dialogBehavior.CurrentNpcName;
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            $"💬 {npcName} is ready to respond. Click '...' to hear the reply.",
                            Color.FromUint(0xFF_A0_D0_FFu)));
                }
            }

            FlushPendingVoiceResults();

            // ── F8 — World Chronicle ──────────────────────────────────────
            try
            {
                HandleDebugMenuHotkey();
                HandleVoiceInputHotkey();

                bool keyDown = Input.IsKeyDown(InputKey.F8);

                // Rising edge only — fire once per press, not while held
                if (keyDown && !_chronicleKeyWasDown)
                {
                    // Only open on the campaign map, not mid-conversation or battle
                    bool inConversation = Campaign.Current?.ConversationManager
                                             ?.IsConversationInProgress ?? false;
                    if (Campaign.Current != null && !inConversation)
                        ShowChronicle();
                }

                _chronicleKeyWasDown = keyDown;
            }
            catch { }
        }

        // ── World Chronicle popup ─────────────────────────────────────────

        private void HandleDebugMenuHotkey()
        {
            if (!AIConfig.DebugMenuEnabled)
                return;

            bool modifierDown = Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
            bool keyDown = modifierDown && Input.IsKeyDown(InputKey.F9);

            if (keyDown && !_debugKeyWasDown)
                RFAIDebugMenu.Show();

            _debugKeyWasDown = keyDown;
        }

        internal void ArmVoiceInput()
        {
            _voiceInputArmed = true;
            _voiceTranscriptionInFlight = false;
            _voiceHotkeyWasDown = false;
            _pendingVoiceTranscript = null;
            _voiceSessionToken++;
            _voiceInputPopupVM?.SetRecordingState(isRecording: false);
            string hotkey = ResolveActiveVoiceHotkeyName();
            InformationManager.DisplayMessage(new InformationMessage(
                $"[AI Voice Input] Click Record to speak. Hotkey {hotkey} also works.",
                Color.FromUint(0xFF_A0_D0_FFu)));
        }

        internal void DisarmVoiceInput()
        {
            _voiceSessionToken++;
            _voiceInputArmed = false;
            _voiceHotkeyWasDown = false;
            _pendingVoiceTranscript = null;
            _pendingVoiceInfoMessage = null;
            _pendingVoiceErrorMessage = null;
            _voicePopupAllowOutsideConversation = false;
            _voicePopupHotkeyOverride = null;
            try
            {
                if (_voiceCaptureService.IsRecording)
                    _voiceCaptureService.StopRecording();
            }
            catch { }
        }

        internal void ShowVoiceInputPopup(
            string npcName,
            string titleText,
            string promptText,
            Action<string> onSubmit,
            Action onCancel,
            bool allowOutsideConversation = false,
            string? hotkeyOverride = null)
        {
            _voicePopupSubmit = onSubmit;
            _voicePopupCancel = onCancel;
            _voicePopupAllowOutsideConversation = allowOutsideConversation;
            _voicePopupHotkeyOverride = hotkeyOverride;

            if (_voiceInputPopupVM == null)
            {
                _voiceInputPopupVM = new RFAIVoiceInputPopupVM(
                    titleText,
                    promptText,
                    string.Empty,
                    SubmitVoicePopupText,
                    CancelVoicePopup,
                    ToggleVoiceRecordingFromPopup);
            }
            else
            {
                _voiceInputPopupVM.TitleText = titleText;
                _voiceInputPopupVM.PromptText = promptText;
                _voiceInputPopupVM.InputText = string.Empty;
                _voiceInputPopupVM.SetHotkeyHint(hotkeyOverride ?? AIConfig.STTHotkey);
                _voiceInputPopupVM.SetRecordingState(isRecording: false);
            }

            _voiceInputPopupVM.SetHotkeyHint(hotkeyOverride ?? AIConfig.STTHotkey);

            if (ScreenManager.TopScreen == null)
            {
                RFAIDebug.Log($"ShowVoiceInputPopup: TopScreen null for {npcName}");
                ArmVoiceInput();
                return;
            }

            if (_voiceInputLayer == null)
            {
                _voiceInputLayer = new GauntletLayer("RFAIVoiceInputPopup", 19520, true);
                try
                {
                    _voiceInputLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
                }
                catch
                {
                    try
                    {
                        _voiceInputLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
                    }
                    catch { }
                }
                _voiceInputLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                _voiceInputLayer.IsFocusLayer = true;
            }

            if (_voiceInputMovie == null)
                _voiceInputMovie = _voiceInputLayer.LoadMovie("RFAIVoiceInputPopup", _voiceInputPopupVM);

            if (!ScreenManager.TopScreen.Layers.Contains(_voiceInputLayer))
                ScreenManager.TopScreen.AddLayer(_voiceInputLayer);

            _voiceInputLayer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_voiceInputLayer);
            ArmVoiceInput();
        }

        private void HideVoiceInputPopup()
        {
            if (_voiceInputLayer == null)
                return;

            try
            {
                _voiceInputLayer.InputRestrictions.ResetInputRestrictions();
                _voiceInputLayer.IsFocusLayer = false;
                if (_voiceInputMovie != null)
                {
                    _voiceInputLayer.ReleaseMovie(_voiceInputMovie);
                    _voiceInputMovie = null;
                }

                if (ScreenManager.TopScreen != null)
                {
                    ScreenManager.TryLoseFocus(_voiceInputLayer);
                    ScreenManager.TopScreen.RemoveLayer(_voiceInputLayer);
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"HideVoiceInputPopup failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _voiceInputLayer = null;
                _voiceInputPopupVM = null;
                _voicePopupSubmit = null;
                _voicePopupCancel = null;
                _voicePopupAllowOutsideConversation = false;
                _voicePopupHotkeyOverride = null;
            }
        }

        internal void CloseVoiceInputPopup()
        {
            DisarmVoiceInput();
            HideVoiceInputPopup();
        }

        private void SubmitVoicePopupText(string text)
        {
            string normalized = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            Action<string>? submit = _voicePopupSubmit;
            DisarmVoiceInput();
            HideVoiceInputPopup();
            submit?.Invoke(normalized);
        }

        private void CancelVoicePopup()
        {
            Action? cancel = _voicePopupCancel;
            DisarmVoiceInput();
            HideVoiceInputPopup();
            cancel?.Invoke();
        }

        private void ToggleVoiceRecordingFromPopup()
        {
            if (_voiceTranscriptionInFlight)
                return;

            if (_voiceCaptureService.IsRecording)
                StopVoiceRecordingAndTranscribe();
            else
                StartVoiceRecording();
        }

        private void HandleVoiceInputHotkey()
        {
            if (!_voiceInputArmed || !AIConfig.STTEnabled)
                return;

            bool inConversation = Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false;
            bool allowWithoutConversation = _voicePopupAllowOutsideConversation && _voiceInputPopupVM != null;
            if (!inConversation && !allowWithoutConversation)
            {
                HideVoiceInputPopup();
                _voiceInputArmed = false;
                _voiceHotkeyWasDown = false;
                if (_voiceCaptureService.IsRecording)
                    _voiceCaptureService.StopRecording();
                return;
            }

            InputKey? hotkey = ResolveSttHotkey();
            if (!hotkey.HasValue)
                return;

            bool keyDown = Input.IsKeyDown(hotkey.Value);

            if (keyDown && !_voiceHotkeyWasDown && !_voiceTranscriptionInFlight)
                StartVoiceRecording();

            if (!keyDown && _voiceHotkeyWasDown && _voiceCaptureService.IsRecording && !_voiceTranscriptionInFlight)
                StopVoiceRecordingAndTranscribe();

            _voiceHotkeyWasDown = keyDown;
        }

        private void StartVoiceRecording()
        {
            try
            {
                _voiceCaptureService.StartRecording();
                _voiceInputPopupVM?.SetRecordingState(isRecording: true);
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI Voice Input] Recording...",
                    Color.FromUint(0xFF_A0_D0_FFu)));
            }
            catch (Exception ex)
            {
                _voiceInputArmed = false;
                _voiceInputPopupVM?.SetError($"Could not start microphone: {ex.Message}");
                RFAIDebug.Log($"StartVoiceRecording failed: {ex.GetType().Name}: {ex.Message}");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[AI Voice Input] Could not start microphone: {ex.Message}",
                    Color.FromUint(0xFF_FF_80_80u)));
            }
        }

        private void StopVoiceRecordingAndTranscribe()
        {
            byte[] wavBytes = _voiceCaptureService.StopRecording();
            _voiceTranscriptionInFlight = true;
            int sessionToken = _voiceSessionToken;
            _voiceInputPopupVM?.SetTranscribingState();
            InformationManager.DisplayMessage(new InformationMessage(
                "[AI Voice Input] Transcribing...",
                Color.FromUint(0xFF_A0_D0_FFu)));

            _ = Task.Run(async () =>
            {
                try
                {
                    string transcript = await AIClient.TranscribeSpeechAsync(
                        wavBytes,
                        "Fantasy setting. Medieval names, places, and factions from Aeurth, Calradia, and Realms Forgotten.")
                        .ConfigureAwait(false);

                    if (sessionToken != _voiceSessionToken)
                        return;

                    if (string.IsNullOrWhiteSpace(transcript))
                    {
                        _pendingVoiceErrorMessage = "[AI Voice Input] I could not hear anything clearly.";
                        return;
                    }

                    _pendingVoiceTranscript = transcript;
                }
                catch (Exception ex)
                {
                    if (sessionToken != _voiceSessionToken)
                        return;

                    RFAIDebug.Log($"StopVoiceRecordingAndTranscribe failed: {ex.GetType().Name}: {ex.Message}");
                    _pendingVoiceErrorMessage = $"[AI Voice Input] Transcription failed: {ex.Message}";
                }
                finally
                {
                    if (sessionToken == _voiceSessionToken)
                        _voiceTranscriptionInFlight = false;
                }
            });
        }

        private static InputKey? ResolveSttHotkey()
        {
            try
            {
                string hotkeyName = Instance?.ResolveActiveVoiceHotkeyName() ?? AIConfig.STTHotkey;
                if (Enum.TryParse(hotkeyName, true, out InputKey parsed))
                    return parsed;
            }
            catch { }

            return InputKey.M;
        }

        private string ResolveActiveVoiceHotkeyName()
        {
            return string.IsNullOrWhiteSpace(_voicePopupHotkeyOverride)
                ? AIConfig.STTHotkey
                : _voicePopupHotkeyOverride!.Trim();
        }

        private void FlushPendingVoiceResults()
        {
            if (!string.IsNullOrWhiteSpace(_pendingVoiceErrorMessage))
            {
                _voiceInputPopupVM?.SetError(_pendingVoiceErrorMessage);
                InformationManager.DisplayMessage(new InformationMessage(
                    _pendingVoiceErrorMessage,
                    Color.FromUint(0xFF_FF_80_80u)));
                _pendingVoiceErrorMessage = null;
            }

            if (!string.IsNullOrWhiteSpace(_pendingVoiceInfoMessage))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    _pendingVoiceInfoMessage,
                    Color.FromUint(0xFF_A0_D0_FFu)));
                _pendingVoiceInfoMessage = null;
            }

            if (!string.IsNullOrWhiteSpace(_pendingVoiceTranscript) && _dialogBehavior != null)
            {
                string transcript = _pendingVoiceTranscript;
                _pendingVoiceTranscript = null;
                if (_voiceInputPopupVM != null)
                    _voiceInputPopupVM.SetTranscript(transcript);
                else
                {
                    _dialogBehavior.ReviewVoiceTranscript(transcript);
                    _voiceInputArmed = false;
                }
            }
        }

        private static void ShowChronicle()
        {
            try
            {
                var store = WorldHistoryStore.Instance;
                string text;

                if (store == null)
                {
                    text = "The chronicle is not available.";
                }
                else
                {
                    // Show all recorded events (no day cap), newest last
                    var events = store.GetRecentEvents(maxCount: 40, maxDays: 0);

                    if (events.Count == 0)
                    {
                        text = "No significant events have been recorded yet.\n\n" +
                               "The chronicle will fill as wars are declared, settlements " +
                               "change hands, and kingdoms rise and fall.";
                    }
                    else
                    {
                        var sb = new StringBuilder();
                        foreach (var e in events)
                            sb.AppendLine($"[Day {e.Day}]  {e.Description}");
                        text = sb.ToString().TrimEnd();
                    }
                }

                InformationManager.ShowInquiry(new InquiryData(
                    titleText:                "📜 World Chronicle",
                    text:                     text,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown:    false,
                    affirmativeText:          "Close",
                    negativeText:             "",
                    affirmativeAction:        () => { },
                    negativeAction:           null));
            }
            catch { }
        }
    }
}
