using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RF_AIDialog
{
    /// <summary>
    /// Injects a "Speak freely... [AI]" option into dialogue with any Hero.
    ///
    /// Flow:
    ///   1. Player clicks "Speak freely... [AI]"
    ///   2. Native Bannerlord text input box appears (ShowTextInquiry)
    ///   3. Player types and confirms
    ///   4. LLM processes in background, returning structured JSON
    ///   5. HUD notification: "[NPC] is ready to respond. Click '...' to hear the reply."
    ///   6. Player clicks '...' -> NPC response appears in the dialogue box
    ///   7. Exchange is saved to NPCContext for continuity in future conversations
    ///
    /// Request lifecycle:
    ///   - If the LLM returns a "request" field and no request is pending, CommitNewRequest() fires.
    ///   - If a request is already pending, the new text is held in _pendingNewRequest and the NPC
    ///     surfaces a confirmation dialog ("replace my old request?") before the player commits.
    ///   - On fulfillment the quest log entry is closed and NPCContext.PendingRequest is cleared.
    /// </summary>
    public class AIDialogBehavior : CampaignBehaviorBase
    {
        // Singleton — used by the CampaignBehaviorManager Harmony prefix so it can
        // call PrehideBeforeBehaviorSave() before behavior SyncData is stored.
        public static AIDialogBehavior? Instance { get; private set; }

        // State
        private volatile bool        _isWaiting          = false;
        private volatile string?     _rawResponse        = null;
        private          RFAIResponse? _parsed           = null;

        public volatile bool ResponseJustArrived = false;
        private bool _responseAudioQueued = false;
        private static bool _ttsDisclosureShown = false;
        private bool _voiceCancelPending = false;

        private Hero?       _currentNpc      = null;
        private string      _npcNameText     = "";
        private string      _lastPlayerMsg   = "";
        private NPCContext? _currentContext  = null;

        // When non-null, the LLM issued a new request while a previous one is pending.
        // The NPC will ask the player to choose. _currentNpc/_currentContext stay alive
        // until the choice is made.
        private string?        _pendingNewRequest  = null;
        private QuestMechanic? _pendingNewMechanic = null;

        public string CurrentNpcName => _npcNameText;

        // Quests removed before save, restored after — so the type never hits disk.
        private List<QuestBase>    _questsHiddenForSave      = new List<QuestBase>();
        // Subset that were also in _trackedObjects — only these go back there on restore.
        private HashSet<QuestBase> _questsRemovedFromTracked = new HashSet<QuestBase>();
        // _questSelection saved from ViewDataTrackerCampaignBehavior before save.
        private QuestBase?         _savedQuestSelection      = null;
        // True once PrehideBeforeBehaviorSave has run for the current save cycle.
        private bool               _prehideDone              = false;

        public override void RegisterEvents()
        {
            Instance = this;
            // Reconstruct quest log entries after load (quest is never serialized).
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this, OnGameLoadFinished);

            // Remove AIDialogQuest from QuestManager._quests before the save system
            // serializes it — the type is not registered in any SaveableTypeDefiner,
            // and Bannerlord 1.3.0 crashes when it encounters an unknown QuestBase type.
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(
                this, HideQuestsBeforeSave);

            // Restore the quests immediately after the save file is written.
            CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(
                this, RestoreQuestsAfterSave);
        }

        // Helper: resolve _quests MBList via reflection (Quests property returns MBReadOnlyList)
        private static MBList<QuestBase> GetQuestsList(QuestManager qm)
        {
            var fi = typeof(QuestManager).GetField("_quests",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return fi?.GetValue(qm) as MBList<QuestBase>;
        }

        // Helper: resolve _trackedObjects via reflection
        private static Dictionary<ITrackableCampaignObject, List<QuestBase>> GetTrackedObjects(QuestManager qm)
        {
            var fi = typeof(QuestManager).GetField("_trackedObjects",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return fi?.GetValue(qm) as Dictionary<ITrackableCampaignObject, List<QuestBase>>;
        }

        // ── Save hide/restore ─────────────────────────────────────────────

        /// <summary>
        /// Called from the Harmony prefix on CampaignBehaviorManager.OnBeforeSave,
        /// which fires BEFORE behavior SyncData is stored. This guarantees that
        /// AIDialogQuest is invisible both to QuestManager and to
        /// ViewDataTrackerCampaignBehavior._questSelection when the serializer
        /// traverses BehaviorSaveData._records.
        /// </summary>
        internal void PrehideBeforeBehaviorSave()
        {
            _questsHiddenForSave.Clear();
            _questsRemovedFromTracked.Clear();
            _savedQuestSelection = null;
            _prehideDone         = false;

            // 1. Remove AIDialogQuests from QuestManager._quests + _trackedObjects.
            try
            {
                var qm = Campaign.Current?.QuestManager;
                if (qm == null)
                {
                    RFAIDebug.Log("PrehideBeforeBehaviorSave: QuestManager null");
                }
                else
                {
                    var quests = GetQuestsList(qm);
                    if (quests == null)
                    {
                        RFAIDebug.Log("PrehideBeforeBehaviorSave: cannot access _quests");
                    }
                    else
                    {
                        for (int i = quests.Count - 1; i >= 0; i--)
                        {
                            if (quests[i] is AIDialogQuest aq)
                            {
                                _questsHiddenForSave.Add(aq);
                                quests.RemoveAt(i);
                            }
                        }

                        var tracked = GetTrackedObjects(qm);
                        if (tracked != null)
                        {
                            foreach (var aq in _questsHiddenForSave)
                            {
                                if (aq.QuestGiver == null) continue;
                                if (tracked.TryGetValue(aq.QuestGiver, out var list) && list.Contains(aq))
                                {
                                    _questsRemovedFromTracked.Add(aq);
                                    list.Remove(aq);
                                    if (list.Count == 0)
                                        tracked.Remove(aq.QuestGiver);
                                }
                            }
                        }

                        int trackedCount = GetTrackedObjects(qm)?.Count ?? -1;
                        RFAIDebug.Log($"PrehideBeforeBehaviorSave: hid {_questsHiddenForSave.Count}, " +
                                      $"remaining {quests.Count}, trackedObjs {trackedCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"PrehideBeforeBehaviorSave: QM exception: {ex.GetType().Name}: {ex.Message}");
            }

            // 2. Clear _questSelection in ViewDataTrackerCampaignBehavior so it is
            //    null when SyncData stores it into BehaviorSaveData._records.
            try
            {
                var viewTracker = Campaign.Current?.GetCampaignBehavior<IViewDataTracker>();
                if (viewTracker != null)
                {
                    var sel = viewTracker.GetQuestSelection();
                    if (sel is AIDialogQuest)
                    {
                        _savedQuestSelection = sel;
                        viewTracker.SetQuestSelection(null);
                        RFAIDebug.Log("PrehideBeforeBehaviorSave: cleared _questSelection");
                    }
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"PrehideBeforeBehaviorSave: questSel exception: {ex.Message}");
            }

            _prehideDone = true;
        }

        private void HideQuestsBeforeSave()
        {
            // PrehideBeforeBehaviorSave() already ran via Harmony prefix on
            // CampaignBehaviorManager.OnBeforeSave (which fires before this listener).
            // Fall through only if the prefix somehow didn't run.
            if (_prehideDone)
            {
                RFAIDebug.Log("HideQuestsBeforeSave: prehide already done, skip");
                return;
            }
            RFAIDebug.Log("HideQuestsBeforeSave: prehide not done — running now (fallback)");
            PrehideBeforeBehaviorSave();
        }

        private void RestoreQuestsAfterSave(bool success, string saveName)
        {
            _prehideDone = false;
            try
            {
                // Restore _questSelection regardless of QuestManager state.
                if (_savedQuestSelection != null)
                {
                    try
                    {
                        Campaign.Current?.GetCampaignBehavior<IViewDataTracker>()
                                        ?.SetQuestSelection(_savedQuestSelection);
                        RFAIDebug.Log("RestoreQuestsAfterSave: restored questSelection");
                    }
                    catch { }
                    _savedQuestSelection = null;
                }

                var qm = Campaign.Current?.QuestManager;
                if (qm == null)
                {
                    RFAIDebug.Log($"RestoreQuestsAfterSave: QuestManager null (success={success})");
                    _questsHiddenForSave.Clear();
                    _questsRemovedFromTracked.Clear();
                    return;
                }

                var quests = GetQuestsList(qm);
                if (quests == null) { RFAIDebug.Log("RestoreQuestsAfterSave: cannot access _quests"); return; }

                // Restore to _quests
                foreach (var q in _questsHiddenForSave)
                    quests.Add(q);

                // Restore only quests that were originally in _trackedObjects
                var tracked = GetTrackedObjects(qm);
                if (tracked != null)
                {
                    foreach (var aq in _questsHiddenForSave)
                    {
                        if (!_questsRemovedFromTracked.Contains(aq)) continue;
                        if (aq.QuestGiver == null) continue;
                        if (!tracked.ContainsKey(aq.QuestGiver))
                            tracked[aq.QuestGiver] = new List<QuestBase>();
                        if (!tracked[aq.QuestGiver].Contains(aq))
                            tracked[aq.QuestGiver].Add(aq);
                    }
                }

                RFAIDebug.Log($"RestoreQuestsAfterSave: restored {_questsHiddenForSave.Count} quests, " +
                              $"{_questsRemovedFromTracked.Count} tracked (success={success})");
                _questsHiddenForSave.Clear();
                _questsRemovedFromTracked.Clear();
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"RestoreQuestsAfterSave exception: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnGameLoadFinished()
        {
            RFAIDebug.Log("OnGameLoadFinished: fired");
            ReconstructQuestsFromNPCContexts();
        }

        private void ReconstructQuestsFromNPCContexts()
        {
            // AIDialogQuest has no SaveableTypeDefiner — comes back null on load,
            // pruned by QuestManager.PreAfterLoad. Rebuild from NPCContext.PendingRequest.
            //
            // _activeByNpcId is static and survives across game loads. Clear it first —
            // stale entries from the previous session would make ForNpc() return a dead
            // quest object and cause the "existing != null && existing.IsOngoing" guard
            // to skip recreation entirely.
            AIDialogQuest.ClearAll();
            RFAIDebug.Log("ReconstructQuestsFromNPCContexts: start");
            try
            {
                var store = NPCContextStore.Instance;
                if (store == null) { RFAIDebug.Log("store null"); return; }

                int currentDay = 0;
                try { currentDay = (int)Campaign.Current.Models.CampaignTimeModel
                                            .CampaignStartTime.ElapsedDaysUntilNow; } catch { }

                foreach (var ctx in store.GetAll())
                {
                    if (!ctx.HasPendingRequest && (ctx.CompletedRequests == null || ctx.CompletedRequests.Count == 0)) continue;

                    Hero? hero = null;
                    try { hero = Hero.FindFirst(h => h.StringId == ctx.HeroId); } catch { }
                    if (hero == null || hero.IsDead) continue;

                    if (!ctx.HasPendingRequest)
                    {
                        ReconstructCompletedQuests(hero, ctx);
                        continue;
                    }

                    var existing = AIDialogQuest.ForNpc(ctx.HeroId);
                    if (existing != null && existing.IsOngoing) continue;

                    try
                    {
                        var req          = ctx.PendingRequest!;
                        int totalDays    = req.Mechanic?.DurationDays > 0 ? req.Mechanic.DurationDays : 30;
                        int elapsed      = currentDay - req.DayIssued;
                        int daysLeft     = Math.Max(1, totalDays - elapsed);
                        string qId       = $"rfai_{hero.StringId}_{req.DayIssued}";
                        RFAIDebug.Log($"ReconstructQuestsFromNPCContexts: recreating {hero.Name} ({daysLeft}d)");
                        var quest = new AIDialogQuest(qId, hero, req.Description, daysLeft);
                        quest.StartQuest();

                        // Re-add objective bullets so the journal stays current.
                        if (req.Mechanic != null)
                        {
                            req.Mechanic.Normalize();
                            for (int i = 0; i < req.Mechanic.Objectives.Count; i++)
                            {
                                var atom  = req.Mechanic.Objectives[i];
                                bool done = req.Mechanic.IsCompleted(i);
                                if (!string.IsNullOrWhiteSpace(atom.Label))
                                    quest.AddObjectiveLog(done ? $"✓ {atom.Label}" : $"○ {atom.Label}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RFAIDebug.Log($"ReconstructQuestsFromNPCContexts: failed {ctx.HeroId} — {ex.Message}");
                    }
                }
                RFAIDebug.Log("ReconstructQuestsFromNPCContexts: done");
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"ReconstructQuestsFromNPCContexts: outer — {ex.Message}");
            }
        }

        private static void ReconstructCompletedQuests(Hero hero, NPCContext ctx)
        {
            if (ctx.CompletedRequests == null || ctx.CompletedRequests.Count == 0)
                return;

            foreach (var completed in ctx.CompletedRequests)
            {
                if (completed == null || string.IsNullOrWhiteSpace(completed.Description))
                    continue;

                try
                {
                    string qId = $"rfai_done_{hero.StringId}_{completed.DayIssued}_{completed.DayCompleted}";
                    var quest = new AIDialogQuest(qId, hero, completed.Description, 36500, resolvedDisplay: true);
                    quest.StartQuest();
                }
                catch (Exception ex)
                {
                    RFAIDebug.Log($"ReconstructCompletedQuests: failed {hero.StringId} - {ex.Message}");
                }
            }
        }

        public void AddDialogs(CampaignGameStarter starter)
        {
            MBTextManager.SetTextVariable("RF_AI_RESPONSE",    new TextObject("{=!}..."));
            MBTextManager.SetTextVariable("RF_AI_WAIT_TEXT",   new TextObject("{=!}..."));
            MBTextManager.SetTextVariable("RF_AI_OLD_REQUEST", new TextObject("{=!}..."));

            // 0. NPC initiative trigger
            starter.AddPlayerLine(
                "rf_ai_initiative_trigger",
                "hero_main_options",
                "rf_ai_npc_thinking",
                "You seem like you have something on your mind... [AI]",
                ConditionHasPendingInitiative,
                ConsequenceTriggerInitiative,
                150, null);

            // 1. Player menu option
            starter.AddPlayerLine(
                "rf_ai_open_input",
                "hero_main_options",
                "rf_ai_npc_thinking",
                "Speak freely... [AI]",
                ConditionCanUseAI,
                ConsequenceOpenTextInput,
                100, null);

            starter.AddPlayerLine(
                "rf_ai_open_voice_input",
                "hero_main_options",
                "rf_ai_npc_thinking",
                "Speak with voice... [AI]",
                ConditionCanUseVoiceAI,
                ConsequenceArmVoiceInput,
                125, null);

            // 2. NPC thinking state
            starter.AddDialogLine(
                "rf_ai_npc_thinking_line",
                "rf_ai_npc_thinking",
                "rf_ai_player_wait",
                "[Thoughtful] Give me a moment to choose my words...",
                null, null, 100, null);

            // 3. Wait option
            starter.AddPlayerLine(
                "rf_ai_player_waiting",
                "rf_ai_player_wait",
                "rf_ai_player_wait_result",
                "{RF_AI_WAIT_TEXT}",
                ConditionUpdateWaitText,
                null, 100, null);

            // 3a. Still waiting
            starter.AddDialogLine(
                "rf_ai_still_waiting_line",
                "rf_ai_player_wait_result",
                "rf_ai_player_wait",
                "[Thoughtful] Hmm...",
                ConditionIsStillWaiting,
                null, 100, null);

            starter.AddDialogLine(
                "rf_ai_voice_cancel_return",
                "rf_ai_player_wait_result",
                "hero_main_options",
                "Very well.",
                ConditionVoiceCancelPending,
                ConsequenceVoiceCancelReturn,
                150, null);

            // 3b. Response ready
            starter.AddDialogLine(
                "rf_ai_npc_response_line",
                "rf_ai_player_wait_result",
                "rf_ai_after_response",
                "{RF_AI_RESPONSE}",
                ConditionShowResponse,
                ConsequenceClearResponse,
                200, null);

            // 4. Replace-request offer (higher priority than player options below)
            starter.AddDialogLine(
                "rf_ai_replace_request_offer",
                "rf_ai_after_response",
                "rf_ai_replace_options",
                "Before you go — I must also tell you: I already asked something of you ({RF_AI_OLD_REQUEST}). " +
                "Shall we set that matter aside and have you pursue this new one instead?",
                ConditionHasPendingNewRequest,
                null,
                300, null);

            // 4a. Accept replacing
            starter.AddPlayerLine(
                "rf_ai_replace_accept",
                "rf_ai_replace_options",
                "rf_ai_after_response",
                "Very well — let us focus on your new request. The old one can wait.",
                null,
                ConsequenceAcceptNewRequest,
                200, null);

            // 4b. Decline replacing
            starter.AddPlayerLine(
                "rf_ai_replace_decline",
                "rf_ai_replace_options",
                "rf_ai_after_response",
                "No, I will honour the original agreement first. We can return to this later.",
                null,
                ConsequenceDeclineNewRequest,
                100, null);

            // 5a. Continue speaking
            starter.AddPlayerLine(
                "rf_ai_continue",
                "rf_ai_after_response",
                "rf_ai_npc_thinking",
                "Continue speaking... [AI]",
                ConditionCanUseAI,
                ConsequenceOpenTextInput,
                200, null);

            starter.AddPlayerLine(
                "rf_ai_continue_voice",
                "rf_ai_after_response",
                "rf_ai_npc_thinking",
                "Continue speaking with voice... [AI]",
                ConditionCanUseVoiceAI,
                ConsequenceArmVoiceInput,
                175, null);

            // 5b. End conversation
            starter.AddPlayerLine(
                "rf_ai_end",
                "rf_ai_after_response",
                "close_window",
                "That will be all.",
                null, null,
                100, null);
        }

        // Conditions

        private bool ConditionCanUseAI()
        {
            QuestAtomEngine.Instance?.CaptureConversationTarget();
            return Hero.OneToOneConversationHero != null && !_isWaiting;
        }

        private bool ConditionCanUseVoiceAI()
        {
            QuestAtomEngine.Instance?.CaptureConversationTarget();
            return AIConfig.STTEnabled && Hero.OneToOneConversationHero != null && !_isWaiting;
        }

        private bool ConditionHasPendingInitiative()
        {
            QuestAtomEngine.Instance?.CaptureConversationTarget();
            var npc = Hero.OneToOneConversationHero;
            if (npc == null || _isWaiting) return false;
            var ctx = NPCContextStore.Instance?.GetOrCreate(npc);
            return ctx != null && ctx.HasPendingInitiative;
        }

        private bool ConditionIsStillWaiting()
            => _isWaiting || _rawResponse == null;

        private bool ConditionUpdateWaitText()
        {
            string waitText = (_isWaiting || _rawResponse == null)
                ? "..."
                : "(Response ready - click here)";
            MBTextManager.SetTextVariable("RF_AI_WAIT_TEXT", new TextObject("{=!}" + waitText));
            return true;
        }

        private bool ConditionVoiceCancelPending()
            => _voiceCancelPending && !_isWaiting;

        private bool ConditionShowResponse()
        {
            if (_isWaiting || _rawResponse == null) return false;

            if (_parsed == null)
                _parsed = ParseResponse(_rawResponse);

            string text;
            if (!string.IsNullOrWhiteSpace(_parsed?.Response))
            {
                text = _parsed!.Response;
            }
            else if (_parsed == null
                     && _rawResponse != null
                     && !_rawResponse.TrimStart().StartsWith("{")
                     && !_rawResponse.TrimStart().StartsWith("`"))
            {
                text = _rawResponse;
            }
            else
            {
                text = "Hmm... the words escape me for the moment.";
            }

            TryQueueResponseAudio(text);
            MBTextManager.SetTextVariable("RF_AI_RESPONSE", new TextObject("{=!}" + Sanitize(text)));
            return true;
        }

        // Returns true when LLM issued a new request but one is already pending.
        // Also populates RF_AI_OLD_REQUEST for display.
        private bool ConditionHasPendingNewRequest()
        {
            if (_pendingNewRequest == null) return false;

            string oldDesc = _currentContext?.PendingRequest?.Description ?? "";
            if (oldDesc.Length > 120) oldDesc = oldDesc.Substring(0, 120) + "...";
            MBTextManager.SetTextVariable("RF_AI_OLD_REQUEST",
                new TextObject("{=!}" + Sanitize(oldDesc)));

            return true;
        }

        // Consequences

        private void ConsequenceOpenTextInput()
        {
            _currentNpc     = Hero.OneToOneConversationHero;
            _npcNameText    = _currentNpc?.Name.ToString() ?? "?";
            _currentContext = _currentNpc != null
                ? NPCContextStore.Instance?.GetOrCreate(_currentNpc)
                : null;
            SanitizePendingRequestForCurrentNpc();

            // Check inventory/troop atoms the moment the player speaks to this NPC.
            if (_currentNpc != null && _currentContext != null)
                QuestAtomEngine.Instance?.CheckConversationAtoms(_currentNpc, _currentContext);

            ShowPlayerInputInquiry("What do you wish to say?");
        }

        private void ConsequenceTriggerInitiative()
        {
            _currentNpc     = Hero.OneToOneConversationHero;
            _npcNameText    = _currentNpc?.Name.ToString() ?? "?";
            _currentContext = _currentNpc != null
                ? NPCContextStore.Instance?.GetOrCreate(_currentNpc)
                : null;
            SanitizePendingRequestForCurrentNpc();

            // Check inventory/troop atoms for NPC-initiated conversations too.
            if (_currentNpc != null && _currentContext != null)
                QuestAtomEngine.Instance?.CheckConversationAtoms(_currentNpc, _currentContext);

            OnPlayerConfirmedInput("(You give the NPC your attention, inviting them to speak first.)");
        }

        private void ConsequenceArmVoiceInput()
        {
            _currentNpc     = Hero.OneToOneConversationHero;
            _npcNameText    = _currentNpc?.Name.ToString() ?? "?";
            _currentContext = _currentNpc != null
                ? NPCContextStore.Instance?.GetOrCreate(_currentNpc)
                : null;
            SanitizePendingRequestForCurrentNpc();

            if (_currentNpc != null && _currentContext != null)
                QuestAtomEngine.Instance?.CheckConversationAtoms(_currentNpc, _currentContext);

            RF_AIDialogSubModule.Instance?.ShowVoiceInputPopup(
                _npcNameText,
                $"Speak with {_npcNameText}",
                "Use the microphone button to dictate, or type directly into the field below.",
                SubmitVoiceTranscript,
                OnVoiceInputCancelled);
        }

        private void OnPlayerConfirmedInput(string playerText)
        {
            if (string.IsNullOrWhiteSpace(playerText))
            {
                _rawResponse = "(Empty message - please try again)";
                return;
            }

            _isWaiting     = true;
            _rawResponse   = null;
            _parsed        = null;
            _responseAudioQueued = false;
            _lastPlayerMsg = playerText;

            Hero?       npc = _currentNpc;
            NPCContext? ctx = _currentContext;
            RFAIDebug.Log($"OnPlayerConfirmedInput: npc={npc?.StringId ?? "null"} | first={(ctx == null || ctx.IsFirstConversation)} | pending={ctx?.HasPendingRequest ?? false}");

            string systemPrompt;
            try   { systemPrompt = PromptBuilder.Build(npc!, ctx, playerText); }
            catch { systemPrompt = FallbackPrompt(); }

            Task.Run(async () =>
            {
                try
                {
                    bool isFirst = ctx == null || ctx.IsFirstConversation;
                    int maxTok   = isFirst
                        ? AIConfig.MaxTokensFirstConversation
                        : AIConfig.MaxTokensSubsequent;

                    _rawResponse = await AIClient.AskAsync(
                        AIConfig.ModelName,
                        systemPrompt,
                        playerText,
                        maxTok).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _rawResponse = $"(AI Error: {ex.Message})";
                    RFAIDebug.Log($"OnPlayerConfirmedInput: AI exception for {npc?.StringId ?? "null"} | {ex.Message}");
                }
                finally
                {
                    _isWaiting          = false;
                    ResponseJustArrived = true;
                }
            });
        }

        private void OnPlayerCancelledInput()
        {
            _rawResponse = "[cancel]";
            _isWaiting   = false;
            _responseAudioQueued = false;
            _voiceCancelPending = false;
            RF_AIDialogSubModule.Instance?.DisarmVoiceInput();
        }

        private void OnVoiceInputCancelled()
        {
            _rawResponse = null;
            _isWaiting = false;
            _responseAudioQueued = false;
            _voiceCancelPending = true;
            RF_AIDialogSubModule.Instance?.DisarmVoiceInput();
            try
            {
                if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
                    Campaign.Current.ConversationManager.ContinueConversation();
            }
            catch { }
        }

        private void ConsequenceVoiceCancelReturn()
        {
            _voiceCancelPending = false;
            _rawResponse = null;
            _isWaiting = false;
            _responseAudioQueued = false;
        }

        public void ReviewVoiceTranscript(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return;

            if (_currentNpc == null)
            {
                _currentNpc = Hero.OneToOneConversationHero;
                _npcNameText = _currentNpc?.Name.ToString() ?? "?";
            }

            if (_currentNpc != null && _currentContext == null)
                _currentContext = NPCContextStore.Instance?.GetOrCreate(_currentNpc);

            SanitizePendingRequestForCurrentNpc();

            if (_currentNpc != null && _currentContext != null)
                QuestAtomEngine.Instance?.CheckConversationAtoms(_currentNpc, _currentContext);

            InformationManager.HideInquiry();
            ShowPlayerInputInquiry(
                "Review your spoken words before sending.",
                transcript.Trim());
        }

        public void SubmitVoiceTranscript(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return;

            OnPlayerConfirmedInput(transcript.Trim());
        }

        private void ConsequenceClearResponse()
        {
            RFAIDebug.Log($"ConsequenceClearResponse: parsedNull={_parsed == null} | npc={_currentNpc?.StringId ?? "null"} | rawPreview={(_rawResponse ?? "").Substring(0, Math.Min((_rawResponse ?? "").Length, 120))}");
            // Execute game actions
            if (_parsed?.Actions != null && _currentNpc != null)
                ActionExecutor.Execute(_parsed.Actions, _currentNpc);

            // Save exchange to NPCContext
            if (_currentContext != null && _parsed != null)
            {
                if (_currentContext.IsFirstConversation &&
                    !string.IsNullOrWhiteSpace(_parsed.PersonalitySummary))
                {
                    _currentContext.GeneratedPersonality = _parsed.PersonalitySummary!;

                    if (!string.IsNullOrWhiteSpace(_parsed.Ambition) && _currentNpc?.IsLord == true)
                        _currentContext.GeneratedAmbition = _parsed.Ambition!;
                }

                string npcSaid = string.IsNullOrWhiteSpace(_parsed.Response)
                    ? "..."
                    : _parsed.Response;

                if (!string.IsNullOrWhiteSpace(_lastPlayerMsg))
                    _currentContext.AddExchange(_lastPlayerMsg, npcSaid);

                // Long-term memory note
                if (!string.IsNullOrWhiteSpace(_parsed.MemoryNote))
                {
                    int memDay = 0;
                    try { memDay = (int)Campaign.Current.Models.CampaignTimeModel
                                        .CampaignStartTime.ElapsedDaysUntilNow; } catch { }
                    AIMemoryStore.AddNpcMemory(_currentContext.HeroId, _parsed.MemoryNote!, memDay);
                }

                // Pending request lifecycle
                if (!string.IsNullOrWhiteSpace(_parsed.Request))
                {
                    string requestText = _parsed.Request!.Trim();

                    if (_currentContext.HasPendingRequest)
                    {
                        // Conflict: hold and let the NPC offer the player a choice.
                        // _currentNpc/_currentContext are NOT reset below.
                        _pendingNewRequest  = requestText;
                        _pendingNewMechanic = _parsed?.QuestMechanic;
                    }
                    else
                    {
                        CommitNewRequest(requestText, _parsed?.QuestMechanic);
                    }
                }

                // Request fulfilled — this is the single trigger for quest closure.
                // The LLM's word alone is NOT enough when the quest has a real
                // mechanic: the atom engine tracks the truth in the world, and a
                // player could otherwise talk a small model into paying the
                // reward for work never done. Roleplay-only requests (no
                // mechanic) still close on the LLM's judgement.
                bool mechanicSatisfied = true;
                var pendingMechanic = _currentContext.PendingRequest?.Mechanic;
                if (pendingMechanic != null)
                {
                    mechanicSatisfied = pendingMechanic.AllCompleted
                        || (pendingMechanic.HasReturnStep && pendingMechanic.AllExceptReturnCompleted);
                }

                if (_parsed.RequestFulfilled && _currentContext.HasPendingRequest && !mechanicSatisfied)
                {
                    RFAIDebug.Log("ConsequenceClearResponse: LLM claimed fulfilled but mechanic objectives are incomplete — closure blocked");
                }

                if (_parsed.RequestFulfilled && _currentContext.HasPendingRequest && mechanicSatisfied)
                {
                    // Pay mechanic reward gold unless the LLM already granted gold explicitly.
                    int rewardGold = _currentContext.PendingRequest!.Mechanic?.RewardGold ?? 0;
                    if (rewardGold > 0 && !HasGoldRewardAction(_parsed.Actions))
                    {
                        try
                        {
                            Hero.MainHero?.ChangeHeroGold(rewardGold);
                            RFAIDebug.Log($"ConsequenceClearResponse: paid {rewardGold} gold from mechanic");
                        }
                        catch { }
                    }
                    else if (rewardGold > 0)
                    {
                        RFAIDebug.Log("ConsequenceClearResponse: skipped mechanic gold because actions already granted gold");
                    }

                    // Close the quest log entry.
                    if (_currentNpc != null)
                    {
                        try
                        {
                            var q = AIDialogQuest.ForNpc(_currentNpc.StringId);
                            if (q != null && q.IsOngoing) q.MarkFulfilled();
                        }
                        catch { }
                    }

                    _currentContext.AddCompletedRequest(
                        _currentContext.PendingRequest!,
                        CurrentDay(),
                        "Completed");
                    _currentContext.PendingRequest = null;
                }

                // Update last known relation for grievance detection
                if (_currentNpc != null)
                    _currentContext.LastKnownRelation = (int)_currentNpc.GetRelationWithPlayer();

                // Clear initiative
                _currentContext.PendingInitiativeReason = null;

                NPCContextStore.Instance?.MarkDirty(_currentContext);
                RFAIDebug.Log($"ConsequenceClearResponse: context saved for {_currentContext.HeroId} | pending={_currentContext.HasPendingRequest} | history={_currentContext.RecentHistory.Count} | externalMemoryPath={AIMemoryStore.DirectoryPath}");
            }

            // Reset state
            _rawResponse   = null;
            _parsed        = null;
            _responseAudioQueued = false;
            _lastPlayerMsg = "";

            // Keep NPC/context alive if the replace-request dialog is about to show.
            if (_pendingNewRequest == null)
            {
                _currentNpc     = null;
                _currentContext = null;
            }
        }

        private void ConsequenceAcceptNewRequest()
        {
            if (_pendingNewRequest != null && _currentNpc != null && _currentContext != null)
            {
                _currentContext.PendingRequest = null;
                CommitNewRequest(_pendingNewRequest, _pendingNewMechanic);
                NPCContextStore.Instance?.MarkDirty(_currentContext);
            }
            FinishPendingRequestCleanup();
        }

        private void ConsequenceDeclineNewRequest()
        {
            FinishPendingRequestCleanup();
        }

        // Helpers

        private void SanitizePendingRequestForCurrentNpc()
        {
            if (_currentNpc == null || _currentContext?.PendingRequest?.Mechanic == null)
                return;

            var sanitized = QuestMechanicValidator.Sanitize(
                _currentContext.PendingRequest.Mechanic,
                _currentNpc);

            if (sanitized != null)
            {
                _currentContext.PendingRequest.Mechanic = sanitized;
                NPCContextStore.Instance?.MarkDirty(_currentContext);
                return;
            }

            RFAIDebug.Log($"SanitizePendingRequestForCurrentNpc: cleared invalid pending request for {_currentNpc.StringId}");
            _currentContext.PendingRequest = null;
            NPCContextStore.Instance?.MarkDirty(_currentContext);

            try
            {
                var quest = AIDialogQuest.ForNpc(_currentNpc.StringId);
                if (quest != null && quest.IsOngoing)
                    quest.Cancel();
            }
            catch { }
        }

        private void CommitNewRequest(string requestText, QuestMechanic? mechanic = null)
        {
            if (_currentNpc == null || _currentContext == null) return;

            int day = 0;
            try { day = (int)Campaign.Current.Models.CampaignTimeModel
                              .CampaignStartTime.ElapsedDaysUntilNow; } catch { }

            if (IsRequestOnCooldown(_currentContext, day))
            {
                RFAIDebug.Log($"CommitNewRequest: rejected request for {_currentContext.HeroId} - cooldown ({day - _currentContext.LastRequestDay}/{NPCContext.RequestCooldownDays}d)");
                return;
            }

            // Sanitize mechanic data generated by the LLM before it becomes a real quest.
            bool hadRawMechanic = mechanic != null;
            mechanic = QuestMechanicValidator.Sanitize(mechanic, _currentNpc);
            mechanic?.Normalize();
            if (hadRawMechanic && mechanic == null)
                RFAIDebug.Log("CommitNewRequest: discarded invalid quest_mechanic, keeping request as roleplay-only");

            _currentContext.PendingRequest = new PendingRequest
            {
                Description = requestText,
                DayIssued   = day,
                Mechanic    = mechanic
            };
            _currentContext.LastRequestDay = day;
            NPCContextStore.Instance?.MarkDirty(_currentContext);

            // Start the quest log entry immediately (don't wait for next load).
            try
            {
                var existing = AIDialogQuest.ForNpc(_currentNpc.StringId);
                if (existing == null || !existing.IsOngoing)
                {
                    int durationDays = mechanic?.DurationDays > 0 ? mechanic.DurationDays : 30;
                    string qId       = $"rfai_{_currentNpc.StringId}_{day}";
                    var quest = new AIDialogQuest(qId, _currentNpc, requestText, durationDays);
                    quest.StartQuest();
                    GrantSuppliedDeliveryGoods(mechanic, _currentNpc);

                    // Log each atom as a to-do bullet so the player sees them in the journal.
                    if (mechanic != null)
                    {
                        foreach (var atom in mechanic.Objectives)
                        {
                            if (!string.IsNullOrWhiteSpace(atom.Label))
                                quest.AddObjectiveLog($"○ {atom.Label}");
                        }
                    }

                    RFAIDebug.Log($"CommitNewRequest: quest started for {_currentNpc.Name}" +
                                  (mechanic != null ? $" ({mechanic.QuestKind}, {mechanic.Objectives.Count} atoms)" : ""));
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"CommitNewRequest: quest start failed — {ex.Message}");
            }
        }

        private void FinishPendingRequestCleanup()
        {
            _pendingNewRequest  = null;
            _pendingNewMechanic = null;
            _currentNpc         = null;
            _currentContext     = null;
        }

        private void ShowPlayerInputInquiry(string promptText, string defaultInputText = "")
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText:                 $"Speak with {_npcNameText}",
                text:                      promptText,
                isAffirmativeOptionShown:  true,
                isNegativeOptionShown:     true,
                affirmativeText:           "Ask",
                negativeText:              "Cancel",
                affirmativeAction:         OnPlayerConfirmedInput,
                negativeAction:            OnPlayerCancelledInput,
                shouldInputBeObfuscated:   false,
                textCondition:             null,
                soundEventPath:            "",
                defaultInputText:          defaultInputText));
        }

        private static int CurrentDay()
        {
            try
            {
                return (int)Campaign.Current.Models.CampaignTimeModel
                    .CampaignStartTime.ElapsedDaysUntilNow;
            }
            catch { return 0; }
        }

        private static bool IsRequestOnCooldown(NPCContext ctx, int currentDay)
        {
            if (ctx == null || ctx.LastRequestDay <= -100000)
                return false;

            return currentDay - ctx.LastRequestDay < NPCContext.RequestCooldownDays;
        }

        private static void GrantSuppliedDeliveryGoods(QuestMechanic? mechanic, Hero npc)
        {
            if (mechanic == null ||
                !mechanic.QuestKind.Equals("delivery", StringComparison.OrdinalIgnoreCase) &&
                !mechanic.QuestKind.Equals("delivery_under_pressure", StringComparison.OrdinalIgnoreCase))
                return;

            bool hasDestination = mechanic.Objectives.Any(atom =>
                string.Equals(atom.AtomType, "VISIT_SETTLEMENT", StringComparison.OrdinalIgnoreCase));
            if (!hasDestination)
                return;

            mechanic.Normalize();
            if (mechanic.GetProgress("delivery_goods_supplied") > 0)
                return;

            bool suppliedAny = false;
            foreach (var atom in mechanic.Objectives)
            {
                if (!string.Equals(atom.AtomType, "BRING_ITEM", StringComparison.OrdinalIgnoreCase))
                    continue;

                string itemId = atom.GetParam("item_id");
                int quantity = Math.Max(1, atom.GetParamInt("quantity", 1));
                if (string.IsNullOrWhiteSpace(itemId))
                    continue;

                try
                {
                    var item = QuestDeliveryRules.ResolveDeliveryItem(itemId);
                    if (item == null)
                    {
                        RFAIDebug.Log($"GrantSuppliedDeliveryGoods: item not found for {itemId}");
                        continue;
                    }

                    MobileParty.MainParty.ItemRoster.AddToCounts(item, quantity);
                    suppliedAny = true;
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[AI Quest] {npc.Name} supplied {quantity}x {item.Name} for delivery.",
                        Color.FromUint(0xFF_A0_D0_FFu)));
                    RFAIDebug.Log($"GrantSuppliedDeliveryGoods: supplied {quantity}x {item.StringId} for {npc.StringId} (requested {itemId})");
                }
                catch (Exception ex)
                {
                    RFAIDebug.Log($"GrantSuppliedDeliveryGoods failed: {ex.GetType().Name}: {ex.Message}");
                }
            }

            if (suppliedAny)
                mechanic.Progress["delivery_goods_supplied"] = 1;
        }

        private static bool HasGoldRewardAction(List<AIAction>? actions)
        {
            if (actions == null) return false;
            return actions.Any(a =>
                a != null &&
                string.Equals(a.Type, "give_gold", StringComparison.OrdinalIgnoreCase) &&
                a.Value > 0);
        }

        private void TryQueueResponseAudio(string text)
        {
            if (_responseAudioQueued || !AIConfig.TTSEnabled || !AIConfig.TTSAutoPlay)
                return;

            _responseAudioQueued = true;

            if (!_ttsDisclosureShown)
            {
                _ttsDisclosureShown = true;
                InformationManager.DisplayMessage(new InformationMessage(
                    "[AI Voice] Spoken dialogue is AI-generated.",
                    Color.FromUint(0xFF_A0_D0_FFu)));
            }

            string? npcName = _currentNpc?.Name?.ToString();
            string instructions = string.IsNullOrWhiteSpace(npcName)
                ? "Speak as a medieval fantasy character in a grounded, natural tone."
                : $"Speak as {npcName}, a medieval fantasy character in a grounded, natural tone.";

            RFAudioService.TrySpeakNpcReply(text, instructions);
        }

        private RFAIResponse? ParseResponse(string raw)
        {
            try
            {
                string? json = JsonCleaner.ExtractJson(raw);
                if (json == null) return null;
                return JsonConvert.DeserializeObject<RFAIResponse>(json);
            }
            catch { return null; }
        }

        private static string Sanitize(string text)
        {
            if (text == "[cancel]") return "Conversation cancelled.";

            text = text
                .Replace("\r\n", " ")
                .Replace("\n",   " ")
                .Replace("\r",   " ")
                .Replace("{",    "(")
                .Replace("}",    ")")
                .Replace("|",    "/")
                .Replace("[",    "(")
                .Replace("]",    ")");

            if (text.Length > 400)
                text = text.Substring(0, 400) + "...";

            return text;
        }

        private string FallbackPrompt() =>
            "You are a medieval lord in Aeurth. " +
            "Respond ONLY with JSON: " +
            "{\"internal_thoughts\":\"...\",\"response\":\"your spoken words\",\"tone\":\"neutral\",\"actions\":[]}";
    }
}
