using TaleWorlds.CampaignSystem;

namespace RF_PartyVisuals
{
    /// <summary>
    /// Owns the enhancer instance and flips the "ready" flag once a campaign map actually exists,
    /// so the OnVisualTick patch does nothing during load. Visuals are transient (rebuilt from live
    /// parties each session) so there is nothing to serialize.
    /// </summary>
    public sealed class PartyVisualsBehavior : CampaignBehaviorBase
    {
        private PartyVisualsEnhancer _enhancer;

        public override void RegisterEvents()
        {
            _enhancer = new PartyVisualsEnhancer();
            PartyVisualsEnhancer.Ready = false;

            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () => PartyVisualsEnhancer.Ready = true);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, _ => PartyVisualsEnhancer.Ready = true);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, () => { });
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
