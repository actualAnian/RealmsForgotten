using Newtonsoft.Json;

namespace RF_AIDialog
{
    /// <summary>
    /// Estrutura da resposta JSON que o LLM deve retornar.
    /// Fase 1: campos básicos. Fases futuras adicionarão mais campos aqui.
    /// </summary>
    public class RFAIResponse
    {
        /// <summary>
        /// Pensamentos internos do NPC — não são falados, usados para debug e futuras mecânicas.
        /// </summary>
        [JsonProperty("internal_thoughts")]
        public string InternalThoughts { get; set; } = "";

        /// <summary>
        /// O que o NPC fala em voz alta — exibido no dialogue box.
        /// </summary>
        [JsonProperty("response")]
        public string Response { get; set; } = "";

        /// <summary>
        /// Tom da resposta: friendly, neutral, suspicious, hostile, fearful.
        /// </summary>
        [JsonProperty("tone")]
        public string Tone { get; set; } = "neutral";
    }
}
