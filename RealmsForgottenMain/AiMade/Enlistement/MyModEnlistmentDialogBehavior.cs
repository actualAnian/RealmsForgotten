using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.Behaviors
{
    public class MyModEnlistmentDialogBehavior : CampaignBehaviorBase
    {
        private MyModEnlistmentDialog _dialog = new MyModEnlistmentDialog();

        public override void RegisterEvents()
        {
            // Register dialogs when the campaign session is launched
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            // Register enlistment dialogs when the campaign starts
            _dialog.AddDialogs(campaignGameStarter);
        }
    }
}
