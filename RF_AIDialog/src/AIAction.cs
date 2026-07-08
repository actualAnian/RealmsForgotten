using Newtonsoft.Json;

namespace RF_AIDialog
{
    /// <summary>
    /// A single game action requested by the LLM in its JSON response.
    /// Executed by ActionExecutor after the NPC finishes speaking.
    /// </summary>
    public class AIAction
    {
        /// <summary>
        /// Action type. Valid values:
        ///   "relation_change" — change NPC-player relation
        ///   "give_gold"       — NPC gives gold to player
        ///   "take_gold"       — player pays the NPC
        ///   "give_item"       — NPC gives player an item
        ///   "take_item"       — player gives the NPC an item
        ///   "assign_role"     — companion: assign a party role (companions only)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        /// <summary>
        /// Numeric magnitude.
        /// relation_change : -5 to +5 (clamped by executor)
        /// give_gold / take_gold : amount in gold coins
        /// give_item / take_item : quantity (default 1)
        /// assign_role : unused (use role field)
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; } = 1;

        /// <summary>
        /// Item string ID — only for give_item / take_item.
        /// </summary>
        [JsonProperty("item_id")]
        public string? ItemId { get; set; }

        /// <summary>
        /// Role string — only for assign_role.
        /// Valid values: "engineer", "scout", "surgeon", "quartermaster"
        /// </summary>
        [JsonProperty("role")]
        public string? Role { get; set; }
    }
}
