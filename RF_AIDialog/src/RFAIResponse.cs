using System.Collections.Generic;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    /// <summary>
    /// Structure of the JSON response returned by the LLM.
    /// personality_summary is only requested on the NPC's first conversation.
    /// </summary>
    public class RFAIResponse
    {
        /// <summary>
        /// Generated only on first contact with this NPC.
        /// Stored in NPCContext and injected into all future prompts
        /// so the character stays consistent across sessions.
        /// Null/empty on subsequent conversations.
        /// </summary>
        [JsonProperty("personality_summary")]
        public string? PersonalitySummary { get; set; }

        /// <summary>
        /// NPC's internal thoughts — not spoken, used for debug and future mechanics.
        /// </summary>
        [JsonProperty("internal_thoughts")]
        public string InternalThoughts { get; set; } = "";

        /// <summary>
        /// What the NPC says out loud — displayed in the dialogue box.
        /// </summary>
        [JsonProperty("response")]
        public string Response { get; set; } = "";

        /// <summary>
        /// Tone: friendly, neutral, suspicious, hostile, fearful.
        /// </summary>
        [JsonProperty("tone")]
        public string Tone { get; set; } = "neutral";

        /// <summary>
        /// Optional game actions to execute after the NPC speaks.
        /// Null or empty = no actions.
        /// </summary>
        [JsonProperty("actions")]
        public List<AIAction>? Actions { get; set; }
    }
}
