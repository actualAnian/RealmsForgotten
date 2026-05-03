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
        public const string APIKey = "YOUR_DEEPSEEK_API_KEY_HERE";

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

        // ── Shared settings ────�