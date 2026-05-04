namespace RF_AIDialog
{
    /// <summary>
    /// Central configuration for the RF_AIDialog mod.
    ///
    /// SWITCHING BETWEEN LOCAL (Ollama) AND API (DeepSeek / OpenAI-compatible):
    ///   Set UseRemoteAPI = true  → uses APIEndpoint + APIKey + APIModelName
    ///   Set UseRemoteAPI = false → uses OllamaEndpoint + ModelName (local)
    /// </summary>
    public static class AIConfig
    {
        // ── Backend selection ─────────────────────────────────────────────

        /// <summary>
        /// true  = use remote OpenAI-compatible API (DeepSeek, OpenAI, etc.)
        /// false = use local Ollama
        /// </summary>
        public const bool UseRemoteAPI = true;

        // ── Remote API (DeepSeek / OpenAI-compatible) ─────────────────────

        /// <summary>
        /// DeepSeek chat completions endpoint (OpenAI-compatible).
        /// </summary>
        public const string APIEndpoint = "https://api.deepseek.com/chat/completions";

        /// <summary>
        /// Your DeepSeek API key. Do NOT commit this to a public repository.
        /// </summary>
        public const string APIKey = "sk-d295471bf32849e6b2b66eed0923c607";

        /// <summary>
        /// DeepSeek model. "deepseek-chat" = DeepSeek-V3 (fast, cheap, excellent).
        /// </summary>
        public const string APIModelName = "deepseek-chat";

        // ── Local Ollama ──────────────────────────────────────────────────

        /// <summary>
        /// Ollama model name. Run `ollama list` to see available models.
        /// </summary>
        public const string ModelName = "gemma3:4b";

        /// <summary>
        /// Ollama endpoint. Default for a local installation.
        /// </summary>
        public const string OllamaEndpoint = "http://localhost:11434/api/chat";

        /// <summary>
        /// Context window size for Ollama (ignored for remote API).
        /// Keep at 2048 on an 8GB GPU running alongside Bannerlord.
        /// </summary>
        public const int ContextSize = 2048;

        // ── Shared settings ───────────────────────────────────────────────

        /// <summary>
        /// Timeout in seconds. Remote APIs are faster than cold Ollama starts.
        /// 30s is generous for DeepSeek; 120s was needed for Ollama cold starts.
        /// </summary>
        public const int TimeoutSeconds = 30;

        /// <summary>
        /// Max tokens for the FIRST conversation with an NPC.
        /// Needs room for personality_summary + full JSON.
        /// </summary>
        public const int MaxTokensFirstConversation = 650;

        /// <summary>
        /// Max tokens for subsequent conversations (no personality_summary).
        /// </summary>
        public const int MaxTokensSubsequent = 450;

        /// <summary>
        /// Maximum gold transferred in a single AI action.
        /// </summary>
        public const int MaxGoldTransfer = 10000;

        /// <summary>
        /// Maximum relation delta per conversation.
        /// </summary>
        public const int MaxRelationDelta = 5;

        /// <summary>
        /// LLM temperature. 0.7 = creative but coherent.
        /// </summary>
        public const double Temperature = 0.7;
    }
}
