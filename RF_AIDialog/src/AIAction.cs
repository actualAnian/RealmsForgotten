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
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        /// <summary>
        /// Numeric magnitude.
        /// relation_change : -5 to +5 (clamped by executor)
        /// give_gold / take_gold : amount in gold coins
        /// give_item : quantity (default 1)
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; } = 1;

        /// <summary>
        /// Item string ID — only used for give_item.
        /// Must be one of the IDs listed in the prompt.
        /// </summary>
        [JsonProperty("item_id")]
        public string? ItemId { get; set; }
    }
}
