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
        ///   "relation_change"       - change NPC-player relation
        ///   "give_gold"             - NPC gives gold to player
        ///   "take_gold"             - player pays the NPC
        ///   "give_item"             - NPC gives player an item
        ///   "take_item"             - player gives the NPC an item
        ///   "assign_role"           - companion: assign a party role
        ///   "go_to_settlement"      - lord party moves to a settlement
        ///   "wait_near_settlement"  - lord party waits/loiters near a settlement
        ///   "patrol_settlement"     - lord party patrols around a settlement
        ///   "attack_party"          - lord party engages a target party
        ///   "raid_village"          - lord party raids a hostile village
        ///   "siege_settlement"      - lord party besieges a hostile fortification
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        /// <summary>
        /// Numeric magnitude.
        /// relation_change : -5 to +5 (clamped by executor)
        /// give_gold / take_gold : amount in gold coins
        /// give_item / take_item : quantity (default 1)
        /// assign_role : unused (use role field)
        /// wait_near_settlement : hours hint (currently informational / clamped)
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; } = 1;

        /// <summary>
        /// Item string ID - only for give_item / take_item.
        /// </summary>
        [JsonProperty("item_id")]
        public string? ItemId { get; set; }

        /// <summary>
        /// Role string - only for assign_role.
        /// Valid values: "engineer", "scout", "surgeon", "quartermaster"
        /// </summary>
        [JsonProperty("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Settlement string ID - only for movement/settlement actions.
        /// </summary>
        [JsonProperty("settlement_id")]
        public string? SettlementId { get; set; }

        /// <summary>
        /// Party string ID - only for party-target actions.
        /// </summary>
        [JsonProperty("party_id")]
        public string? PartyId { get; set; }

        /// <summary>
        /// Optional duration hint for waiting behavior.
        /// </summary>
        [JsonProperty("hours")]
        public int Hours { get; set; } = 0;

        /// <summary>
        /// Optional radius hint for patrol behavior.
        /// </summary>
        [JsonProperty("radius")]
        public int Radius { get; set; } = 0;

        /// <summary>
        /// Optional narrative reason for the order.
        /// </summary>
        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }
}
