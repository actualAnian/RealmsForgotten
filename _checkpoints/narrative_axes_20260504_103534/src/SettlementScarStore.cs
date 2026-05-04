using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using ChangeOwnerOfSettlementDetail = TaleWorlds.CampaignSystem.Actions.ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail;

namespace RF_AIDialog
{
    /// <summary>
    /// Records the conquest history of settlements — who took them, from whom,
    /// and how many times they changed hands during this campaign.
    ///
    /// When a lord speaks about their holdings, this data lets the LLM know
    /// the bloody or peaceful history behind each fief. A town taken by siege
    /// from an old enemy carries a different emotional weight than one inherited
    /// or gifted. NPCs never recite this mechanically — it colours the depth and
    /// pride (or shame, or defiance) with which they speak of their lands.
    ///
    /// Scars are written once per ownership change via BySiege. The most recent
    /// conquest is what is injected — earlier ones are captured in TimesChanged.
    /// </summary>
    public class SettlementScarStore : CampaignBehaviorBase
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static SettlementScarStore? Instance { get; private set; }

        // ── Storage ───────────────────────────────────────────────────────
        // Key: Settlement.StringId (stable across saves)
        private Dictionary<string, SettlementScar> _scars = new Dictionary<string, SettlementScar>();

        // ── Lifecycle ─────────────────────────────────────────────────────

        public override void RegisterEvents()
        {
            Instance = this;

            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(
                this, OnSettlementOwnerChanged);
        }

        public override void SyncData(IDataStore dataStore)
        {
            Instance = this;

            string scarJson = "";

            // Serialize before handing to SyncData (saving path)
            if (!dataStore.IsLoading)
            {
                try { scarJson = JsonConvert.SerializeObject(_scars); }
                catch { scarJson = "{}"; }
            }

            dataStore.SyncData("SettlementScars_v1", ref scarJson);

            // Deserialize after SyncData sets the value (loading path)
            if (dataStore.IsLoading && !string.IsNullOrWhiteSpace(scarJson))
            {
                try
                {
                    _scars = JsonConvert.DeserializeObject<Dictionary<string, SettlementScar>>(scarJson)
                             ?? new Dictionary<string, SettlementScar>();
                }
                catch { _scars = new Dictionary<string, SettlementScar>(); }
            }
        }

        // ── Event handler ─────────────────────────────────────────────────

        private void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementDetail detail)
        {
            try
            {
                // Only record genuine conquest — not gifts, inheritance, or administrative transfers
                if (detail != ChangeOwnerOfSettlementDetail.BySiege) return;
                if (settlement == null || !settlement.IsFortification) return;

                string id = settlement.StringId;
                if (string.IsNullOrEmpty(id)) return;

                int day = CurrentDay();

                string newKingdomName  = newOwner?.Clan?.Kingdom?.Name?.ToString()  ?? newOwner?.Clan?.Name?.ToString()  ?? "Unknown";
                string prevKingdomName = oldOwner?.Clan?.Kingdom?.Name?.ToString()  ?? oldOwner?.Clan?.Name?.ToString()  ?? "Unknown";
                string capturerName    = capturerHero?.Name?.ToString() ?? newOwner?.Name?.ToString() ?? "Unknown";

                if (_scars.TryGetValue(id, out SettlementScar existing))
                {
                    // Update existing scar — increment counter
                    existing.CapturerKingdomName   = newKingdomName;
                    existing.PreviousOwnerKingdomName = prevKingdomName;
                    existing.CapturerHeroName       = capturerName;
                    existing.DayOfCapture           = day;
                    existing.TimesChanged           += 1;
                }
                else
                {
                    _scars[id] = new SettlementScar
                    {
                        SettlementId              = id,
                        CapturerKingdomName       = newKingdomName,
                        PreviousOwnerKingdomName  = prevKingdomName,
                        CapturerHeroName          = capturerName,
                        DayOfCapture              = day,
                        TimesChanged              = 1
                    };
                }
            }
            catch { }
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Returns the scar for a settlement, or null if never captured by siege.</summary>
        public SettlementScar? GetScar(Settlement settlement)
        {
            if (settlement == null) return null;
            _scars.TryGetValue(settlement.StringId, out SettlementScar scar);
            return scar;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static int CurrentDay()
        {
            try
            {
                return (int)Campaign.Current.Models.CampaignTimeModel
                    .CampaignStartTime.ElapsedDaysUntilNow;
            }
            catch { return 0; }
        }
    }

    /// <summary>
    /// The recorded conquest history of a single settlement.
    /// </summary>
    public class SettlementScar
    {
        [JsonProperty("id")]
        public string SettlementId { get; set; } = "";

        [JsonProperty("capturer_kingdom")]
        public string CapturerKingdomName { get; set; } = "";

        [JsonProperty("prev_owner_kingdom")]
        public string PreviousOwnerKingdomName { get; set; } = "";

        [JsonProperty("capturer_hero")]
        public string CapturerHeroName { get; set; } = "";

        [JsonProperty("day")]
        public int DayOfCapture { get; set; } = 0;

        [JsonProperty("times_changed")]
        public int TimesChanged { get; set; } = 0;
    }
}
