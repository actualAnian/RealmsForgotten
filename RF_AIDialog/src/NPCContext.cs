using System.Collections.Generic;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    /// <summary>
    /// A specific actionable request made by an NPC to the player.
    /// Created when the LLM includes a "request" field in its response.
    /// Cleared when the LLM marks "request_fulfilled": true.
    ///
    /// Examples:
    ///   "Bring me 10 units of grain before the harvest festival."
    ///   "Find out whether Vlandia is moving troops toward Pen Cannoc."
    ///   "Deliver this letter to the merchant Bolros in Epicrotea."
    /// </summary>
    public class PendingRequest
    {
        /// <summary>What the NPC asked for, in natural language.</summary>
        [JsonProperty("desc")]
        public string Description { get; set; } = "";

        /// <summary>In-game day when the request was made.</summary>
        [JsonProperty("day")]
        public int DayIssued { get; set; } = 0;

        /// <summary>
        /// Optional structured mechanic produced by the LLM.
        /// When present, QuestAtomEngine tracks objectives automatically
        /// against real game events (settlement visits, party defeats, etc.).
        /// When absent, the quest is pure roleplay — fulfilled by LLM judgement.
        /// </summary>
        [JsonProperty("mechanic")]
        public QuestMechanic? Mechanic { get; set; }
    }

    /// <summary>
    /// One raw exchange between player and NPC (short-term history).
    /// </summary>
    public class ConversationEntry
    {
        [JsonProperty("player")]
        public string Player { get; set; } = "";

        [JsonProperty("npc")]
        public string Npc { get; set; } = "";
    }

    /// <summary>
    /// A significant fact extracted by the LLM from a past conversation.
    /// Persists longer than raw history — up to MaxMemories entries.
    /// Examples: "Player paid 200 gold for safe passage through my lands."
    ///           "Player warned me of a plot against my clan."
    ///           "We agreed to support each other in the war against Battania."
    /// </summary>
    public class MemoryEntry
    {
        [JsonProperty("note")]
        public string Note { get; set; } = "";

        /// <summary>In-game day when this memory was formed.</summary>
        [JsonProperty("day")]
        public int Day { get; set; } = 0;
    }

    /// <summary>
    /// Lightweight receipt for AI quests that were completed. We keep this as
    /// plain JSON state, not as QuestBase, so the save system never has to
    /// serialize the custom quest class.
    /// </summary>
    public class CompletedRequestRecord
    {
        [JsonProperty("desc")]
        public string Description { get; set; } = "";

        [JsonProperty("day_issued")]
        public int DayIssued { get; set; }

        [JsonProperty("day_completed")]
        public int DayCompleted { get; set; }

        [JsonProperty("outcome")]
        public string Outcome { get; set; } = "";
    }

    /// <summary>
    /// Persistent per-NPC state: personality, conversation history,
    /// long-term memories, pending initiative, and last known relation.
    /// Serialized to the campaign save via NPCContextStore.
    /// </summary>
    public class NPCContext
    {
        [JsonProperty("hero_id")]
        public string HeroId { get; set; } = "";

        /// <summary>
        /// AI-generated personality summary — built on first contact,
        /// injected into all future prompts for consistency.
        /// </summary>
        [JsonProperty("personality")]
        public string GeneratedPersonality { get; set; } = "";

        /// <summary>
        /// AI-generated named ambition — built on first contact with a lord.
        /// Captures what they want most in the world: power, specific land, revenge,
        /// a legacy, protection of someone. Injected into all future prompts so their
        /// long-term drive stays consistent across sessions.
        /// Empty for wanderers and notables (they get personality but not ambition).
        /// </summary>
        [JsonProperty("ambition")]
        public string GeneratedAmbition { get; set; } = "";

        /// <summary>
        /// Last N raw conversation exchanges (short-term memory).
        /// </summary>
        [JsonProperty("history")]
        public List<ConversationEntry> RecentHistory { get; set; } = new List<ConversationEntry>();

        /// <summary>
        /// Significant facts extracted by the LLM from past conversations (long-term memory).
        /// One entry per notable exchange — trades, agreements, betrayals, gifts, warnings.
        /// Trivial small-talk does not generate entries.
        /// </summary>
        [JsonProperty("memories")]
        public List<MemoryEntry> Memories { get; set; } = new List<MemoryEntry>();

        /// <summary>
        /// A specific task this NPC has asked the player to fulfill.
        /// Set when the LLM includes a "request" in its response.
        /// Cleared when the LLM reports "request_fulfilled": true.
        /// Persists across conversations until resolved or the NPC is no longer reachable.
        /// </summary>
        [JsonProperty("pending_request")]
        public PendingRequest? PendingRequest { get; set; }

        /// <summary>
        /// Recently completed AI requests, used only to rebuild completed-looking
        /// journal entries after load. This stays small and contains no QuestBase.
        /// </summary>
        [JsonProperty("completed_requests")]
        public List<CompletedRequestRecord> CompletedRequests { get; set; } = new List<CompletedRequestRecord>();

        /// <summary>
        /// Reason this NPC wants to initiate a conversation with the player.
        /// Set by NPCInitiativeBehavior. Cleared after the conversation ends.
        /// </summary>
        [JsonProperty("pending_initiative")]
        public string? PendingInitiativeReason { get; set; }

        /// <summary>
        /// Relation score at the end of the last conversation.
        /// Used to detect drops that trigger a grievance initiative.
        /// </summary>
        [JsonProperty("last_known_relation")]
        public int LastKnownRelation { get; set; } = 0;

        /// <summary>
        /// Last in-game day on which this NPC created an AI request.
        /// Used as a lightweight cooldown so the same NPC does not offer work
        /// too frequently across conversations or save/load.
        /// </summary>
        [JsonProperty("last_request_day")]
        public int LastRequestDay { get; set; } = -100000;

        [JsonProperty("last_initiative_letter_day")]
        public int LastInitiativeLetterDay { get; set; } = -100000;

        // ── Limits ────────────────────────────────────────────────────────

        public static int MaxHistory  => 6;   // raw exchanges kept
        public static int MaxMemories => 10;  // semantic facts kept
        public static int MaxCompletedRequests => 3;
        public static int RequestCooldownDays => 10;
        public static int InitiativeLetterCooldownDays => 20;
        public static int GlobalInitiativeLetterSpacingDays => 4;

        // ── Computed ──────────────────────────────────────────────────────

        [JsonIgnore]
        public bool IsFirstConversation => string.IsNullOrWhiteSpace(GeneratedPersonality);

        [JsonIgnore]
        public bool HasGeneratedAmbition => !string.IsNullOrWhiteSpace(GeneratedAmbition);

        [JsonIgnore]
        public bool HasPendingRequest => PendingRequest != null
                                      && !string.IsNullOrWhiteSpace(PendingRequest.Description);

        [JsonIgnore]
        public bool HasPendingInitiative => !string.IsNullOrWhiteSpace(PendingInitiativeReason);

        // ── Mutation helpers ──────────────────────────────────────────────

        public void AddExchange(string playerText, string npcText)
        {
            RecentHistory.Add(new ConversationEntry { Player = playerText, Npc = npcText });
            while (RecentHistory.Count > MaxHistory)
                RecentHistory.RemoveAt(0);
        }

        public void AddMemory(string note, int currentDay)
        {
            if (string.IsNullOrWhiteSpace(note)) return;
            Memories.Add(new MemoryEntry { Note = note.Trim(), Day = currentDay });
            while (Memories.Count > MaxMemories)
                Memories.RemoveAt(0);
        }

        public void AddCompletedRequest(PendingRequest request, int currentDay, string outcome)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
                return;

            CompletedRequests.Add(new CompletedRequestRecord
            {
                Description = request.Description.Trim(),
                DayIssued = request.DayIssued,
                DayCompleted = currentDay,
                Outcome = string.IsNullOrWhiteSpace(outcome) ? "Completed" : outcome.Trim()
            });

            while (CompletedRequests.Count > MaxCompletedRequests)
                CompletedRequests.RemoveAt(0);
        }
    }
}
