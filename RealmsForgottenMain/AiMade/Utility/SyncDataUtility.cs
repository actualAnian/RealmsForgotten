using System;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Utility
{
    public static class SyncDataUtility
    {
        // Specify the exact folder path
        private static readonly string logFolderPath = Globals.realmsForgottenAssembly.Location;
        private static readonly string logFilePath = Path.Combine(logFolderPath, "BannerlordSaveErrors.log");

        public static void Log(string message)
        {
            try
            {
                // Ensure the log folder exists
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
                InformationManager.DisplayMessage(new InformationMessage($"Log written: {message}")); // Log confirmation
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error writing to log file: {ex.Message}"));
            }
        }

        public static void SyncDataWithLogging(CampaignBehaviorBase behavior, IDataStore dataStore)
        {
            try
            {
                MethodInfo syncDataMethod = behavior.GetType().GetMethod("SyncData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (syncDataMethod != null)
                {
                    syncDataMethod.Invoke(behavior, new object[] { dataStore });
                    InformationManager.DisplayMessage(new InformationMessage($"SyncData called for {behavior.GetType().Name}")); // SyncData called confirmation
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"SyncData method not found for {behavior.GetType().Name}"));
                }
            }
            catch (Exception ex)
            {
                string behaviorName = behavior.GetType().Name;
                string errorMessage = $"Error syncing data in {behaviorName}: {ex.Message}";
                InformationManager.DisplayMessage(new InformationMessage($"Sync error in {behaviorName}: {ex.Message}")); // Sync error notification
                Log(errorMessage);
                Log(ex.StackTrace);
            }
        }
    }
}
