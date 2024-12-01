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
            // Check if the party leader is of the "human" race
            if (party.LeaderHero != null && party.LeaderHero.CharacterObject.Race.ToString() == "human")
            {
                if (party.Army != null)
                {
                    IncreaseArmyCohesion(party.Army);
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