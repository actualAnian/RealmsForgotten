using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade
{
    public class AggressiveDwarfUrkhaiBehavior : CampaignBehaviorBase
    {
        // Dictionary to track last checked states for synchronization (if needed)
        private Dictionary<string, int> lastWarDeclarationDays = new Dictionary<string, int>();

        public override void RegisterEvents()
        {
            CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool success)
        {
            if (decision is MakePeaceKingdomDecision makePeaceDecision)
            {
                // Get the involved kingdoms
                var kingdom1 = makePeaceDecision.Kingdom;
                var kingdom2 = makePeaceDecision.FactionToMakePeaceWith as Kingdom;

                // Ensure kingdoms are valid and check for restricted cultures
                if (kingdom1 != null && kingdom2 != null &&
                    (IsRestrictedCulture(kingdom1.Culture.StringId) || IsRestrictedCulture(kingdom2.Culture.StringId)))
                {
                    // Reject the peace decision
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Peace decision between {kingdom1.Name} and {kingdom2.Name} was overridden and rejected."));

                    // Revert peace and declare war if already applied
                    if (success)
                    {
                        FactionManager.DeclareWar(kingdom1, kingdom2);
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"{kingdom1.Name} and {kingdom2.Name} are now at war again."));
                    }
                }
            }
        }

        private void OnDailyTick()
        {
            // Ensure the Dwarf and Urkhai kingdoms are always at war
            var dwarfKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "dwarf_kingdom");
            var urkhaiKingdom = Kingdom.All.FirstOrDefault(k => k.StringId == "urkhai_kingdom");

            if (dwarfKingdom != null && urkhaiKingdom != null && !dwarfKingdom.IsAtWarWith(urkhaiKingdom))
            {
                FactionManager.DeclareWar(dwarfKingdom, urkhaiKingdom);
                InformationManager.DisplayMessage(new InformationMessage(
                    "The war between the Dwarf Kingdom and Urkhai Kingdom was reinstated!"));
            }
        }

        private bool IsRestrictedCulture(string cultureId)
        {
            // Restricted cultures
            return cultureId == "dwarf" || cultureId == "urkhai";
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync the dictionary for tracking states
            dataStore.SyncData("lastWarDeclarationDays", ref lastWarDeclarationDays);
        }
    }
}