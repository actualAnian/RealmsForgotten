using System;
using System.Collections.Generic;
using System.Linq;
using RF_warsystem.Behaviors;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem;

public static class RFWarExternalIntentApi
{
    public static void ReinforceEnduringRivalryWar(Kingdom attacker, Kingdom defender)
    {
        if (attacker == null || defender == null || attacker == defender || attacker.IsEliminated || defender.IsEliminated)
        {
            return;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float doctrineFactor = 1f + Math.Max(0f, profile.Persistence) * 0.2f + Math.Max(0f, profile.RevengeBias) * 0.2f;
        RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(attacker, defender, 0.96f * doctrineFactor);
        RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(defender, attacker, 0.88f);
        RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(attacker, defender, 0.9f * doctrineFactor);
        RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(defender, attacker, 0.78f);
        RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(attacker, 0.3f * doctrineFactor);
        RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(defender, 0.18f);
        RFWarExternalFrontContext.ReinforceEnemy(attacker, defender, 0.96f * doctrineFactor, 2.5f);
        RFWarExternalFrontContext.ReinforceEnemy(defender, attacker, 0.72f, 2.5f);
        RFWarSpecialAuthorityBehavior.RequestWar(attacker, defender, RFWarSpecialRequestType.EnduringRivalry, 0.92f * doctrineFactor);
    }

    public static void ReinforceReligiousWar(Kingdom kingdom1, Kingdom kingdom2)
    {
        if (kingdom1 == null || kingdom2 == null || kingdom1 == kingdom2 || kingdom1.IsEliminated || kingdom2.IsEliminated)
        {
            return;
        }

        RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(kingdom1, kingdom2, 0.85f);
        RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(kingdom2, kingdom1, 0.85f);
        RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(kingdom1, kingdom2, 0.72f);
        RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(kingdom2, kingdom1, 0.72f);
        RFWarExternalFrontContext.ReinforceEnemy(kingdom1, kingdom2, 0.7f, 2f);
        RFWarExternalFrontContext.ReinforceEnemy(kingdom2, kingdom1, 0.7f, 2f);
        RFWarSpecialAuthorityBehavior.RequestWar(kingdom1, kingdom2, RFWarSpecialRequestType.HolyWar, 0.82f);
        RFWarSpecialAuthorityBehavior.RequestWar(kingdom2, kingdom1, RFWarSpecialRequestType.HolyWar, 0.82f);
    }

    public static void RequestCoalitionPeace(Kingdom left, Kingdom right)
    {
        if (left == null || right == null || left == right || left.IsEliminated || right.IsEliminated)
        {
            return;
        }

        RFWarSpecialAuthorityBehavior.RequestPeace(left, right);
    }

    public static void ReinforceMercenaryContractWar(
        Kingdom contractor,
        IEnumerable<Kingdom>? kingdomEnemies)
    {
        if (contractor == null || contractor.IsEliminated)
        {
            return;
        }

        List<Kingdom> enemies = kingdomEnemies?
            .Where(enemy => enemy != null && !enemy.IsEliminated && enemy != contractor)
            .Distinct()
            .ToList() ?? new List<Kingdom>();

        foreach (Kingdom enemy in enemies)
        {
            RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(contractor);
            float coalitionFactor = 1f + Math.Max(0f, profile.CoalitionLoyalty) * 0.25f;
            RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(contractor, enemy, 0.78f * coalitionFactor);
            RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(contractor, enemy, 0.82f * coalitionFactor);
            RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(contractor, 0.22f * coalitionFactor);
            RFWarExternalFrontContext.ReinforceEnemy(contractor, enemy, 0.8f * coalitionFactor, 2f);
            RFWarSpecialAuthorityBehavior.RequestWar(contractor, enemy, RFWarSpecialRequestType.MercenaryContract, 0.78f * coalitionFactor);

            foreach (Town town in enemy.Fiefs)
            {
                if (town?.Settlement == null)
                {
                    continue;
                }

                float settlementPriority = town.Settlement.IsTown ? 0.8f : 0.64f;
                RFWarExternalFrontContext.ReinforceTarget(contractor, town.Settlement, settlementPriority * coalitionFactor, 2f);
            }
        }
    }

    public static void ReinforceStrategicIntrigueWar(
        Kingdom allyKingdom,
        Kingdom targetKingdom,
        Settlement? promisedSettlement)
    {
        if (allyKingdom == null || targetKingdom == null || allyKingdom == targetKingdom || allyKingdom.IsEliminated || targetKingdom.IsEliminated)
        {
            return;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(allyKingdom);
        float coalitionFactor = 1f + Math.Max(0f, profile.CoalitionLoyalty) * 0.2f;
        RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(allyKingdom, targetKingdom, 0.84f * coalitionFactor);
        RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(allyKingdom, targetKingdom, 0.86f * coalitionFactor);
        RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(allyKingdom, 0.2f * coalitionFactor);
        RFWarExternalFrontContext.ReinforceEnemy(allyKingdom, targetKingdom, 0.84f * coalitionFactor, 2f);
        RFWarSpecialAuthorityBehavior.RequestWar(allyKingdom, targetKingdom, RFWarSpecialRequestType.StrategicIntrigue, 0.84f * coalitionFactor);

        if (promisedSettlement != null)
        {
            RFWarStrategicMemoryBehavior.RaiseExternalSettlementHeatFloor(allyKingdom, promisedSettlement, 2.6f * coalitionFactor);
            RFWarExternalFrontContext.ReinforceTarget(allyKingdom, promisedSettlement, 0.95f * coalitionFactor, 2f);
        }
    }

    public static void ReinforceHolyWar(
        Kingdom leader,
        Kingdom target,
        Settlement sacredTarget,
        IEnumerable<Kingdom>? participants)
    {
        if (leader == null || target == null || sacredTarget == null)
        {
            return;
        }

        List<Kingdom> activeParticipants = participants?
            .Where(kingdom => kingdom != null && !kingdom.IsEliminated)
            .Distinct()
            .ToList() ?? new List<Kingdom>();

        if (!activeParticipants.Contains(leader))
        {
            activeParticipants.Add(leader);
        }

        foreach (Kingdom participant in activeParticipants)
        {
            RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(participant);
            float coalitionFactor = 1f + Math.Max(0f, profile.CoalitionLoyalty) * 0.3f;
            RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(participant, target, (participant == leader ? 1.2f : 0.85f) * coalitionFactor);
            RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(participant, target, (participant == leader ? 1.05f : 0.82f) * coalitionFactor);
            RFWarStrategicMemoryBehavior.RaiseExternalSettlementHeatFloor(participant, sacredTarget, (participant == leader ? 3.2f : 2.5f) * coalitionFactor);
            RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(participant, (participant == leader ? 0.45f : 0.28f) * coalitionFactor);
            RFWarExternalFrontContext.ReinforceEnemy(participant, target, (participant == leader ? 1f : 0.82f) * coalitionFactor, 2f);
            RFWarExternalFrontContext.ReinforceTarget(participant, sacredTarget, (participant == leader ? 1f : 0.9f) * coalitionFactor, 2f);
            RFWarExternalFrontContext.ReinforceHolyWar(participant, target, sacredTarget, (participant == leader ? 1f : 0.82f) * coalitionFactor, 2f);
            RFWarSpecialAuthorityBehavior.RequestWar(participant, target, RFWarSpecialRequestType.HolyWar, (participant == leader ? 1f : 0.86f) * coalitionFactor);
        }
    }

    public static void ReinforceCollectiveDefense(
        Kingdom attacker,
        Kingdom attackedKingdom,
        IEnumerable<Kingdom>? defenders)
    {
        if (attacker == null || attackedKingdom == null)
        {
            return;
        }

        List<Kingdom> activeDefenders = defenders?
            .Where(kingdom => kingdom != null && !kingdom.IsEliminated)
            .Distinct()
            .ToList() ?? new List<Kingdom>();

        foreach (Kingdom defender in activeDefenders)
        {
            RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(defender);
            float coalitionFactor = 1f + Math.Max(0f, profile.CoalitionLoyalty) * 0.3f;
            RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(defender, attacker, (defender == attackedKingdom ? 0.95f : 0.7f) * coalitionFactor);
            RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(defender, attacker, (defender == attackedKingdom ? 0.95f : 0.76f) * coalitionFactor);
            RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(defender, (defender == attackedKingdom ? 0.9f : 0.4f) * coalitionFactor);
            RFWarExternalFrontContext.ReinforceEnemy(defender, attacker, (defender == attackedKingdom ? 0.88f : 0.72f) * coalitionFactor, 2f);
            RFWarExternalFrontContext.ReinforceCollectiveDefense(defender, attacker, (defender == attackedKingdom ? 1f : 0.78f) * coalitionFactor, 2f);
            RFWarSpecialAuthorityBehavior.RequestWar(defender, attacker, RFWarSpecialRequestType.CollectiveDefense, (defender == attackedKingdom ? 1f : 0.84f) * coalitionFactor);

            foreach (Town town in attackedKingdom.Fiefs)
            {
                if (town?.Settlement == null)
                {
                    continue;
                }

                RFWarExternalFrontContext.ReinforceTarget(defender, town.Settlement, (town.Settlement.IsTown ? 0.92f : 0.74f) * coalitionFactor, 2f);
            }
        }
    }

    public static void ReinforceAlignmentWar(
        IEnumerable<Kingdom>? goodKingdoms,
        IEnumerable<Kingdom>? evilKingdoms)
    {
        List<Kingdom> good = goodKingdoms?
            .Where(kingdom => kingdom != null && !kingdom.IsEliminated)
            .Distinct()
            .ToList() ?? new List<Kingdom>();

        List<Kingdom> evil = evilKingdoms?
            .Where(kingdom => kingdom != null && !kingdom.IsEliminated)
            .Distinct()
            .ToList() ?? new List<Kingdom>();

        foreach (Kingdom goodKingdom in good)
        {
            RFWarStrategicProfile goodProfile = RFWarStrategicProfiles.Get(goodKingdom);
            float goodCoalitionFactor = 1f + Math.Max(0f, goodProfile.CoalitionLoyalty) * 0.3f;

            foreach (Kingdom evilKingdom in evil)
            {
                RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(goodKingdom, evilKingdom, 0.72f * goodCoalitionFactor);
                RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(goodKingdom, evilKingdom, 0.88f * goodCoalitionFactor);
                RFWarExternalFrontContext.ReinforceEnemy(goodKingdom, evilKingdom, 0.78f * goodCoalitionFactor, 2f);
                RFWarExternalFrontContext.ReinforceAlignmentWar(goodKingdom, evilKingdom, 0.92f * goodCoalitionFactor, 2f);
                RFWarSpecialAuthorityBehavior.RequestWar(goodKingdom, evilKingdom, RFWarSpecialRequestType.AlignmentWar, 0.86f * goodCoalitionFactor);
            }

            RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(goodKingdom, 0.38f * goodCoalitionFactor);
        }

        foreach (Kingdom evilKingdom in evil)
        {
            RFWarStrategicProfile evilProfile = RFWarStrategicProfiles.Get(evilKingdom);
            float evilCoalitionFactor = 1f + Math.Max(0f, evilProfile.CoalitionLoyalty) * 0.3f;
            RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(evilKingdom, 0.38f * evilCoalitionFactor);

            foreach (Kingdom goodKingdom in good)
            {
                RFWarExternalFrontContext.ReinforceEnemy(evilKingdom, goodKingdom, 0.78f * evilCoalitionFactor, 2f);
                RFWarExternalFrontContext.ReinforceAlignmentWar(evilKingdom, goodKingdom, 0.92f * evilCoalitionFactor, 2f);
                RFWarSpecialAuthorityBehavior.RequestWar(evilKingdom, goodKingdom, RFWarSpecialRequestType.AlignmentWar, 0.86f * evilCoalitionFactor);
            }
        }
    }

    public static void ReinforceAlignmentWarPair(Kingdom attacker, Kingdom defender)
    {
        if (attacker == null || defender == null || attacker == defender || attacker.IsEliminated || defender.IsEliminated)
        {
            return;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float coalitionFactor = 1f + Math.Max(0f, profile.CoalitionLoyalty) * 0.3f;
        RFWarStrategicMemoryBehavior.RaiseExternalPairRivalryFloor(attacker, defender, 0.72f * coalitionFactor);
        RFWarStrategicMemoryBehavior.RaiseExternalPairCommitmentFloor(attacker, defender, 0.88f * coalitionFactor);
        RFWarExternalFrontContext.ReinforceEnemy(attacker, defender, 0.78f * coalitionFactor, 2f);
        RFWarExternalFrontContext.ReinforceAlignmentWar(attacker, defender, 0.92f * coalitionFactor, 2f);
        RFWarStrategicMemoryBehavior.RaiseExternalHomePressureFloor(attacker, 0.38f * coalitionFactor);
        RFWarSpecialAuthorityBehavior.RequestWar(attacker, defender, RFWarSpecialRequestType.AlignmentWar, 0.86f * coalitionFactor);
    }
}
