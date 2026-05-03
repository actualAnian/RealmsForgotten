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
    ///   6. Player clicks '...' → NPC response appears in the dialogue box
    ///   7. Exchange is saved to NPCContext for continuity in future conversations
    /// </summary>
    public class AIDialogBehavior : CampaignBehaviorBase
    {
        // ── State ─────────────────────────────────────────────────────────
        private volatile bool        _isWaiting          = false;
        private volatile string?     _rawResponse        = null;
        private          RFAIResponse? _parsed           = null;

        /// <summary>
        /// Signals the SubModule that a response just arrived.
        /// Read and cleared on the main thread (OnApplicationTick).
        /// </summary>
        public volatile bool ResponseJustArrived = false;

        private Hero?       _currentNpc      = null;
        private string      _npcNameText     = "";
        private string      _lastPlayerMsg   = "";
        private NPCContext? _currentContext  = null;

        /// <summary>Current NPC name — read by the SubModule for HUD notification.</summary>
        public string CurrentNpcName => _npcNameText;

        // ─────────────────────────────────────────────────────────────────

        public override void RegisterEvents() { }
        public override void SyncData(IDataStore dataStore) { }

        public void AddDialogs(CampaignGameStarter starter)
        {
            MBTextManager.SetTextVariable("RF_AI_RESPONSE",  new TextObject("{=!}..."));
            MBTextManager.SetTextVariable("RF_AI_WAIT_TEXT", new TextObject("{=!}..."));

            // ── 0. NPC initiative trigger (higher priority) ───────────────
            starter.AddPlayerLine(
                "rf_ai_initiative_trigger",
                "hero_main_options",
                "rf_ai_npc_thinking",
                "You seem like you have something on your mind... [AI]",
                ConditionHasPendingInitiative,
                ConsequenceTriggerInitiative,
                150, null);

            // ── 1. Player menu option ─────────────────────────────────────
            starter.AddPlayerLine(
                "rf_ai_open_input",
                "hero_main_options",
                "rf_ai_npc_thinking",
                "Speak freely... [AI]",
                ConditionCanUseAI,
                ConsequenceOpenTextInput,
                100, null);

            // ── 2. NPC thinking state ─────────────────────────────────────
            starter.AddDialogLine(
                "rf_ai_npc_thinking_line",
                "rf_ai_npc_thinking",
                "rf_ai_player_wait",
                "[Thoughtful] Give me a moment to choose my words...",
                null, null, 100, null);

            // ── 3. Single wait option — text updates each time state is entered
            starter.AddPlayerLine(
                "rf_ai_player_waiting",
                "rf_ai_player_wait",
                "rf_ai_player_wait_result",
                "{RF_AI_WAIT_TEXT}",
                ConditionUpdateWaitText,
                null, 100, null);

            // ── 3a. Still waiting — loop back ─────────────────────────────
            starter.AddDialogLine(
                "rf_ai_still_waiting_line",
                "rf_ai_player_wait_result",
                "rf_ai_player_wait",
                "[Thoughtful] Hmm...",
                ConditionIsStillWaiting,
                null, 100, null);

            // ── 3b. Response ready — NPC speaks, goes to continue menu ──────
            starter.AddDialogLine(
                "rf_ai_npc_response_line",
                "rf_ai_player_wait_result",
                "rf_ai_after_response",
                "{RF_AI_RESPONSE}",
                ConditionShowResponse,
                ConsequenceClearResponse,
                200, null);

            // ── 4a. Continue speaking ─────────────────────────────────────
            starter.AddPlayerLine(
                "rf_ai_continue",
                "rf_ai_after_response",
                "rf_ai_npc_thinking",
                "Continue speaking... [AI]",
                ConditionCanUseAI,
                ConsequenceOpenTextInput,
                200, null);

            // ── 4b. End conversation ──────────────────────────────────────
            starter.AddPlayerLine(
                "rf_ai_end",
                "rf_ai_after_response",
                "hero_main_options",
                "That will be all.",
                null, null,
                100, null);
        }

        // ── Conditions ────────────────────────────────────────────────────

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
                : "(Response ready — click here)";
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

        // ── Consequences ──────────────────────────────────────────────────

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

            // Send a neutral opener — the NPC will lead with their initiative topic
            OnPlayerConfirmedInput("(You give the NPC your attention, inviting them to speak first.)");
        }

        private void OnPlayerConfirmedInput(string playerText)
        {
            if (string.IsNullOrWhiteSpace(playerText))
            {
                _rawResponse = "(Empty message — please try again)";
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
            // ── Execute game actions ──────────────────────────────────────
            if (_parsed?.Actions != null && _currentNpc != null)
                ActionExecutor.Execute(_parsed.Actions, _currentNpc);

            // ── Save exchange to NPCContext ────────────────────────────────
            if (_currentContext != null && _parsed != null)
            {
                if (_currentContext.IsFirstConversation &&
                    !string.IsNullOrWhiteSpace(_parsed.PersonalitySummary))
                {
                    _currentContext.GeneratedPersonality = _parsed.PersonalitySummary!;
                }

                string npcSaid = string.I