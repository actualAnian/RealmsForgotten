using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// A significant world event captured in real time and read from external AI memory.
    /// Examples: wars declared, settlements captured, clans or kingdoms destroyed.
    /// </summary>
    public class WorldEvent
    {
        /// <summary>Human-readable description injected verbatim into NPC prompts.</summary>
        [JsonProperty("desc")]
        public string Description { get; set; } = "";

        /// <summary>In-game day when this event occurred.</summary>
        [JsonProperty("day")]
        public int Day { get; set; } = 0;
    }

    /// <summary>
    /// Keeps a lightweight in-session view of world history.
    /// Long-term world history is written to AIMemoryStore instead of the campaign save.
    /// Populated by WorldHistoryBehavior (event listeners).
    /// Read by WorldContext.BuildDynamicState for NPC prompt injection.
    /// </summary>
    public class WorldHistoryStore : CampaignBehaviorBase
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static WorldHistoryStore? Instance { get; private set; }

        // ── Data ──────────────────────────────────────────────────────────
        private List<WorldEvent> _events = new List<WorldEvent>();
        public const int MaxEvents = 40; // hard cap on in-session fallback events

        // ── Construction ──────────────────────────────────────────────────
        public WorldHistoryStore()
        {
            Instance = this;
        }

        // ── CampaignBehaviorBase ──────────────────────────────────────────
        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore) { }

        // ── Public API ────────────────────────────────────────────────────

        public void AddEvent(string description, int day)
        {
            if (string.IsNullOrWhiteSpace(description)) return;
            _events.Add(new WorldEvent { Description = description, Day = day });
            AIMemoryStore.AddWorldEvent(description, day);
            AIMemoryStore.UpsertSummary("world", "recent", $"Recent major event: {description}", day);
            while (_events.Count > MaxEvents)
                _events.RemoveAt(0);
        }

        /// <summary>
        /// Returns the most recent events — newest last, capped at maxCount.
        /// If maxDays > 0, also filters out events older than that many days.
        /// </summary>
        public List<WorldEvent> GetRecentEvents(int maxCount = 15, int maxDays = 0)
        {
            int currentDay = 0;
            try { currentDay = (int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow; } catch { }

            var external = AIMemoryStore.GetWorldEvents(maxCount, maxDays);
            if (external.Count > 0)
            {
                return external
                    .Select(e => new WorldEvent { Description = e.Text, Day = e.Day })
                    .ToList();
            }

            var result = new List<WorldEvent>();
            // Walk backwards (newest first), collect up to maxCount
            for (int i = _events.Count - 1; i >= 0 && result.Count < maxCount; i--)
            {
                var e = _events[i];
                if (maxDays > 0 && currentDay - e.Day > maxDays) break;
                result.Add(e);
            }
            result.Reverse(); // back to chronological order
            return result;
        }
    }
}
