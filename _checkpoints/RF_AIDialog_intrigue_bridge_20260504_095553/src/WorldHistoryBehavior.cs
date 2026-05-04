using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using DeclareWarDetail         = TaleWorlds.CampaignSystem.Actions.DeclareWarAction.DeclareWarDetail;
using MakePeaceDetail          = TaleWorlds.CampaignSystem.Actions.MakePeaceAction.MakePeaceDetail;
using ChangeOwnerOfSettlementDetail = TaleWorlds.CampaignSystem.Actions.ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail;

namespace RF_AIDialog
{
    /// <summary>
    /// Listens to significant campaign events and records them in WorldHistoryStore.
    /// Tracked events:
    ///   - Wars declared / peace signed
    ///   - Settlements captured (towns and castles only)
    ///   - Kingdoms destroyed
    ///   - Major clans destroyed
    /// </summary>
    public class WorldHistoryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.WarDeclared
                .AddNonSerializedListener(this, OnWarDeclared);

            CampaignEvents.MakePeace
                .AddNonSerializedListener(this, OnMakePeace);

            CampaignEvents.OnSettlementOwnerChangedEvent
                .AddNonSerializedListener(this, OnSettlementOwnerChanged);

            CampaignEvents.KingdomDestroyedEvent
                .AddNonSerializedListener(this, OnKingdomDestroyed);

            CampaignEvents.OnClanDestroyedEvent
                .AddNonSerializedListener(this, OnClanDestroyed);
        }

        public override void SyncData(IDataStore dataStore) { }

        // ── Helpers ───────────────────────────────────────────────────────

        private static int CurrentDay()
        {
            try { return (int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow; }
            catch { return 0; }
        }

        private static void Record(string description)
        {
            try { WorldHistoryStore.Instance?.AddEvent(description, CurrentDay()); }
            catch { }
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarDetail detail)
        {
            try
            {
                Record($"War declared between {faction1?.Name} and {faction2?.Name}.");
            }
            catch { }
        }

        private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceDetail detail)
        {
            try
            {
                Record($"Peace signed between {faction1?.Name} and {faction2?.Name}.");
            }
            catch { }
        }

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
                // Only record towns and castles — village ownership changes are too frequent
                if (!settlement.IsTown && !settlement.IsCastle) return;

                string type         = settlement.IsTown ? "Town" : "Castle";
                string newFaction   = newOwner?.MapFaction?.Name?.ToString() ?? newOwner?.Clan?.Name?.ToString() ?? "unknown";
                string oldFaction   = oldOwner?.MapFaction?.Name?.ToString() ?? oldOwner?.Clan?.Name?.ToString() ?? "unknown";

                string how = detail == ChangeOwnerOfSettlementDetail.BySiege    ? "by siege"   :
                             detail == ChangeOwnerOfSettlementDetail.ByBarter   ? "by trade"   :
                             detail == ChangeOwnerOfSettlementDetail.ByGift     ? "as a gift"  :
                             detail == ChangeOwnerOfSettlementDetail.ByKingDecision ? "by royal decree" : "";

                string howStr = string.IsNullOrEmpty(how) ? "" : $" ({how})";

                Record($"{type} {settlement.Name} captured from {oldFaction} by {newFaction}{howStr}.");
            }
            catch { }
        }

        private void OnKingdomDestroyed(Kingdom kingdom)
        {
            try
            {
                Record($"Kingdom {kingdom?.Name} has been destroyed — its lords are scattered and lands divided.");
            }
            catch { }
        }

        private void OnClanDestroyed(Clan clan)
        {
            try
            {
                // Skip minor factions and clans with no lords (bandits etc.)
                if (clan == null)                            return;
                if (clan.IsMinorFaction)                    return;
                if (clan.AliveLords == null || clan.AliveLords.Count == 0) return;

                string kingdomStr = clan.Kingdom != null
                    ? $" of {clan.Kingdom.Name}"
                    : "";

                Record($"Clan {clan.Name}{kingdomStr} has been destroyed.");
            }
            catch { }
        }
    }
}
