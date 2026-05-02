using System;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// CampaignBehavior de teste: dispara uma chamada ao Ollama assim que
    /// a campanha é carregada e armazena o resultado num campo volatile.
    ///
    /// A exibição real (InformationManager) é feita em RF_AIDialogSubModule.OnApplicationTick
    /// porque InformationManager só pode ser chamado da thread principal.
    /// </summary>
    public class OllamaTestBehavior : CampaignBehaviorBase
    {
        // ── Configuração ──────────────────────────────────────────────────
        // Altere para o nome do modelo que você tem no Ollama.
        // Para ver os modelos: abra um terminal e rode `ollama list`
        private const string ModelName = "qwen2.5:14b";

        // ── Estado compartilhado entre threads ────────────────────────────
        // 'volatile' garante que a thread principal sempre lê o valor mais recente
        public volatile string? PendingMessage = null;
        public volatile bool    IsWaiting      = false;

        // ─────────────────────────────────────────────────────────────────

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Nada a serializar neste mod de teste
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            TriggerLLMTest();
        }

        /// <summary>
        /// Pode ser chamado de qualquer lugar para disparar um novo teste.
        /// </summary>
        public void TriggerLLMTest()
        {
            if (IsWaiting)
            {
                PendingMessage = "[RF_AI] Ainda aguardando resposta anterior...";
                return;
            }

            IsWaiting      = true;
            PendingMessage = "[RF_AI] Conectando ao Ollama...";

            // Task.Run executa em background — não trava o game loop
            Task.Run(async () =>
            {
                try
                {
                    string systemPrompt =
                        "Você é um lord medieval num mundo de fantasia sombria. " +
                        "Responda sempre em caráter, de forma breve (1-2 frases).";

                    string userMessage =
                        "Saudações, lord. Quem és tu e de qual clã provéns?";

                    string response = await AIClient.AskAsync(
                        ModelName,
                        systemPrompt,
                        userMessage);

                    PendingMessage = "[RF_AI OK] " + response;
                }
                catch (Exception ex)
                {
                    PendingMessage = "[RF_AI ERRO] " + ex.Message;
                }
                finally
                {
                    IsWaiting = false;
                }
            });
        }
    }
}
