using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.Module1
{
    public class DailyMessageBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnDailyTick()
        {
            var message = "As you went through a crossroads, a lonely peregrin asked for your help.";
            InformationManager.DisplayMessage(new InformationMessage(message));
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Implement if data needs to be persistent
        }
    }
}