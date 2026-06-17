using System.Diagnostics;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Utility
{
    public class TickProfilerBehavior : CampaignBehaviorBase
    {
        private const string LogPath = "tick_stack_log.txt";

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnHourlyTick()
        {
            LogStack(nameof(OnHourlyTick));
        }

        private void OnDailyTick()
        {
            LogStack(nameof(OnDailyTick));
        }

        private void LogStack(string methodName)
        {
            var stackTrace = new StackTrace(true); // true includes file info (if available)
            string stackString = $"=== {methodName} Call Stack ===\n{stackTrace}\n";

            // Show first line of caller info in game
            var frame = stackTrace.GetFrame(1);
            var caller = frame != null ? frame.GetMethod().Name : "Unknown";
            MBInformationManager.AddQuickInformation(
                new TextObject($"{methodName} called by: {caller}"), 0, null);

            // Append full trace to log file
            File.AppendAllText(LogPath, stackString);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}