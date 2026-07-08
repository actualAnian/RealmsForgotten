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
            RFAIDebug.Log($"NPCContextStore.SyncData: IsLoading={dataStore.IsLoading}");

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
                    RFAIDebug.Log($"NPCContextStore.SyncData: loaded {_contexts.Count} contexts OK");
                }
                catch (Exception ex)
                {
                    RFAIDebug.Log($"NPCContextStore.SyncData: deserialize FAILED — {ex.Message}");
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
        /// Returns all persisted contexts. Used by AIDialogBehavior.OnGameLoaded
        /// to reconstruct quest log entries without creating blank contexts.
        /// </summary>
        public IEnumerable<NPCContext> GetAll() => _contexts.Values;

        /// <summary>
        /// Pe