using System.Collections.Generic;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    /// <summary>
    /// One exchange between player and NPC stored in history.
    /// </summary>
    public class ConversationEntry
    {
        [JsonProperty("player")]
        public string Player { get; set; } = "";

        [JsonProperty("npc")]
        public string Npc { get; set; } = "";
    }

    /// <summary>
    /// Persistent per-NPC state: generated personality, conversation history,
    /// pending initiative, and last known relation.
    /// Serialized to the campaign save via NPCContextStore.
    /// </summary>
    public class NPCContext
    {
        [JsonProperty("hero_id")]
        public string HeroId { get; set; } = "";

        /// <summary>
        /// AI-generated personality summary — built from game data on first contact,
        /// then injected into every subsequent prompt so the NPC stays consistent.
        /// Empty string = not yet generated.
        /// </summary>
        [JsonProperty("personality")]
        public string GeneratedPersonality { get; set; } = "";

        /// <summary>
        /// Last N conversation exchanges, oldest first.
        /// Gives the NPC real memory across dialogue sessions.
        /// </summary>
        [JsonProperty("history")]
        public List<ConversationEntry> RecentHistory { get; set; } = new List<ConversationEntry>();

        /// <summary>
        /// Reason this NPC wants to initiate a conversation with the player.
        /// Set by NPCInitiativeBehavior when conditions are met.
        /// Cleared after the conversation ends.
        /// Null/empty = no pending initiative.
        /// </summary>
        [JsonProperty("pending_initiative")]
        public string? PendingInitiativeReason { get; set; }

        /// <summary>
        /// Relation score at the end of the last conversation.
        /// Used to detect significant drops that trigger a grievance initiative.
        /// </summary>
        [JsonProperty("last_kno