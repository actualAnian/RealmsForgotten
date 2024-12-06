using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.PartyOverrides
{
    internal class YourFactionAI : CustomAIBase
    {
        private readonly CultureObject _culture = Campaign.Current.ObjectManager.GetObject<CultureObject>("dwarf");

        internal override void Think(MobileParty party, PartyThinkParams thinkParams)
        {
            // Only execute behavior if the party is the leader of its army and belongs to a main faction (Kingdom)
            if (party.Army?.LeaderParty == null || !party.Army.LeaderParty.Equals(party) || !(party.MapFaction is Kingdom))
                return;

            base.Think(party, thinkParams); // Call base AI logic

            Army army = party.Army;
            // Ensure the army is valid and is not set to a behavior unrelated to settlements
            if ((army != null ? (army.ArmyType > 0 ? 1 : 0) : 1) != 0 || !(party.Army.AiBehaviorObject is Settlement))
                return;

            Settlement aiBehaviorObject = (Settlement)party.Army.AiBehaviorObject;

            // Check if the targeted settlement belongs to a main faction (Kingdom) and is at war
            if (aiBehaviorObject?.OwnerClan?.Kingdom == null ||
                !FactionManager.IsAtWarAgainstFaction(party.ActualClan.Kingdom, aiBehaviorObject.OwnerClan.Kingdom) ||
                (!aiBehaviorObject.IsTown && !aiBehaviorObject.IsCastle) ||
                aiBehaviorObject.Culture.Equals(_culture))
                return;

            // Determine the behavior based on current conditions
            Settlement targetSettlement = DetermineTargetSettlement(aiBehaviorObject);

            float score = thinkParams.AIBehaviorScores
                            .OrderByDescending(s => s.Item2)
                            .FirstOrDefault().Item2 + DefaultIncrement;

            AIBehaviorTuple behaviorTuple;
            if (targetSettlement == null)
            {
                // Default to besieging if no specific target settlement is found
                Settlement fallbackSettlement = GetFallbackSettlement();
                behaviorTuple = new AIBehaviorTuple(fallbackSettlement, AiBehavior.BesiegeSettlement, false);
                SetPartyAiAction.GetActionForBesiegingSettlement(party, fallbackSettlement);
            }
            else
            {
                // Look for an enemy party to engage
                MobileParty targetParty = FindEnemyPartyToEngage(party);
                if (targetParty != null)
                {
                    // Engage the enemy party
                    behaviorTuple = new AIBehaviorTuple(targetParty, AiBehavior.EngageParty, false);
                    SetPartyAiAction.GetActionForEngagingParty(party, targetParty);
                }
                else
                {
                    // Default to besieging the settlement if no enemy party is found
                    behaviorTuple = new AIBehaviorTuple(targetSettlement, AiBehavior.BesiegeSettlement, false);
                    SetPartyAiAction.GetActionForBesiegingSettlement(party, targetSettlement);
                }
            }

            // Create and add a new AI behavior override
            CustomAIBase.AIBehaviorOverride aiOverride = new CustomAIBase.AIBehaviorOverride(behaviorTuple, score, CampaignTime.Now + CampaignTime.Days(5f));
            AddOverride(party, aiOverride, thinkParams);
        }

        private Settlement DetermineTargetSettlement(Settlement currentSettlement)
        {
            // Logic to find a valid target settlement based on village states
            return currentSettlement.BoundVillages
                .Where(v => v.VillageState != Village.VillageStates.Looted) // Use valid VillageStates value
                .FirstOrDefault()?.Settlement;
        }

        private MobileParty FindEnemyPartyToEngage(MobileParty party)
        {
            // Logic to find a nearby enemy party that belongs to a main faction (Kingdom)
            return MobileParty.All
                .Where(p => p.MapFaction is Kingdom && p.MapFaction != party.MapFaction && p.IsActive && p.Position2D.DistanceSquared(party.Position2D) < 10000)
                .OrderBy(p => p.Position2D.DistanceSquared(party.Position2D))
                .FirstOrDefault();
        }

        private Settlement GetFallbackSettlement()
        {
            // Logic to find a fallback settlement controlled by your faction
            return Campaign.Current.Settlements.FirstOrDefault(s => s.Culture.StringId == "your_faction");
        }
    }
}