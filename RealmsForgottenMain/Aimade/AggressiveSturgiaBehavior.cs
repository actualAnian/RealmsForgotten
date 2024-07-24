using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Library;


namespace RealmsForgotten.Behaviors
{
    public class AggressiveSturgiaBehavior : CampaignBehaviorBase
    {
        // Field to track party aggressiveness
        private Dictionary<string, bool> partyAggressiveness = new Dictionary<string, bool>();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
            CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
        }

        private void OnDailyTickParty(MobileParty party)
        {
            // Check if the party belongs to the Sturgian culture
            if (party.LeaderHero != null && party.LeaderHero.Culture != null && party.LeaderHero.Culture.StringId == "sturgia")
            {
                // Increase aggressiveness by making the party seek out enemies more frequently
                MakePartyAggressive(party);
            }
        }

        private void MakePartyAggressive(MobileParty party)
        {
            // Logic to make the party more aggressive
            var enemyParties = MobileParty.All.Where(p => p.IsActive && p.MapFaction.IsAtWarWith(party.MapFaction) && party.Position2D.DistanceSquared(p.Position2D) < 10000).ToList();

            if (enemyParties.Any())
            {
                party.Ai.SetMoveEngageParty(enemyParties.GetRandomElement());

                // Track the aggressiveness state
                if (!partyAggressiveness.ContainsKey(party.StringId))
                {
                    partyAggressiveness.Add(party.StringId, true);
                }
            }
        }

        private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool success)
        {
            if (decision is MakePeaceKingdomDecision makePeaceDecision)
            {
                var sturgiaKingdom = Kingdom.All.FirstOrDefault(k => k.Culture.StringId == "sturgia");
                var battaniaKingdom = Kingdom.All.FirstOrDefault(k => k.Culture.StringId == "battania");

                if (sturgiaKingdom != null && battaniaKingdom != null)
                {
                    if ((makePeaceDecision.Kingdom == sturgiaKingdom && makePeaceDecision.FactionToMakePeaceWith == battaniaKingdom) ||
                        (makePeaceDecision.Kingdom == battaniaKingdom && makePeaceDecision.FactionToMakePeaceWith == sturgiaKingdom))
                    {
                        // Log or handle the rejection of the peace decision
                        InformationManager.DisplayMessage(new InformationMessage("Peace decision between Sturgia and Battania was rejected."));
                    }
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync the partyAggressiveness dictionary
            dataStore.SyncData("partyAggressiveness", ref partyAggressiveness);
        }
    }
}