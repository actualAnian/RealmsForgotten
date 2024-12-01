using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Utility
{
    public class CustomCampaignBehaviorWrapper : CampaignBehaviorBase
    {
        private List<CampaignBehaviorBase> campaignBehaviors = new List<CampaignBehaviorBase>();

        public void AddBehavior(CampaignBehaviorBase behavior)
        {
            campaignBehaviors.Add(behavior);
        }

        public override void RegisterEvents()
        {
            foreach (var behavior in campaignBehaviors)
            {
                behavior.RegisterEvents();
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            foreach (var behavior in campaignBehaviors)
            {
                SyncDataUtility.SyncDataWithLogging(behavior, dataStore);
            }
        }
    }
}

