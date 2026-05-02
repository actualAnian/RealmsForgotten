namespace RF_AIDialog
{
    /// <summary>
    /// Central configuration for the RF_AIDialog mod.
    /// Edit here to adjust model, endpoint, and behaviour limits.
    /// </summary>
    public static class AIConfig
    {
        /// <summary>
        /// Ollama model name. Run `ollama list` in a terminal to see available models.
        /// </summary>
        public const string ModelName = "gemma3:4b";

        /// <summary>
        /// Ollama endpoint. Default for a local installation.
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
        /// </summary>
        public const int MaxGoldTransfer = 10000;

        /// <summary>
        /// Maximum relation delta per conversation (relation_change action).
        /// Keeps relation changes meaningful but not instant max/min.
        /// </summary>
        public const int MaxRelationDelta = 5;

        /// <summary>
        /// Context window size. 4096 gives comfortable headroom for
        /// prompt (personality + history + actions ~1100 tokens) + response.
        /// </summary>
        public const int ContextSize = 4096;

        /// <summary>
        /// LLM temperature. 0.7 = creative but coherent.
        /// </summary>
        public const double Temperature = 0.7;
    }
}
