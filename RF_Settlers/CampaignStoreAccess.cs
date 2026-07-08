using System;
using System.Collections;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RF_Settlers
{
    /// <summary>
    /// Reads a campaign behavior's SyncData store EARLY — inside
    /// RegisterSubModuleObjects, before campaign deserialization has run. The
    /// store dictionary already holds the raw save data at that point; vanilla
    /// just has no public accessor for it. Same reflection path the Player
    /// Settlement mod uses (Campaign._campaignBehaviorManager →
    /// _campaignBehaviorDataStore → _behaviorDict[behavior.StringId]).
    /// </summary>
    public static class CampaignStoreAccess
    {
        public static IDataStore GetStore(Campaign campaign, CampaignBehaviorBase behavior)
        {
            try
            {
                object manager = AccessTools.Field(typeof(Campaign), "_campaignBehaviorManager")?.GetValue(campaign);
                if (manager == null)
                {
                    return null;
                }

                object dataStore = AccessTools.Field(manager.GetType(), "_campaignBehaviorDataStore")?.GetValue(manager);
                if (dataStore == null)
                {
                    return null;
                }

                if (AccessTools.Field(dataStore.GetType(), "_behaviorDict")?.GetValue(dataStore) is not IDictionary behaviorDict)
                {
                    return null;
                }

                string key = behavior.StringId;
                if (behaviorDict.Contains(key))
                {
                    return behaviorDict[key] as IDataStore;
                }
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Early store access failed: {exception}");
            }

            return null;
        }
    }
}
