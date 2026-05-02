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
    /// Persistent per-NPC state: generated personality, conversation history.
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

        // Max entries kept in history
        public static int MaxHistory => 4;

        /// <summary>True on the very first AI conversation with this NPC.</summary>
        [JsonIgnore]
        public bool IsFirstConversation => string.IsNullOrWhiteSpace(GeneratedPersonality);

        /// <summary>
        /// Appends an exchange and trims to MaxHistory.
        /// </summary>
        public void AddExchange(string playerText, string npcText)
        {
            RecentHistory.Add(new ConversationEntry { Player = playerText, Npc = npcText });
            while (RecentHistory.Count > MaxHistory)
                RecentHistory.RemoveAt(0);
        }
    }
}
