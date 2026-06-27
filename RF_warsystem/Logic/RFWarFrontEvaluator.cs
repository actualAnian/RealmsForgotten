using System;
using System.Linq;
using RF_warsystem.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Logic;

internal static class RFWarFrontEvaluator
{
    private const float BorderDistanceSquared = 19600f;

    public static float GetWarFrontOpportunity(Kingdom attacker, Kingdom defender)
    {
        float ownVulnerability = GetBorderVulnerability(attacker, defender);
        float enemyVulnerability = GetBorderVulnerability(defender, attacker);
        float externalEnemyPriority = RFWarExternalFrontContext.GetEnemyPriority(attacker, defender);
        return ClampSigned((enemyVulnerability - ownVulnerability) / 1.75f + externalEnemyPriority * 0.65f);
    }

    public static float GetPeacePressure(Kingdom attacker, Kingdom defender)
    {
        float ownVulnerability = GetBorderVulnerability(attacker, defender);
        float enemyVulnerability = GetBorderVulnerability(defender, attacker);
        float externalEnemyPriority = RFWarExternalFrontContext.GetEnemyPriority(attacker, defender);
        return Clamp01((ownVulnerability - enemyVulnerability * 0.45f) / 1.5f - externalEnemyPriority * 0.25f);
    }

    public static float GetTargetFrontPriority(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (targetSettlement == null)
        {
            return 0f;
        }

        if (missionType == Army.ArmyTypes.Defender)
        {
            return GetDefensePriority(kingdom, targetSettlement);
        }

        if (targetSettlement.MapFaction is not Kingdom enemyKingdom)
        {
            return 0f;
        }

        float value = IsFrontierSettlement(enemyKingdom, targetSettlement, kingdom) ? 0.55f : -0.2f;
        value += 0.45f * GetSettlementSoftness(targetSettlement);
        value += targetSettlement.IsTown ? 0.15f : 0f;
        value += missionType == Army.ArmyTypes.Raider && targetSettlement.IsVillage ? 0.2f : 0f;
        value += 0.75f * RFWarExternalFrontContext.GetTargetPriority(kingdom, targetSettlement);
        value += 0.35f * RFWarExternalFrontContext.GetEnemyPriority(kingdom, enemyKingdom);
        return ClampSigned(value);
    }

    public static float GetFrontSectorPriority(Kingdom kingdom, Kingdom enemy, Settlement anchorSettlement, bool homelandDefense)
    {
        if (kingdom == null || enemy == null || anchorSettlement == null)
        {
            return 0f;
        }

        float clusterValue = GetClusterValue(kingdom, enemy, anchorSettlement, homelandDefense);
        float accessValue = GetAccessValue(kingdom, anchorSettlement);
        float pressureValue = homelandDefense
            ? GetDefensivePressureValue(enemy, anchorSettlement)
            : GetSettlementSoftness(anchorSettlement);

        float value = (clusterValue * 0.44f) + (accessValue * 0.28f) + (pressureValue * 0.28f);
        return ClampSigned(value);
    }

    public static float GetPhaseAnchorPriority(Kingdom kingdom, Kingdom enemy, Settlement settlement, bool homelandDefense)
    {
        if (kingdom == null || enemy == null || settlement == null)
        {
            return 0f;
        }

        bool isFrontier = homelandDefense
            ? IsFrontierSettlement(kingdom, settlement, enemy)
            : IsFrontierSettlement(enemy, settlement, kingdom);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);

        if (homelandDefense)
        {
            float value = settlement.IsUnderSiege ? 0.95f : 0f;
            value += settlement.LastAttackerParty?.MapFaction == enemy ? 0.65f : 0f;
            value += phase == RFWarCampaignPhase.Stabilize && settlement.IsFortification && isFrontier ? 0.42f : 0f;
            value += isFrontier ? 0.2f : -0.08f;
            value += settlement.IsTown ? 0.1f : 0f;
            return ClampSigned(value);
        }

        float factor = phase switch
        {
            RFWarCampaignPhase.BreakFront => settlement.IsFortification && isFrontier
                ? 0.95f
                : isFrontier
                    ? 0.12f
                    : -0.34f,
            RFWarCampaignPhase.StripSupport => isFrontier
                ? settlement.IsTown
                    ? 0.72f
                    : settlement.IsCastle
                        ? 0.62f
                        : 0.18f
                : -0.18f,
            RFWarCampaignPhase.PressCastle => settlement.IsCastle
                ? (isFrontier ? 0.45f : 0.88f)
                : settlement.IsTown
                    ? -0.16f
                    : -0.08f,
            RFWarCampaignPhase.PressTown => settlement.IsTown
                ? (isFrontier ? 0.42f : 0.94f)
                : settlement.IsCastle
                    ? 0.08f
                    : -0.14f,
            RFWarCampaignPhase.DeepStrike => !isFrontier
                ? settlement.IsTown
                    ? 0.96f
                    : settlement.IsCastle
                        ? 0.54f
                        : 0.16f
                : -0.26f,
            RFWarCampaignPhase.Stabilize => settlement.IsFortification && isFrontier
                ? 0.74f
                : isFrontier
                    ? 0.18f
                    : -0.16f,
            _ => 0f
        };

        return ClampSigned(factor);
    }

    private static float GetBorderVulnerability(Kingdom kingdom, Kingdom enemy)
    {
        var frontierFiefs = kingdom.Fiefs.Where(town => town?.Settlement != null && IsFrontierSettlement(kingdom, town.Settlement, enemy)).ToList();
        if (frontierFiefs.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        foreach (Town frontier in frontierFiefs)
        {
            Settlement settlement = frontier.Settlement;
            float vulnerability = 0.15f + 0.65f * GetSettlementSoftness(settlement);
            if (settlement.IsUnderSiege)
            {
                vulnerability += 1.2f;
            }
            else if (settlement.LastAttackerParty?.MapFaction == enemy)
            {
                vulnerability += 0.75f;
            }

            total += vulnerability;
        }

        return total / frontierFiefs.Count;
    }

    private static float GetDefensePriority(Kingdom kingdom, Settlement settlement)
    {
        float value = settlement.IsUnderSiege ? 1f : 0f;
        if (settlement.LastAttackerParty != null && kingdom.IsAtWarWith(settlement.LastAttackerParty.MapFaction))
        {
            value += 0.75f;
        }

        if (IsFrontierSettlement(kingdom, settlement, null))
        {
            value += 0.4f;
        }

        value += 0.35f * GetSettlementSoftness(settlement);
        value += settlement.IsTown ? 0.15f : 0f;
        return ClampSigned(value);
    }

    private static float GetClusterValue(Kingdom kingdom, Kingdom enemy, Settlement anchorSettlement, bool homelandDefense)
    {
        var towns = homelandDefense ? kingdom.Fiefs : enemy.Fiefs;
        float total = 0f;
        int count = 0;

        foreach (Town town in towns)
        {
            Settlement? settlement = town?.Settlement;
            if (settlement == null || settlement.GatePosition.DistanceSquared(anchorSettlement.GatePosition) > BorderDistanceSquared * 2f)
            {
                continue;
            }

            float value = settlement.IsTown ? 1f : settlement.IsCastle ? 0.8f : 0.35f;
            value += GetSettlementSoftness(settlement) * (homelandDefense ? 0.55f : 0.42f);

            if (homelandDefense)
            {
                value += settlement.IsUnderSiege ? 0.95f : 0f;
                value += settlement.LastAttackerParty?.MapFaction == enemy ? 0.6f : 0f;
            }
            else
            {
                value += settlement.IsVillage ? 0.12f : 0f;
                value += Math.Max(0f, RFWarExternalFrontContext.GetTargetPriority(kingdom, settlement)) * 0.4f;
            }

            total += value;
            count++;
        }

        if (count == 0)
        {
            return 0f;
        }

        return Clamp01(total / (count * 1.6f));
    }

    private static float GetAccessValue(Kingdom kingdom, Settlement anchorSettlement)
    {
        float best = float.MaxValue;
        foreach (Town town in kingdom.Fiefs)
        {
            Settlement? ownSettlement = town?.Settlement;
            if (ownSettlement == null)
            {
                continue;
            }

            best = Math.Min(best, ownSettlement.GatePosition.DistanceSquared(anchorSettlement.GatePosition));
        }

        if (best == float.MaxValue)
        {
            return 0f;
        }

        if (best <= BorderDistanceSquared)
        {
            return 1f;
        }

        if (best >= BorderDistanceSquared * 4f)
        {
            return 0f;
        }

        return Clamp01(1f - ((best - BorderDistanceSquared) / (BorderDistanceSquared * 3f)));
    }

    private static float GetDefensivePressureValue(Kingdom enemy, Settlement anchorSettlement)
    {
        float best = 0f;
        foreach (Town town in enemy.Fiefs)
        {
            Settlement? enemySettlement = town?.Settlement;
            if (enemySettlement == null)
            {
                continue;
            }

            float distanceSquared = enemySettlement.GatePosition.DistanceSquared(anchorSettlement.GatePosition);
            if (distanceSquared <= BorderDistanceSquared)
            {
                best = Math.Max(best, 1f);
            }
            else if (distanceSquared <= BorderDistanceSquared * 2f)
            {
                best = Math.Max(best, 0.55f);
            }
        }

        return best;
    }

    private static bool IsFrontierSettlement(Kingdom ownerKingdom, Settlement settlement, Kingdom? specificEnemy)
    {
        foreach (Kingdom enemyKingdom in ownerKingdom.FactionsAtWarWith.OfType<Kingdom>())
        {
            if (specificEnemy != null && enemyKingdom != specificEnemy)
            {
                continue;
            }

            foreach (Town enemyTown in enemyKingdom.Fiefs)
            {
                if (enemyTown?.Settlement == null)
                {
                    continue;
                }

                if (settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition) <= BorderDistanceSquared)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static float GetSettlementSoftness(Settlement settlement)
    {
        int garrison = settlement.Town?.GarrisonParty?.Party.NumberOfHealthyMembers ?? 0;
        int militia = (int)settlement.Militia;
        int defenders = garrison + militia;

        int threshold = settlement.IsTown ? 220 : settlement.IsCastle ? 140 : 60;
        if (defenders >= threshold)
        {
            return 0f;
        }

        return Clamp01((threshold - defenders) / (float)threshold);
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }

    private static float ClampSigned(float value)
    {
        return Math.Max(-1f, Math.Min(1f, value));
    }
}
