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
                PrepareForSave();
                try   { _serialized = JsonConvert.SerializeObject(_contexts); }
                catch { _serialized = "{}"; }
                RFAIDebug.Log($"NPCContextStore.SyncData SAVE | contexts={_contexts.Count} | bytes={(_serialized?.Length ?? 0)}");
            }

            dataStore.SyncData("RF_AI_NPCContexts", ref _serialized);

            // Deserialize after loading
            if (dataStore.IsLoading && !string.IsNullOrWhiteSpace(_serialized))
            {
                try
                {
                    _contexts = JsonConvert.DeserializeObject<Dictionary<string, NPCContext>>(_serialized)
                                ?? new Dictionary<string, NPCContext>();
                    RFAIDebug.Log($"NPCContextStore.SyncData LOAD | contexts={_contexts.Count} | bytes={_serialized.Length}");
                }
                catch
                {
                    _contexts = new Dictionary<string, NPCContext>();
                    RFAIDebug.Log("NPCContextStore.SyncData LOAD | deserialize failed, contexts reset");
                }
            }
            else if (dataStore.IsLoading)
            {
                RFAIDebug.Log("NPCContextStore.SyncData LOAD | empty payload");
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
        /// Returns a context only if one already exists. This avoids creating
        /// thousands of blank NPC records from passive daily scans.
        /// </summary>
        public NPCContext? GetExisting(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId)) return null;
            return _contexts.TryGetValue(heroId, out var ctx) ? ctx : null;
        }

        /// <summary>
        /// Returns all persisted contexts. Used by ReconstructQuestsFromNPCContexts
        /// to rebuild quest log entries on load without creating blank contexts.
        /// </summary>
        public IEnumerable<NPCContext> GetAll() => _contexts.Values;

        /// <summary>
        /// Persists changes to an existing context (already referenced by the dictionary).
        /// Call after mutating the context returned by GetOrCreate.
        /// </summary>
        public void MarkDirty(NPCContext ctx)
        {
            // The dictionary already holds a reference to the same object,
            // so mutations are visible immediately. This method is a no-op
            // kept for call-site clarity and future extensibility.
        }

        private void PrepareForSave()
        {
            try
            {
                var remove = new List<string>();

                foreach (var pair in _contexts)
                {
                    var ctx = pair.Value;
                    if (ctx == null)
                    {
                        remove.Add(pair.Key);
                        continue;
                    }

                    // Migrate legacy save-backed semantic memories out of the save.
                    if (ctx.Memories != null && ctx.Memories.Count > 0)
                    {
                        foreach (var mem in ctx.Memories)
                        {
                            if (!string.IsNullOrWhiteSpace(mem.Note))
                                AIMemoryStore.AddNpcMemory(ctx.HeroId, mem.Note, mem.Day);
                        }
                        ctx.Memories.Clear();
                    }

                    if (!ShouldPersist(ctx))
                        remove.Add(pair.Key);
                }

                foreach (string key in remove)
                    _contexts.Remove(key);
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"NPCContextStore.PrepareForSave failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static bool ShouldPersist(NPCContext ctx)
        {
            if (ctx == null)
                return false;

            return !string.IsNullOrWhiteSpace(ctx.GeneratedPersonality)
                || !string.IsNullOrWhiteSpace(ctx.GeneratedAmbition)
                || (ctx.RecentHistory != null && ctx.RecentHistory.Count > 0)
                || (ctx.CompletedRequests != null && ctx.CompletedRequests.Count > 0)
                || ctx.HasPendingRequest
                || ctx.HasPendingInitiative
                || ctx.LastKnownRelation != 0
                || ctx.LastRequestDay > -100000
                || ctx.LastInitiativeLetterDay > -100000;
        }
    }
}
