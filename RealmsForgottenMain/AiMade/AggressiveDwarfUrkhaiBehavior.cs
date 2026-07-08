using System.Linq;
using RF_warsystem;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade
{
    public class AggressiveDwarfUrkhaiBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Nothing to sync for now.
        }

        private void OnWeeklyTick()
        {
            if (!TryGetEnduringRivalryPair(out Kingdom dwarfKingdom, out Kingdom urkhaiKingdom))
            {
                return;
            }

            if (!dwarfKingdom.IsAtWarWith(urkhaiKingdom))
            {
                RFWarExternalIntentApi.ReinforceEnduringRivalryWar(dwarfKingdom, urkhaiKingdom);
            }
        }

        private static bool TryGetEnduringRivalryPair(out Kingdom dwarfKingdom, out Kingdom urkhaiKingdom)
        {
            dwarfKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "dwarf_kingdom");
            urkhaiKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "urkhai_kingdom");
            return dwarfKingdom != null && urkhaiKingdom != null && !dwarfKingdom.IsEliminated && !urkhaiKingdom.IsEliminated;
        }
    }
}
