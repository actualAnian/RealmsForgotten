using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
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
        // State
        private volatile bool        _isWaiting          = false;
        private volatile string?     _rawResponse        = null;
        private          RFAIResponse? _parsed           = null;

        public volatile bool ResponseJustArrived = false;

        private Hero?       _currentNpc      = null;
        private string      _npcNameText     = "";
        private string      _lastPlayerMsg   = "";
        private NPCContext? _currentContext  = null;

        // When non-null, the LLM issued a new request while a previous one is pending.
        // The NPC will ask the player to choose. _currentNpc/_currentContext stay alive
        // until the choice is made.
        private string? _pendingNewRequest = null;

        public string CurrentNpcName => _npcNameText;

        public override void RegisterEvents() { }
        public override void SyncData(IDataStore dataStore) { }

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
            => Hero.OneToOneConversationHero != null && !_isWaiting;

        private bool ConditionHasPendingInitiative()
        {
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

            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText:                $"Speak with {_npcNameText}",
                text:                     "What do you wish to say?",
                isAffirmativeOptionShown:  true,
                isNegativeOptionShown:     true,
                affirmativeText:           "Ask",
                negativeText:              "Cancel",
                affirmativeAction:         OnPlayerConfirmedInput,
                negativeAction:            OnPlayerCancelledInput));
        }

        private void ConsequenceTriggerInitiative()
        {
            _currentNpc     = Hero.OneToOneConversationHero;
            _npcNameText    = _currentNpc?.Name.ToString() ?? "?";
            _currentContext = _currentNpc != null
                ? NPCContextStore.Instance?.GetOrCreate(_currentNpc)
                : null;

            OnPlayerConfirmedInput("(You give the NPC your attention, inviting them to speak first.)");
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
            _lastPlayerMsg = playerText;

            Hero?       npc = _currentNpc;
            NPCContext? ctx = _currentContext;

            string systemPrompt;
            try   { systemPrompt = PromptBuilder.Build(npc!, ctx); }
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
        }

        private void ConsequenceClearResponse()
        {
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
                    _currentContext.AddMemory(_parsed.MemoryNote, memDay);
                }

                // Pending request lifecycle
                if (!string.IsNullOrWhiteSpace(_parsed.Request))
                {
                    string requestText = _parsed.Request!.Trim();

                    if (_currentContext.HasPendingRequest)
                    {
                        // Conflict: hold and let the NPC offer the player a choice.
                        // _currentNpc/_currentContext are NOT reset below.
                        _pendingNewRequest = requestText;
                    }
                    else
                    {
                        CommitNewRequest(requestText);
                    }
                }

                // Request fulfilled
                if (_parsed.RequestFulfilled && _currentContext.HasPendingRequest)
                {
                    try
                    {
                        if (_currentNpc != null)
                            AIDialogQuest.ForNpc(_currentNpc.StringId)?.MarkFulfilled();
                    }
                    catch { }

                    _currentContext.PendingRequest = null;
                }

                // Update last known relation for grievance detection
                if (_currentNpc != null)
                    _currentContext.LastKnownRelation = (int)_currentNpc.GetRelationWithPlayer();

                // Clear initiative
                _currentContext.PendingInitiativeReason = null;

                NPCContextStore.Instance?.MarkDirty(_currentContext);
            }

            // Reset state
            _rawResponse   = null;
            _parsed        = null;
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
                AIDialogQuest.ForNpc(_currentNpc.StringId)?.Cancel();
                _currentContext.PendingRequest = null;
                CommitNewRequest(_pendingNewRequest);
                NPCContextStore.Instance?.MarkDirty(_currentContext);
            }
            FinishPendingRequestCleanup();
        }

        private void ConsequenceDeclineNewRequest()
        {
            FinishPendingRequestCleanup();
        }

        // Helpers

        private void CommitNewRequest(string requestText)
        {
            if (_currentNpc == null || _currentContext == null) return;

            int day = 0;
            try { day = (int)Campaign.Current.Models.CampaignTimeModel
                              .CampaignStartTime.ElapsedDaysUntilNow; } catch { }

            _currentContext.PendingRequest = new PendingRequest
            {
                Description = requestText,
                DayIssued   = day
            };

            try
            {
                long hours = 0;
                try { hours = (long)CampaignTime.Now.ToHours; } catch { hours = day * 24L; }
                string questId = $"rfai_{_currentNpc.StringId}_{hours}";
                new AIDialogQuest(questId, _currentNpc, requestText, 30).StartQuest();
            }
            catch { }
        }

        private void FinishPendingRequestCleanup()
        {
            _pendingNewRequest = null;
            _currentNpc        = null;
            _currentContext    = null;
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
