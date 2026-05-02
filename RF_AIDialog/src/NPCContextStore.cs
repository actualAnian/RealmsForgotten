using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Persists NPCContext objects across campaign saves.
    /// Registered as a CampaignBehavior so SyncData is called automatically
    /// on save/load. Access via NPCContextStore.Instance.
    /// </summary>
    public class NPCContextStore : CampaignBehaviorBase
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static NPCContextStore? Instance { get; private set; }

        // ── Data ──────────────────────────────────────────────────────────
        private Dictionary<string, NPCContext> _contexts
            = new Dictionary<string, NPCContext>();

        // Serialized JSON — what IDataStore actually reads/writes
        private string _serialized = "";

        // ── Construction ──────────────────────────────────────────────────
        public NPCContextStore()
        {
            Instance = this;
        }

        // ── CampaignBehaviorBase ──────────────────────────────────────────
        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            // Serialize current state before saving
            if (!dataStore.IsLoading)
            {
                try   { _serialized = JsonConvert.SerializeObject(_contexts); }
                catch { _serialized = "{}"; }
            }

            dataStore.SyncData("RF_AI_NPCContexts", ref _serialized);

            // Deserialize after loading
            if (dataStore.IsLoading && !string.IsNullOrWhiteSpace(_serialized))
            {
                try
                {
                    _contexts = JsonConvert.DeserializeObject<Dictionary<string, NPCContext>>(_serialized)
                                ?? new Dictionary<string, NPCContext>();
                }
                catch
                {
                    _contexts = new Dictionary<string, NPCContext>();
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the NPCContext for this hero, creating a blank one if none exists yet.
        /// </summary>
        public NPCContext GetOrCreate(Hero hero)
        {
            if (hero == null) return new NPCContext();

            string id = hero.StringId;
            if (!_contexts.TryGetValue(id, out var ctx))
            {
                ctx = new NPCContext { HeroId = id };
                _contexts[id] = ctx;
            }
            return ctx;
        }

        /// <summary>
        /// Persists changes to an existing context (already referenced by the dictionary).
        /// Call after mutating the context returned by GetOrCreate.
        /// </summary>
        public void MarkDirty(NPCContext ctx)
        {
            // The dictionary already holds a reference to the same object,
            // so no explicit write-back is needed — this method exists as
            // a clear call-site signal that the context was mutated.
            if (ctx != null && !string.IsNullOrWhiteSpace(ctx.HeroId))
                _contexts[ctx.HeroId] = ctx;
        }
    }
}
