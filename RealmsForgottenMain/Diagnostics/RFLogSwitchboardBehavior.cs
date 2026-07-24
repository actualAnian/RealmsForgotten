using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.Diagnostics
{
    /// <summary>
    /// Keeps the satellite modules' log switches in sync with the RF Diagnostics
    /// MCM page. MCM writes settings straight into the settings instance, so a
    /// player flipping a switch mid-session must have it pushed across the
    /// module boundary — a cheap hourly re-push does that without any MCM
    /// change-event plumbing.
    /// </summary>
    public class RFLogSwitchboardBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => RFLogSwitchboard.PushToModules());
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, RFLogSwitchboard.PushToModules);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // no state: the switches live in MCM settings
        }
    }
}
