using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    public class AggressiveDwarfUrkhaiBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Nothing to sync for now
        }

        private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool success)
        {
            if (decision is MakePeaceKingdomDecision makePeaceDecision)
            {
                var kingdom1 = makePeaceDecision.Kingdom;
                var kingdom2 = makePeaceDecision.FactionToMakePeaceWith as Kingdom;

                if (kingdom1 != null && kingdom2 != null &&
                    (IsRestrictedCulture(kingdom1.Culture.StringId) || IsRestrictedCulture(kingdom2.Culture.StringId)))
                {
                    if (success)
                    {
                        FactionManager.DeclareWar(kingdom1, kingdom2);
                        MBInformationManager.AddQuickInformation(new TextObject($"⚔️ {kingdom1.Name} and {kingdom2.Name} are now at war again!"));
                    }
                }
            }
        }

        private void OnDailyTick()
        {
            var dwarfKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "dwarf_kingdom");
            var urkhaiKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "urkhai_kingdom");
            if (dwarfKingdom != null && urkhaiKingdom != null && !dwarfKingdom.IsEliminated && !urkhaiKingdom.IsEliminated && !dwarfKingdom.IsAtWarWith(urkhaiKingdom))
            {
                FactionManager.DeclareWar(dwarfKingdom, urkhaiKingdom);
                MBInformationManager.AddQuickInformation(new TextObject(
                    "⚔️ War between the Dwarves and Urkhai has been reinstated!"));
            }
        }

        private bool IsRestrictedCulture(string cultureId)
        {
            return cultureId == "dwarf" || cultureId == "urkhai";
        }
    }
}
