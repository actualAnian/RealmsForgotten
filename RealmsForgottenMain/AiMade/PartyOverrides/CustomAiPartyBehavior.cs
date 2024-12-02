using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.PartyOverrides
{
    internal abstract class CustomAIBase
    {
        // Cache for AI behavior overrides specific to a MobileParty
        internal Dictionary<MobileParty, CustomAIBase.AIBehaviorOverride> ThinkParamsCache = new Dictionary<MobileParty, CustomAIBase.AIBehaviorOverride>();

        // Default increment value used for scoring AI behaviors
        protected float DefaultIncrement => 0.5f;

        // Main method for thinking and deciding on AI behavior
        internal virtual void Think(MobileParty party, PartyThinkParams thinkParams)
        {
            if (!ThinkParamsCache.ContainsKey(party))
                return;

            // Use the comparison operator correctly
            if (ThinkParamsCache[party].Expiration <= CampaignTime.Now)
            {
                ThinkParamsCache.Remove(party);
            }
            else
            {
                // Correct enum comparison with valid AiBehavior values
                if ((int)ThinkParamsCache[party].Tuple.AiBehavior == (int)AiBehavior.EngageParty) // Example adjustment: use a valid AiBehavior value
                {
                    Settlement settlement = (Settlement)ThinkParamsCache[party].Tuple.Party;

                    // Correctly access the VillageState property from the settlement's Village instance
                    if (settlement.IsVillage && settlement.Village.VillageState == Village.VillageStates.Looted) // Use valid VillageStates value
                    {
                        ThinkParamsCache.Remove(party);
                        return;
                    }
                }

                PartyThinkParams partyThinkParams = thinkParams;
                (AIBehaviorTuple, float) valueTuple = (ThinkParamsCache[party].Tuple, ThinkParamsCache[party].Score);
                ref (AIBehaviorTuple, float) local = ref valueTuple;

                // Pass the parameter with the 'in' keyword
                partyThinkParams.AddBehaviorScore(in local);
            }
        }

        // Method to add or update AI behavior overrides for a party
        protected void AddOverride(MobileParty party, CustomAIBase.AIBehaviorOverride aiOverride, PartyThinkParams thinkParams)
        {
            if (ThinkParamsCache.ContainsKey(party))
                ThinkParamsCache[party] = aiOverride;
            else
                ThinkParamsCache.Add(party, aiOverride);

            PartyThinkParams partyThinkParams = thinkParams;
            (AIBehaviorTuple, float) valueTuple = (aiOverride.Tuple, aiOverride.Score);
            ref (AIBehaviorTuple, float) local = ref valueTuple;

            // Pass the parameter with the 'in' keyword
            partyThinkParams.AddBehaviorScore(in local);
        }

        // Inner class representing an override for AI behavior
        internal class AIBehaviorOverride
        {
            // Constructs the AIBehaviorTuple to be used in decision-making
            internal AIBehaviorTuple Tuple => new AIBehaviorTuple(this.Party, this.AiBehavior, false);

            [SaveableProperty(1)]
            internal IMapPoint Party { get; private set; }

            [SaveableProperty(2)]
            internal AiBehavior AiBehavior { get; private set; }

            [SaveableProperty(3)]
            internal float Score { get; private set; }

            [SaveableProperty(4)]
            internal CampaignTime Expiration { get; private set; }

            // Constructor for creating a new AI behavior override
            public AIBehaviorOverride(AIBehaviorTuple tuple, float score, CampaignTime expiration)
            {
                Party = tuple.Party;
                AiBehavior = tuple.AiBehavior;
                Score = score;
                Expiration = expiration;
            }
        }
    }
}