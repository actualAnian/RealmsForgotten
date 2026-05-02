namespace RF_AIDialog
{
    /// <summary>
    /// Configurações centrais do mod RF_AIDialog.
    /// Edite aqui para ajustar o modelo e o endpoint do Ollama.
    /// </summary>
    public static class AIConfig
    {
        /// <summary>
        /// Nome do modelo Ollama. Use `ollama list` no terminal para ver os disponíveis.
        /// </summary>
        public const string ModelName = "gemma3:4b";

        /// <summary>
        /// Endpoint do Ollama. Padrão para instalação local.
        /// </summary>
        public const string OllamaEndpoint = "http://localhost:11434/api/chat";

        /// <summary>
        /// Timeout em segundos para a chamada ao LLM.
        /// Modelos maiores podem precisar de mais tempo.
        /// </summary>
        public const int TimeoutSeconds = 60;

        /// <summary>
        /// Max tokens in the response.
        /// Regular conversation: ~150 tokens. First contact (with personality_summary): ~400.
        /// 500 covers both cases safely without excessive VRAM use.
        /// </summary>
        public const int MaxTokens = 800;

        /// <summary>
        /// Maximum gold that can be transferred in a single AI action (give_gold / take_gold).
        /// Prevents runaway LLM values while still allowing meaningful transactions.
        /// </summary>
        public const int MaxGoldTransfer = 10000;

        /// <summary>
        /// Maximum relation delta per con