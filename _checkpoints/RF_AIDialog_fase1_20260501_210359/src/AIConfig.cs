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
        /// Máximo de tokens na resposta. Limita o tamanho e uso de VRAM.
        /// </summary>
        public const int MaxTokens = 200;

        /// <summary>
        /// Tamanho do contexto. Menor = menos VRAM. 2048 é seguro para modelos grandes.
        /// </summary>
        public const int ContextSize = 2048;

        /// <summary>
        /// Temperatura do LLM. 0.7 = criativo mas coerente.
        /// </summary>
        public const double Temperature = 0.7;
    }
}
