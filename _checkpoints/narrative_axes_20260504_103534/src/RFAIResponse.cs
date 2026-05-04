using System.Collections.Generic;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    /// <summary>
    /// Structure of the JSON response returned by the LLM.
    /// personality_summary is only requested on the NPC's first conversation.
    /// memory_note is optional — the LLM writes it when something significant happened.
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
        /// Generated only on first contact with a lord NPC.
        /// A single sentence capturing what this lord wants most in the world —
        /// power, specific land, revenge, a legacy, to protect someone.
        /// Stored in NPCContext and injected as a quiet motivation driver in all future prompts.
        /// Null/empty on subsequent conversations or for non-lord NPCs.
        /// </summary>
        [JsonProperty("ambition")]
        public string? Ambition { get; set; }

        /// <summary>
        /// NPC's internal thoughts — not spoken.
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
        //