using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// Rebuilds compact external memory summaries from JSONL memory files.
    /// This behavior intentionally saves no Bannerlord state.
    /// </summary>
    public class AIMemorySummaryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, Rebuild);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, Rebuild);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void Rebuild()
        {
            AIMemoryStore.RebuildSummaries();
        }
    }
}
