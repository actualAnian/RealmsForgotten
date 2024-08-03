using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Behaviors
{
    public class AggressiveSturgiaBehavior : CampaignBehaviorBase
    {
        // Field to track party aggressiveness
        private Dictionary<string, bool> partyAggressiveness = new Dictionary<string, bool>();

        // Field to track the last war declaration day for each faction
        private Dictionary<string, int> lastWarDeclarationDays = new Dictionary<string, int>();

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
            var enemyParties = MobileParty.All
                .Where(p => p.IsActive && p.MapFaction.IsAtWarWith(party.MapFaction) && party.Position2D.DistanceSquared(p.Position2D) < 10000)
                .ToList();

            if (enemyParties.Any())
            {
                party.Ai.SetMoveEngageParty(enemyParties.GetRandomElement());

                // Track the aggressiveness state
                if (!partyAggressiveness.ContainsKey(party.StringId))
                {
                    partyAggressiveness.Add(party.StringId, true);
                }
            }
            else
            {
                // Check the peace duration and declare war if needed
                var sturgiaKingdom = Kingdom.All.FirstOrDefault(k => k.Culture.StringId == "sturgia");
                if (sturgiaKingdom != null)
                {
                    if (!lastWarDeclarationDays.ContainsKey(sturgiaKingdom.StringId))
                    {
                        lastWarDeclarationDays[sturgiaKingdom.StringId] = CampaignTime.Now.GetDayOfYear;
                    }

                    int daysSinceLastWar = CampaignTime.Now.GetDayOfYear - lastWarDeclarationDays[sturgiaKingdom.StringId];
                    if (daysSinceLastWar > 15)
                    {
                        DeclareWarOnSpecificFactions(sturgiaKingdom);
                        lastWarDeclarationDays[sturgiaKingdom.StringId] = CampaignTime.Now.GetDayOfYear;
                    }
                }
            }
        }

        private void DeclareWarOnSpecificFactions(Kingdom sturgiaKingdom)
        {
            var potentialEnemies = Kingdom.All
                .Where(k => k != sturgiaKingdom && !k.IsAtWarWith(sturgiaKingdom) &&
                            (k.Culture.StringId == "vlandia" || k.Culture.StringId == "battania" || k.Culture.StringId == "empire"))
                .ToList();

            if (potentialEnemies.Any())
            {
                var chosenEnemy = potentialEnemies.GetRandomElement();
                FactionManager.DeclareWar(sturgiaKingdom, chosenEnemy);
                InformationManager.DisplayMessage(new InformationMessage($"Sturgia has declared war on {chosenEnemy.Name} after a period of peace."));
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
            dataStore.SyncData("lastWarDeclarationDays", ref lastWarDeclarationDays);
        }
    }
}