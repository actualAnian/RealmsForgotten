using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    /// <summary>
    /// Builds the quest-atom catalog injected into the LLM system prompt.
    ///
    /// Settlement IDs are loaded ONCE from RF_Map/ModuleData/settlements.xml
    /// (the authoritative source for this mod's map) and cached for the session.
    /// At runtime they are filtered by the NPC's culture so the LLM receives
    /// only contextually relevant IDs (max ~15), keeping prompt size manageable.
    ///
    /// Faction IDs come from the live Kingdom list (they can be eliminated mid-game).
    /// </summary>
    public static class QuestAtomCatalog
    {
        // ── Static atom docs ──────────────────────────────────────────────

        private const string AtomDocs =
@"QUEST MECHANIC SYSTEM — READ CAREFULLY:
Attach 'quest_mechanic' to any 'request' that involves verifiable real-world
conditions (travel, combat, inventory, troop count).
The game engine tracks completion automatically — do NOT rely on the player claiming
they finished. OMIT 'quest_mechanic' for pure information/roleplay requests.

QUEST KINDS (use exactly one in quest_mechanic.quest_kind):
  travel_report  — go somewhere, then return with news
  delivery       — bring goods, then return
  recruitment    — gather troops, then return
  retaliation    — defeat enemy parties, then return
  scouting       — make contact with enemy parties/armies near a frontier or holding, then return
  delivery_under_pressure — carry goods to a destination despite danger, then return
  escort_with_ambush — travel toward a destination, repel attackers, then return
  capture_prisoner — capture a named hero and bring them back as prisoner
  courtship_tournament — win one or more tournaments, then return to claim favor

ATOM TYPES (use in quest_mechanic.objectives[]):

  VISIT_SETTLEMENT  — player must travel to a specific settlement
    params: settlement_id (use ids from the list below), [label]
    example: {""atom"":""VISIT_SETTLEMENT"",""params"":{""settlement_id"":""town_EN1""},""label"":""Go to Aispur""}

  LEAVE_SETTLEMENT  — player must leave a specific settlement after visiting it
    params: settlement_id (use ids from the list below), [label]
    example: {""atom"":""LEAVE_SETTLEMENT"",""params"":{""settlement_id"":""town_EN1""},""label"":""Leave Aispur and begin the count""}

  DEFEAT_PARTY  — player must defeat N enemy parties of a given faction, hero, or exact party
    params: [faction_id] (use ids from the list below), [faction_name], [party_id], [hero_id], count (string int, default ""1"")
    example: {""atom"":""DEFEAT_PARTY"",""params"":{""faction_id"":""vlandia"",""faction_name"":""Vlandian"",""count"":""3""},""label"":""Rout 3 Vlandian patrols""}

  TALK_TO_PARTY  — player must make contact with N distinct mobile parties or armies
    params: faction_id (use ids from the list below), [count] (string int, default ""1"")
    optional params: party_id, hero_id, character_id, settlement_id, radius
    settlement_id + radius means only count contacts currently near that settlement/frontier reference.
    Use hero_id for live heroes/notables; use character_id for CharacterObject/template targets.
    example: {""atom"":""TALK_TO_PARTY"",""params"":{""faction_id"":""vlandia"",""settlement_id"":""town_EN1"",""radius"":""80"",""count"":""1""},""label"":""Question a Vlandian party or army moving near Aispur""}

  BRING_ITEM  — player must carry the item when next speaking to you
    params: item_id (one of: grain wine hides linen tools silver_ore wool pottery salt dates), quantity (string int)
    example: {""atom"":""BRING_ITEM"",""params"":{""item_id"":""grain"",""quantity"":""10""},""label"":""Bring 10 grain""}

  BRING_TROOPS  — player must have at least N healthy soldiers when next speaking to you
    params: troop_count (string int)
    example: {""atom"":""BRING_TROOPS"",""params"":{""troop_count"":""50""},""label"":""Muster 50 soldiers""}

  BRING_PRISONER_HERO  — player must return while holding a specific hero as prisoner
    params: hero_id, [faction_id]
    example: {""atom"":""BRING_PRISONER_HERO"",""params"":{""hero_id"":""derthert"",""faction_id"":""vlandia""},""label"":""Bring King Derthert back alive as your prisoner""}

  WIN_TOURNAMENT  — player must win N tournaments, optionally in a specific town
    params: count (string int, default ""1""), [town_id]
    example: {""atom"":""WIN_TOURNAMENT"",""params"":{""count"":""3""},""label"":""Win 3 tournaments to prove your worth""}

  RETURN_TO_NPC  — player must return to YOU after all prior objectives are done
    params: (none)
    example: {""atom"":""RETURN_TO_NPC"",""params"":{},""label"":""Come back and report""}

Rules:
• Prefer the quest kinds above. Build a mechanic that fits ONE archetype only.
• Objectives are validated IN ORDER. Put RETURN_TO_NPC LAST.
• The game validates quest quality before accepting a mechanic. If the quest kind is not in allowed_quest_kinds, if a local quest targets a far settlement, or if a lord asks for war work against a non-enemy/same faction, the mechanic will be discarded.
• Use LEAVE_SETTLEMENT when the player must depart from a town/castle; do NOT fake this with VISIT_SETTLEMENT.
• Use TALK_TO_PARTY when the task is to scout, count, question, or make contact with mobile parties/caravans/lords/armies on the map.
• For war scouting, do NOT ask for patrols circling another faction's settlement. Patrols usually stay in their own territory. Ask the player to find an enemy party or army near local holdings/frontier instead.
• When RETURN_TO_NPC is present, reward and closure happen in the return conversation
  via 'request_fulfilled': true. Do NOT include both RETURN_TO_NPC and expect automatic closure.
• 'reward_gold' is paid when 'request_fulfilled': true fires in the conversation.
• 'days' is the quest deadline (default 30).";

        // ── XML cache ─────────────────────────────────────────────────────

        // Key: culture string (e.g. "empire", "vlandia"). Value: list of (id, name, type).
        private static Dictionary<string, List<(string id, string name, string type)>>? _byCulture;
        private static readonly object _lock = new object();

        private static Dictionary<string, List<(string id, string name, string type)>> GetCache()
        {
            if (_byCulture != null) return _byCulture;
            lock (_lock)
            {
                if (_byCulture != null) return _byCulture;
                _byCulture = LoadFromXml();
            }
            return _byCulture;
        }

        private static Dictionary<string, List<(string id, string name, string type)>> LoadFromXml()
        {
            var result = new Dictionary<string, List<(string, string, string)>>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                string path = Path.Combine(
                    BasePath.Name,
                    "Modules", "RF_Map", "ModuleData", "settlements.xml");

                if (!File.Exists(path))
                {
                    RFAIDebug.Log($"QuestAtomCatalog: settlements.xml not found at {path}");
                    return result;
                }

                var doc = XDocument.Load(path);
                int loaded = 0;

                foreach (var s in doc.Root?.Elements("Settlement") ?? Enumerable.Empty<XElement>())
                {
                    string id = s.Attribute("id")?.Value ?? "";
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    // Villages are not useful quest destinations — skip them
                    bool hasVillage  = s.Descendants("Village").Any();
                    if (hasVillage && !s.Descendants("Town").Any()) continue;

                    // Castle or town?
                    var townEl = s.Descendants("Town").FirstOrDefault();
                    if (townEl == null) continue; // no Town component at all

                    bool isCastle = townEl.Attribute("is_castle")?.Value
                                       ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
                    string type  = isCastle ? "castle" : "town";

                    // Human-readable name — strip localization key "{=key}Actual Name"
                    string rawName = s.Attribute("name")?.Value ?? id;
                    string name    = StripLocKey(rawName);

                    // Culture string, e.g. "Culture.empire" → "empire"
                    string rawCulture = s.Attribute("culture")?.Value ?? "";
                    string culture    = rawCulture.Replace("Culture.", "").Trim();
                    if (string.IsNullOrWhiteSpace(culture)) culture = "unknown";

                    if (!result.TryGetValue(culture, out var list))
                    {
                        list = new List<(string, string, string)>();
                        result[culture] = list;
                    }
                    list.Add((id, name, type));
                    loaded++;
                }

                RFAIDebug.Log($"QuestAtomCatalog: loaded {loaded} towns/castles from XML " +
                              $"across {result.Count} cultures");
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QuestAtomCatalog: XML load failed — {ex.Message}");
            }

            return result;
        }

        private static string StripLocKey(string raw)
        {
            int end = raw.IndexOf('}');
            if (end >= 0 && end < raw.Length - 1)
                return raw.Substring(end + 1).Trim();
            return raw;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the full catalog string for injection into the system prompt.
        /// Never throws — all game data access is guarded.
        /// </summary>
        public static string Build(Hero npc)
        {
            var sb = new StringBuilder();
            sb.AppendLine(AtomDocs);
            sb.AppendLine();

            // Settlement list filtered by NPC culture
            try
            {
                var settlements = GetSettlementsForNpc(npc, maxCount: 15);
                if (settlements.Count > 0)
                {
                    sb.AppendLine("VALID settlement_id VALUES (use these exactly — do not invent ids):");
                    foreach (var (id, name, type) in settlements)
                        sb.AppendLine($"  \"{id}\"  →  {name} ({type})");
                    sb.AppendLine();
                }
            }
            catch { }

            // Live faction list
            try
            {
                var factions = GetLiveFactions(maxCount: 10);
                if (factions.Count > 0)
                {
                    sb.AppendLine("VALID faction_id VALUES (active kingdoms — do not invent ids):");
                    foreach (var (id, name) in factions)
                        sb.AppendLine($"  \"{id}\"  →  {name}");
                    sb.AppendLine();
                }
            }
            catch { }

            return sb.ToString().TrimEnd();
        }

        public static bool IsValidSettlementId(string settlementId)
        {
            if (string.IsNullOrWhiteSpace(settlementId))
                return false;

            try
            {
                var cache = GetCache();
                foreach (var list in cache.Values)
                {
                    if (list.Any(e => e.id.Equals(settlementId, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            catch { }

            return false;
        }

        public static bool IsValidFactionId(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
                return false;

            try
            {
                return Kingdom.All.Any(k =>
                    !k.IsEliminated &&
                    k.StringId.Equals(factionId, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static List<(string id, string name, string type)> GetSettlementsForNpc(
            Hero npc, int maxCount)
        {
            var cache   = GetCache();
            var result  = new List<(string, string, string)>();
            var seenIds = new HashSet<string>();

            // Derive culture string from the NPC's faction/clan culture
            string npcCulture = npc.MapFaction?.Culture?.StringId
                             ?? npc.Culture?.StringId
                             ?? "";
            npcCulture = npcCulture.Replace("Culture.", "").Trim();

            // 1. NPC's own culture first (most relevant)
            if (!string.IsNullOrWhiteSpace(npcCulture)
                && cache.TryGetValue(npcCulture, out var ownList))
            {
                // Towns before castles — towns are better quest destinations
                foreach (var entry in ownList.Where(e => e.type == "town"))
                {
                    if (result.Count >= maxCount) break;
                    if (seenIds.Add(entry.id)) result.Add(entry);
                }
                foreach (var entry in ownList.Where(e => e.type == "castle"))
                {
                    if (result.Count >= maxCount) break;
                    if (seenIds.Add(entry.id)) result.Add(entry);
                }
            }

            // 2. Fill remaining with towns from all other cultures
            if (result.Count < maxCount)
            {
                foreach (var list in cache.Values)
                {
                    foreach (var entry in list.Where(e => e.type == "town"))
                    {
                        if (result.Count >= maxCount) break;
                        if (seenIds.Add(entry.id)) result.Add(entry);
                    }
                    if (result.Count >= maxCount) break;
                }
            }

            return result;
        }

        private static List<(string id, string name)> GetLiveFactions(int maxCount)
        {
            var result = new List<(string, string)>();
            try
            {
                foreach (var k in Kingdom.All)
                {
                    if (k.IsEliminated) continue;
                    result.Add((k.StringId, k.Name.ToString()));
                    if (result.Count >= maxCount) break;
                }
            }
            catch { }
            return result;
        }
    }
}
