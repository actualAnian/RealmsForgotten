using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace RF_AIDialog
{
    /// <summary>
    /// Listens to clan defections (JoinKingdomByDefection) and fires two reactions
    /// on behalf of the betrayed kingdom's ruler:
    ///
    ///   1. Sets a PendingInitiativeReason on the betrayed king so that if the
    ///      player speaks with him, he leads with the betrayal — not the player.
    ///
    ///   2. Records a specific WorldHistory entry naming the deserter, so that
    ///      ALL NPCs know who betrayed whom and can reference it in dialogue.
    ///
    /// The war declaration and relation penalties are handled by ApplyInciteBreakAction
    /// (in StrategicIntrigue). This behavior only handles the narrative layer.
    /// </summary>
    public class BetrayalReactionBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(
                this, OnClanChangedKingdom);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnClanChangedKingdom(
            Clan clan,
            Kingdom oldKingdom,
            Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail,
            bool showNotification)
        {
            // Only react to genuine defections to another kingdom
            if (detail != ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection)
                return;
            if (oldKingdom == null || newKingdom == null || clan == null)
                return;
            // Ignore player clan defections — those are handled by player intent
            if (clan == Clan.PlayerClan)
                return;

            try
            {
                string deserterName  = clan.Name?.ToString()       ?? "Unknown Clan";
                string oldKingName   = oldKingdom.Name?.ToString() ?? "Unknown Kingdom";
                string newKingName   = newKingdom.Name?.ToString() ?? "Unknown Kingdom";

                // ── 1. WorldHistory — specific betrayal record ─────────────
                // Generic WarDeclared will also fire, but this entry names the
                // deserter so NPCs can reference the specific act of treachery.
                int day = CurrentDay();
                WorldHistoryStore.Instance?.AddEvent(
                    $"Clan {deserterName} betrayed {oldKingName} and defected to {newKingName}. " +
                    $"{oldKingName} declared war in vengeance.",
                    day);
                AIMemoryStore.AddClanMemory(
                    clan.StringId,
                    $"Clan {deserterName} betrayed {oldKingName} and defected to {newKingName}.",
                    day);
                AIMemoryStore.UpsertSummary(
                    "clan",
                    clan.StringId,
                    $"Clan {deserterName} is remembered for defecting from {oldKingName} to {newKingName} on day {day}.",
                    day);

                // ── 2. PendingInitiative on the betrayed ruler ────────────
                // If the player speaks with the betrayed king within the next
                // ~30 days, the king will take the initiative and lead with
                // this betrayal — it is the foremost thing on his mind.
                Hero betrayedRuler = oldKingdom.RulingClan?.Leader;
                if (betrayedRuler != null)
                {
                    var ctx = NPCContextStore.Instance?.GetOrCreate(betrayedRuler);
                    if (ctx != null && string.IsNullOrWhiteSpace(ctx.PendingInitiativeReason))
                    {
                        ctx.PendingInitiativeReason =
                            $"Clan {deserterName} has betrayed us and fled to {newKingName}. " +
                            $"We have declared war. The deserters will answer for their treachery. " +
                            $"Speak of this — your rage and resolve are foremost in your mind.";
                        NPCContextStore.Instance?.MarkDirty(ctx);
                    }
                }
            }
            catch { }
        }

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
}
