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
        /// Timeout in seconds for LLM calls.
        /// Larger context windows (4096) make Ollama reload the model on first use —
        /// 120s gives enough room for cold starts without feeling frozen.
        /// </summary>
        public const int TimeoutSeconds = 120;

        /// <summary>
        /// Max tokens for the FIRST conversation with an NPC.
        /// Needs extra room for personality_summary (2-3 sentences) + full JSON.
        /// </summary>
        public const int MaxTokensFirstConversation = 500;

        /// <summary>
        /// Max tokens for subsequent conversations (no personality_summary).
        /// A typical response JSON is 100-150 tokens; 300 gives 2x headroom.
        /// Keeping this low is the main lever for reducing response latency.
        /// </summary>
        public const int MaxTokensSubsequent = 300;

        /// <summary>
        /// Maximum gold that can be transferred in a single AI action (give_gold / take_gold).
        /// Prevents runaway LLM values while still allowing meaningful transactions.
        /// </summary>
        public con