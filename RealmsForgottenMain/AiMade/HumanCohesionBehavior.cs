using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.Behaviors
{
    public class HumanCohesionBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, float> armyCohesionChanges = new Dictionary<string, float>();
        private Dictionary<string, float> partyMoraleChanges = new Dictionary<string, float>();
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
        }

        private void OnDailyTickParty(MobileParty party)
        {
            // Check if the party leader is of the "human" race. Race is an int
            // id — Race.ToString() gives "0"/"1"..., never "human"; use the
            // RaceManager-backed IsHuman() extension.
            // Faction parties only — human-led BANDIT bands don't share the
            // "cohesion of men" bonus (author decision 2026-07-15).
            if (party.LeaderHero != null && party.LeaderHero.CharacterObject.IsHuman()
                && party.ActualClan != null && !party.ActualClan.IsBanditFaction)
            {
                if (party.Army != null)
                {
                    // Once per ARMY per day — DailyTickParty fires for every
                    // member party, so crediting each one stacked +1 per human
                    // lord and made big human armies effectively immortal
                    // (cohesion is the game's army-disband limiter).
                    if (party.Army.LeaderParty == party)
                    {
                        IncreaseArmyCohesion(party.Army);
                    }
                }
                else
                {
                    IncreasePartyMorale(party);
                }
            }
        }

        private void IncreaseArmyCohesion(Army army)
        {
            // Increase the army's cohesion
            army.Cohesion += 1.0f; // Adjust the value as needed
           
        }

        private void IncreasePartyMorale(MobileParty party)
        {
            // Increase the party's morale as an alternative to cohesion
            party.RecentEventsMorale += 1.0f; // Adjust the value as needed
           
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync the armyCohesionChanges and partyMoraleChanges dictionaries
            dataStore.SyncData("armyCohesionChanges", ref armyCohesionChanges);
            dataStore.SyncData("partyMoraleChanges", ref partyMoraleChanges);
        }
    }
}