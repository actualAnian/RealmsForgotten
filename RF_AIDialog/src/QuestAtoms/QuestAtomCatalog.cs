using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_AIDialog
{
    /// <summary>
    /// Builds the quest-atom catalog string that is injected into the LLM system prompt.
    ///
    /// The catalog has three parts:
    ///   1. Static atom-type docs — what each atom does, what params it needs.
    ///   2. Dynamic settlement list — real StringIds the LLM can reference.
    ///   3. Dynamic faction list   — real StringIds the LLM can reference.
    ///
    /// Injected only for lords and notables (not companions).
    /// </summary>
    public static class QuestAtomCatalog
    {
        // ── Static docs ───────────────────────────────────────────────────

        private const string AtomDocs =
@"QUEST MECHANIC SYSTEM — READ CAREFULLY:
You may attach a 'quest_mechanic' object to ANY 'request' that involves verifiable
real-world conditions (travel, combat, inventory, troop count).

When 'quest_mechanic' is present the game engine tracks completion automatically —
do NOT rely on the player telling you they finished. The game will tell YOU.

OMIT 'quest_mechanic' for pure information/roleplay requests (e.g. 'find out rumors').

ATOM TYPES — use these in quest_mechanic.objectives[]:
  VISIT_SETTLEMENT
    desc: player must travel to a specific settlement
    params: settlement_id (string — use ids from the list below), [label (string)]
    example: {""atom"":""VISIT_SETTLEMENT"",""params"":{""settlement_id"":""town_ES3""},""label"":""Go to Epicrotea""}

  DEFEAT_PARTY
    desc: player must defeat N enemy parties of a given faction
    params: faction_id (string — use ids from the list below),
            faction_name (string — human-readable name),
            count (string-encoded int, default ""1"")
    example: {""atom"":""DEFEAT_PARTY"",""params"":{""faction_id"":""vlandia"",""faction_name"":""Vlandian"",""count"":""2""},""label"":""Rout 2 Vlandian patrols""}

  BRING_ITEM
    desc: player must carry a specific trade good when they next speak to you
    params: item_id (string — one of: grain wine hides linen tools silver_ore wool pottery salt dates),
            quantity (string-encoded int, default ""1"")
    example: {""atom"":""BRING_ITEM"",""params"":{""item_id"":""grain"",""quantity"":""10""},""label"":""Bring 10 units of grain""}

  BRING_TROOPS
    desc: player must have at least N healthy soldiers when they next speak to you
    params: troop_count (string-encoded int)
    example: {""atom"":""BRING_TROOPS"",""params"":{""troop_count"":""50""},""label"":""Muster 50 soldiers""}

  RETURN_TO_NPC
    desc: player must return to YOU after all prior objectives are complete
    params: (none required)
    example: {""atom"":""RETURN_TO_NPC"",""params"":{},""label"":""Come back and report""}

Objectives are evaluated IN ORDER. Put RETURN_TO_NPC last if used.
'reward_gold' is paid automatically when ALL objectives are complete.
'days' is the quest deadline (default 30).";

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the full catalog string for injection into the system prompt.
        /// All game data calls are wrapped in try/catch — never throws.
        /// </summary>
        public static string Build(Hero npc)
        {
            var sb = new StringBuilder();
            sb.AppendLine(AtomDocs);
            sb.AppendLine();

            // Dynamic settlement list
            try
            {
                var settlements = GetSettlements(npc, maxCount: 10);
                if (settlements.Count > 0)
                {
                    sb.AppendLine("VALID settlement_id VALUES (use these — do not invent ids):");
                    foreach (var (id, name, type) in settlements)
                        sb.AppendLine($"  \"{id}\"  →  {name} ({type})");
                    sb.AppendLine();
                }
            }
            catch { }

            // Dynamic faction list
            try
            {
                var factions = GetFactions(maxCount: 10);
                if (factions.Count > 0)
                {
                    sb.AppendLine("VALID faction_id VALUES (use these — do not invent ids):");
                    foreach (var (id, name) in factions)
                        sb.AppendLine($"  \"{id}\"  →  {name}");
                    sb.AppendLine();
                }
            }
            catch { }

            return sb.ToString().TrimEnd();
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static List<(string id, string name, string type)> GetSettlements(Hero npc, int maxCount)
        {
            var result = new List<(string, string, string)>();
            var seen   = new HashSet<string>();

            void Add(Settlement s)
            {
                if (seen.Contains(s.StringId)) return;
                seen.Add(s.StringId);
                string type = s.IsTown ? "town" : s.IsCastle ? "castle" : "village";
                result.Add((s.StringId, s.Name.ToString(), type));
            }

            // 1. NPC's current settlement (most contextually relevant)
            if (npc.CurrentSettlement != null)
                Add(npc.CurrentSettlement);

            // 2. NPC's own faction (towns + castles)
            if (npc.MapFaction != null)
            {
                foreach (var s in Settlement.All)
                {
                    if (result.Count >= maxCount) break;
                    if (!s.IsTown && !s.IsCastle) continue;
                    if (s.MapFaction == npc.MapFaction) Add(s);
                }
            }

            // 3. Fill remaining slots with towns from other kingdoms
            foreach (var s in Settlement.All)
            {
                if (result.Count >= maxCount) break;
                if (!s.IsTown) continue;
                Add(s);
            }

            return result;
        }

        private static List<(string id, string name)> GetFactions(int maxCount)
        {
            var result = new List<(string, string)>();
            foreach (var k in Kingdom.All)
            {
                if (k.IsEliminated) continue;
                result.Add((k.StringId, k.Name.ToString()));
                if (result.Count >= maxCount) break;
            }
            return result;
        }
    }
}
