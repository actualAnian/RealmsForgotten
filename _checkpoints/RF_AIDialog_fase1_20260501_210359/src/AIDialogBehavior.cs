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
    /// Injeta uma opção "Falar livremente [IA]" no diálogo com qualquer Hero.
    ///
    /// Fluxo:
    ///   1. Player clica "Falar livremente... [IA]"
    ///   2. Aparece caixa de texto nativa do Bannerlord (ShowTextInquiry)
    ///   3. Player digita o que quiser e confirma
    ///   4. LLM processa em background retornando JSON estruturado
    ///   5. Player clica "..." até a resposta chegar
    ///   6. Resposta (campo "response" do JSON) aparece no dialogue box
    /// </summary>
    public class AIDialogBehavior : CampaignBehaviorBase
    {
        // ── Estado ────────────────────────────────────────────────────────
        private volatile bool        _isWaiting   = false;
        private volatile string?     _rawResponse = null;   // JSON bruto do LLM
        private          RFAIResponse? _parsed    = null;   // JSON parseado

        private Hero?  _currentNpc  = null;
        private string _npcNameText = "";

        // ─────────────────────────────────────────────────────────────────

        public override void RegisterEvents() { }
        public override void SyncData(IDataStore dataStore) { }

        public void AddDialogs(CampaignGameStarter starter)
        {
            MBTextManager.SetTextVariable("RF_AI_RESPONSE", new TextObject("{=!}..."));

            // ── 1. Opção no menu principal ────────────────────────────────
            starter.AddPlayerLine(
                "rf_ai_open_input",
                "hero_main_options",
                "rf_ai_npc_thinking",
                "Falar livremente... [IA]",
                ConditionCanUseAI,
                ConsequenceOpenTextInput,
                100, null);

            // ── 2. NPC no estado "pensando" ───────────────────────────────
            starter.AddDialogLine(
                "rf_ai_npc_thinking_line",
                "rf_ai_npc_thinking",
                "rf_ai_player_wait",
                "[Reflexivo] Aguarde um momento enquanto escolho minhas palavras...",
                null, null, 100, null);

            // ── 3a. Loop de espera ────────────────────────────────────────
            starter.AddPlayerLine(
                "rf_ai_player_waiting",
                "rf_ai_player_wait",
                "rf_ai_npc_thinking",
                "...",
                ConditionIsStillWaiting,
                null, 100, null);

            // ── 3b. Resposta pronta ───────────────────────────────────────
            starter.AddPlayerLine(
                "rf_ai_player_read",
                "rf_ai_player_wait",
                "rf_ai_npc_response",
                "(Ouvir a resposta)",
                ConditionResponseReady,
                null, 200, null);

            // ── 4. NPC fala com o texto do LLM ───────────────────────────
            starter.AddDialogLine(
                "rf_ai_npc_response_line",
                "rf_ai_npc_response",
                "hero_main_options",
                "{RF_AI_RESPONSE}",
                ConditionShowResponse,
                ConsequenceClearResponse,
                100, null);
        }

        // ── Condições ─────────────────────────────────────────────────────

        private bool ConditionCanUseAI()
            => Hero.OneToOneConversationHero != null && !_isWaiting;

        private bool ConditionIsStillWaiting()
            => _isWaiting || _rawResponse == null;

        private bool ConditionResponseReady()
            => !_isWaiting && _rawResponse != null;

        private bool ConditionShowResponse()
        {
            // Parseia o JSON se ainda não foi parseado
            if (_parsed == null && _rawResponse != null)
                _parsed = ParseResponse(_rawResponse);

            // Extrai o texto a exibir — usa o campo "response" do JSON,
            // com fallback para o texto bruto se o parse falhar
            string text = _parsed?.Response
                       ?? _rawResponse
                       ?? "[Sem resposta]";

            // Sanitiza para o TextObject do Bannerlord
            text = Sanitize(text);

            MBTextManager.SetTextVariable("RF_AI_RESPONSE", new TextObject("{=!}" + text));
            return true;
        }

        // ── Consequências ─────────────────────────────────────────────────

        private void ConsequenceOpenTextInput()
        {
            _currentNpc  = Hero.OneToOneConversationHero;
            _npcNameText = _currentNpc?.Name.ToString() ?? "?";

            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText:                $"Falar com {_npcNameText}",
                text:                     "O que deseja dizer?",
                isAffirmativeOptionShown:  true,
                isNegativeOptionShown:     true,
                affirmativeText:           "Perguntar",
                negativeText:              "Cancelar",
                affirmativeAction:         OnPlayerConfirmedInput,
                negativeAction:            OnPlayerCancelledInput));
        }

        private void OnPlayerConfirmedInput(string playerText)
        {
            if (string.IsNullOrWhiteSpace(playerText))
            {
                _rawResponse = "[Pergunta vazia — tente novamente]";
                return;
            }

            _isWaiting   = true;
            _rawResponse = null;
            _parsed      = null;

            Hero?  npc = _currentNpc;
            string systemPrompt;
            try   { systemPrompt = PromptBuilder.Build(npc!); }
            catch { systemPrompt = FallbackPrompt(); }

            Task.Run(async () =>
            {
                try
                {
                    _rawResponse = await AIClient.AskAsync(
                        AIConfig.ModelName,
                        systemPrompt,
                        playerText).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _rawResponse = $"[Erro: {ex.Message}]";
                }
                finally
                {
                    _isWaiting = false;
                }
            });
        }

        private void OnPlayerCancelledInput()
        {
            _rawResponse = null;
            _isWaiting   = false;
            // Cancela silenciosamente — o diálogo retorna ao hero_main_options
            // via ConditionIsStillWaiting → false quando _rawResponse == null
            // e _isWaiting == false, mas ConditionResponseReady também é false.
            // Precisamos forçar uma saída: usamos a string de cancelamento.
            _rawResponse = "[cancel]";
        }

        private void ConsequenceClearResponse()
        {
            _rawResponse = null;
            _parsed      = null;
            _currentNpc  = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private RFAIResponse? ParseResponse(string raw)
        {
            try
            {
                string? json = JsonCleaner.ExtractJson(raw);
                if (json == null) return null;
                return JsonConvert.DeserializeObject<RFAIResponse>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string Sanitize(string text)
        {
            // Remove o marcador de cancelamento
            if (text == "[cancel]") return "[Conversa cancelada]";

            text = text
                .Replace("\r\n", " ")
                .Replace("\n",   " ")
                .Replace("\r",   " ")
                .Replace("{",    "(")
                .Replace("}",    ")")
                .Replace("|",    "/");

            if (text.Length > 400)
                text = text.Substring(0, 400) + "...";

            return text;
        }

        private string FallbackPrompt() =>
            "Você é um lord medieval em Calradia. " +
            "Responda APENAS com JSON: " +
            "{\"internal_thoughts\":\"...\",\"response\":\"sua fala\",\"tone\":\"neutral\"}";
    }
}
