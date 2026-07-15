using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_ResourceZones
{
    /// <summary>
    /// F3.5 of PLANO_RESOURCE_ZONES.md — the greed function fed into
    /// RF_warsystem's war-target selection (RFWarExternalIntentApi.
    /// SetResourceGreedProvider): how much (0..1) should <c>attacker</c> covet
    /// <c>defender</c>'s resource zones?
    ///
    /// greed = reachable wealth of the defender's zones × have-not multiplier.
    /// - A zone's worth: tier × vein richness × type weight (gold &gt; silver &gt; iron...).
    /// - Reachability: zones close to the attacker's realm count fully, far
    ///   ones fade — nobody starts a war over a mine across the continent.
    /// - Have-not: kingdoms poor in zones are hungrier than saturated ones.
    /// Cached per campaign day (the planner probes many kingdom pairs).
    /// </summary>
    public static class ResourceZoneGreedEvaluator
    {
        private const float FullReachDistance = 45f;
        private const float MaxReachDistance = 110f;
        private const float WealthNormalization = 10f;

        private static int _cacheDay = -1;
        private static readonly Dictionary<string, float> Cache = new(StringComparer.Ordinal);

        public static float GetGreed(Kingdom attacker, Kingdom defender)
        {
            if (attacker == null || defender == null || attacker == defender
                || Campaign.Current == null || ResourceZonesCampaignBehavior.Instance == null
                || !ResourceZonesCampaignBehavior.RuntimeEnabled)
            {
                return 0f;
            }

            int today = (int)CampaignTime.Now.ToDays;
            if (today != _cacheDay)
            {
                _cacheDay = today;
                Cache.Clear();
            }

            string key = $"{attacker.StringId}->{defender.StringId}";
            if (Cache.TryGetValue(key, out float cached))
            {
                return cached;
            }

            float greed = Compute(attacker, defender);
            Cache[key] = greed;
            return greed;
        }

        private static float Compute(Kingdom attacker, Kingdom defender)
        {
            List<(ResourceZoneRecord Record, MobileParty Party)> zones =
                ResourceZonesCampaignBehavior.Instance!.GetLiveZones();
            if (zones.Count == 0)
            {
                return 0f;
            }

            float reachableDefenderWealth = 0f;
            float attackerWealth = 0f;

            foreach ((ResourceZoneRecord record, MobileParty party) in zones)
            {
                Kingdom? ownerKingdom = record.OwnerClan?.Kingdom;
                if (ownerKingdom == null)
                {
                    continue; // bandit-held zones tempt everyone equally — no war motive
                }

                float worth = ZoneWorth(record, party);
                if (ownerKingdom == attacker)
                {
                    attackerWealth += worth;
                }
                else if (ownerKingdom == defender)
                {
                    reachableDefenderWealth += worth * ReachFactor(attacker, party);
                }
            }

            if (reachableDefenderWealth <= 0f)
            {
                return 0f;
            }

            // Zone-poor kingdoms are hungrier; saturated ones less so (0.75–1.25).
            float haveNot = 1.25f - 0.5f * Math.Min(1f, attackerWealth / WealthNormalization);
            float greed = reachableDefenderWealth / WealthNormalization * haveNot;
            return Math.Max(0f, Math.Min(1f, greed));
        }

        private static float ZoneWorth(ResourceZoneRecord record, MobileParty party)
        {
            float typeWeight = party.PartyComponent is ResourceZonePartyComponent component
                ? component.ZoneType switch
                {
                    ResourceZoneType.Karthradium => 1.8f, // the dwarves' treasure — coveted above all
                    ResourceZoneType.Gold => 1.5f,
                    ResourceZoneType.Silver => 1.3f,
                    ResourceZoneType.Iron => 1.0f,
                    _ => 0.8f,
                }
                : 1f;

            int richness = record.Richness > 0 ? record.Richness : 2;
            return record.Tier * ResourceZoneRules.RichnessYieldMultiplier(richness) * typeWeight;
        }

        /// <summary>1 for zones at the attacker's doorstep, fading to 0 beyond
        /// campaigning range — measured to the attacker's nearest fief.</summary>
        private static float ReachFactor(Kingdom attacker, MobileParty zoneParty)
        {
            float closest = float.MaxValue;
            foreach (Settlement settlement in attacker.Settlements)
            {
                if (!settlement.IsTown && !settlement.IsCastle)
                {
                    continue;
                }

                float distance = (settlement.GatePosition - zoneParty.Position).Length;
                if (distance < closest)
                {
                    closest = distance;
                }
            }

            if (closest == float.MaxValue)
            {
                return 0f; // fiefless kingdom: no border to project greed from
            }

            if (closest <= FullReachDistance)
            {
                return 1f;
            }

            if (closest >= MaxReachDistance)
            {
                return 0f;
            }

            return 1f - (closest - FullReachDistance) / (MaxReachDistance - FullReachDistance);
        }
    }
}
