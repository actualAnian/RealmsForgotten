using System;
using System.Collections.Generic;
using System.Linq;
using RF_warsystem.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Logic;

internal enum RFWarObjectiveType
{
    SurvivalDefense,
    Reconquest,
    HolyWar,
    PunitiveRaid,
    Expansion,
    Containment
}

internal static class RFWarStrategicIntent
{
    private const float FrontClusterDistanceSquared = 32400f;

    public static float GetWarIntentFactor(Kingdom attacker, Kingdom defender)
    {
        RFWarObjectiveType objective = GetWarObjective(attacker, defender);
        return objective switch
        {
            RFWarObjectiveType.SurvivalDefense => -0.45f,
            RFWarObjectiveType.Reconquest => 0.55f,
            RFWarObjectiveType.HolyWar => 0.45f,
            RFWarObjectiveType.PunitiveRaid => 0.2f,
            RFWarObjectiveType.Expansion => 0.35f,
            _ => 0.1f
        };
    }

    public static float GetOpportunityWindow(Kingdom attacker, Kingdom defender)
    {
        float enemyPressure = GetThreatenedFiefRatio(defender);
        float enemyMultiFront = Math.Min(1f, Math.Max(0, defender.FactionsAtWarWith.OfType<Kingdom>().Count() - 1) / 3f);
        float enemyTreasuryDistress = GetTreasuryDistress(defender);
        float momentum = Math.Max(0f, RFWarStrategicMemoryBehavior.GetMomentum(attacker, defender) / 2.2f);
        float rivalry = Math.Min(1f, RFWarStrategicMemoryBehavior.GetRivalry(attacker, defender) / 2.5f);
        return ClampSigned(enemyPressure * 0.3f + enemyMultiFront * 0.22f + enemyTreasuryDistress * 0.18f + momentum * 0.15f + rivalry * 0.15f);
    }

    public static float GetPeaceExitFactor(Kingdom attacker, Kingdom defender)
    {
        RFWarObjectiveType objective = GetWarObjective(attacker, defender);
        float ownPressure = GetThreatenedFiefRatio(attacker);
        float ownTreasuryDistress = GetTreasuryDistress(attacker);
        float negativeMomentum = Math.Max(0f, -RFWarStrategicMemoryBehavior.GetMomentum(attacker, defender) / 2.2f);

        float value = objective switch
        {
            RFWarObjectiveType.SurvivalDefense => 0.55f * ownPressure + 0.2f * ownTreasuryDistress + 0.25f * negativeMomentum,
            RFWarObjectiveType.Reconquest => 0.2f * ownPressure + 0.25f * ownTreasuryDistress + 0.35f * negativeMomentum,
            RFWarObjectiveType.HolyWar => 0.15f * ownPressure + 0.2f * ownTreasuryDistress + 0.3f * negativeMomentum,
            RFWarObjectiveType.PunitiveRaid => 0.15f * ownPressure + 0.15f * ownTreasuryDistress + 0.25f * negativeMomentum,
            RFWarObjectiveType.Expansion => 0.2f * ownPressure + 0.25f * ownTreasuryDistress + 0.35f * negativeMomentum,
            _ => 0.25f * ownPressure + 0.2f * ownTreasuryDistress + 0.25f * negativeMomentum
        };

        return Clamp01(value);
    }

    public static float GetTargetIntentFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            Settlement? anchor = GetPrimaryDefenseFront(kingdom);
            return GetAnchorAlignmentFactor(anchor, targetSettlement);
        }

        if (targetSettlement.MapFaction is not Kingdom defender)
        {
            return 0f;
        }

        RFWarObjectiveType objective = GetWarObjective(kingdom, defender);
        float objectiveFactor = GetObjectiveTargetFactor(objective, kingdom, defender, targetSettlement, missionType);
        Settlement? anchorSettlement = GetPrimaryOffensiveFront(kingdom, defender, objective);
        float frontAlignment = GetAnchorAlignmentFactor(anchorSettlement, targetSettlement);
        float sacredTargetFactor = RFWarExternalFrontContext.GetSacredTargetFactor(kingdom, defender, targetSettlement);
        return ClampSigned(objectiveFactor * 0.48f + frontAlignment * 0.32f + sacredTargetFactor * 0.2f);
    }

    private static RFWarObjectiveType GetWarObjective(Kingdom attacker, Kingdom defender)
    {
        float strengthRatio = attacker.CurrentTotalStrength / Math.Max(1f, defender.CurrentTotalStrength);
        float claimPressure = GetClaimPressure(attacker, defender);
        float homeThreat = GetThreatenedFiefRatio(attacker);
        float alignmentHostility = IsAlignmentHostile(attacker, defender) ? 1f : 0f;
        float rivalry = Math.Min(1f, RFWarStrategicMemoryBehavior.GetRivalry(attacker, defender) / 2.4f);
        float holyWarPressure = RFWarExternalFrontContext.GetHolyWarPressure(attacker, defender);
        float collectiveDefensePressure = RFWarExternalFrontContext.GetCollectiveDefensePressure(attacker, defender);
        float alignmentWarPressure = RFWarExternalFrontContext.GetAlignmentWarPressure(attacker, defender);
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float sacredZeal = Math.Max(0f, profile.SacredZeal);
        float opportunism = Math.Max(0f, profile.Opportunism);
        float siegePatience = Math.Max(0f, profile.SiegePatience);
        float deepStrikeBias = Math.Max(0f, profile.DeepStrikeBias);

        if ((homeThreat >= 0.45f && strengthRatio < 1f) || collectiveDefensePressure >= 0.78f)
        {
            return RFWarObjectiveType.SurvivalDefense;
        }

        if (claimPressure >= 0.66f)
        {
            return RFWarObjectiveType.Reconquest;
        }

        if ((holyWarPressure >= 0.45f - (sacredZeal * 0.18f) && strengthRatio >= 0.82f - (sacredZeal * 0.08f))
            || ((alignmentHostility > 0f || alignmentWarPressure >= 0.5f - (sacredZeal * 0.15f)) && strengthRatio >= 0.9f - (sacredZeal * 0.08f)))
        {
            return RFWarObjectiveType.HolyWar;
        }

        if (profile.RaidPreference + (deepStrikeBias * 0.45f) >= 0.35f
            && profile.RaidPreference + (deepStrikeBias * 0.25f) > profile.SiegePreference + (siegePatience * 0.2f))
        {
            return RFWarObjectiveType.PunitiveRaid;
        }

        if (rivalry >= 0.65f && strengthRatio >= 0.9f)
        {
            return RFWarObjectiveType.Containment;
        }

        if (strengthRatio >= 1.05f - (opportunism * 0.08f)
            || (strengthRatio >= 0.96f && opportunism >= 0.35f && GetTreasuryDistress(defender) >= 0.2f))
        {
            return RFWarObjectiveType.Expansion;
        }

        return RFWarObjectiveType.Containment;
    }

    private static float GetObjectiveTargetFactor(RFWarObjectiveType objective, Kingdom attacker, Kingdom defender, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        return objective switch
        {
            RFWarObjectiveType.Reconquest => GetReconquestFactor(attacker, targetSettlement),
            RFWarObjectiveType.HolyWar => GetHolyWarFactor(attacker, targetSettlement),
            RFWarObjectiveType.PunitiveRaid => missionType == Army.ArmyTypes.Raider ? GetRaidFactor(defender, targetSettlement) : -0.15f,
            RFWarObjectiveType.Expansion => GetExpansionFactor(defender, targetSettlement),
            RFWarObjectiveType.SurvivalDefense => missionType == Army.ArmyTypes.Defender ? 0.55f : -0.35f,
            _ => GetContainmentFactor(defender, targetSettlement)
        };
    }

    private static float GetReconquestFactor(Kingdom attacker, Settlement targetSettlement)
    {
        float value = IsSameStrategicCulture(attacker.Culture?.StringId, targetSettlement.Culture?.StringId) ? 0.8f : 0f;
        value += targetSettlement.IsTown ? 0.2f : targetSettlement.IsCastle ? 0.1f : -0.05f;
        value += Math.Min(1f, RFWarStrategicMemoryBehavior.GetSettlementHeat(attacker, targetSettlement) / 2.2f) * 0.3f;
        return ClampSigned(value);
    }

    private static float GetHolyWarFactor(Kingdom attacker, Settlement targetSettlement)
    {
        float value = IsAlignmentHostile(attacker.Culture?.StringId, targetSettlement.Culture?.StringId) ? 0.65f : 0f;
        value += targetSettlement.IsTown ? 0.2f : 0f;
        if (targetSettlement.MapFaction is Kingdom defender)
        {
            value += RFWarExternalFrontContext.GetHolyWarPressure(attacker, defender) * 0.2f;
            value += RFWarExternalFrontContext.GetSacredTargetFactor(attacker, defender, targetSettlement) * 0.5f;
        }

        return ClampSigned(value);
    }

    private static float GetRaidFactor(Kingdom defender, Settlement targetSettlement)
    {
        float value = targetSettlement.IsVillage ? 0.75f : targetSettlement.IsCastle ? -0.15f : -0.05f;
        value += RFWarFrontEvaluator.GetTargetFrontPriority(defender, targetSettlement, Army.ArmyTypes.Raider) * 0.25f;
        return ClampSigned(value);
    }

    private static float GetExpansionFactor(Kingdom defender, Settlement targetSettlement)
    {
        float softness = GetSettlementSoftness(targetSettlement);
        float value = targetSettlement.IsTown ? 0.45f : targetSettlement.IsCastle ? 0.2f : -0.1f;
        value += softness * 0.35f;
        value += targetSettlement.IsVillage ? 0f : 0.1f;
        return ClampSigned(value);
    }

    private static float GetContainmentFactor(Kingdom defender, Settlement targetSettlement)
    {
        float value = targetSettlement.IsTown ? 0.15f : targetSettlement.IsCastle ? 0.1f : 0.05f;
        value += GetSettlementSoftness(targetSettlement) * 0.25f;
        return ClampSigned(value);
    }

    private static Settlement? GetPrimaryDefenseFront(Kingdom kingdom)
    {
        return kingdom.Fiefs
            .Select(town => town?.Settlement)
            .Where(settlement => settlement != null)
            .OrderByDescending(settlement => GetDefenseAnchorScore(kingdom, settlement!))
            .FirstOrDefault();
    }

    private static Settlement? GetPrimaryOffensiveFront(Kingdom attacker, Kingdom defender, RFWarObjectiveType objective)
    {
        return defender.Fiefs
            .Select(town => town?.Settlement)
            .Where(settlement => settlement != null)
            .OrderByDescending(settlement => GetOffenseAnchorScore(attacker, objective, settlement!))
            .FirstOrDefault();
    }

    private static float GetDefenseAnchorScore(Kingdom kingdom, Settlement settlement)
    {
        float value = settlement.IsUnderSiege ? 1.5f : 0f;
        if (settlement.LastAttackerParty != null && kingdom.IsAtWarWith(settlement.LastAttackerParty.MapFaction))
        {
            value += 0.9f;
        }

        value += IsFrontierSettlement(kingdom, settlement) ? 0.5f : 0f;
        value += settlement.IsTown ? 0.25f : 0.1f;
        value += GetSettlementSoftness(settlement) * 0.4f;
        return value;
    }

    private static float GetOffenseAnchorScore(Kingdom attacker, RFWarObjectiveType objective, Settlement settlement)
    {
        float value = objective switch
        {
            RFWarObjectiveType.Reconquest => GetReconquestFactor(attacker, settlement),
            RFWarObjectiveType.HolyWar => GetHolyWarFactor(attacker, settlement),
            RFWarObjectiveType.PunitiveRaid => settlement.IsVillage ? 0.9f : 0.15f,
            RFWarObjectiveType.Expansion => settlement.IsTown ? 0.7f : 0.35f,
            _ => settlement.IsTown ? 0.3f : 0.15f
        };

        value += GetSettlementSoftness(settlement) * 0.35f;
        return value;
    }

    private static float GetAnchorAlignmentFactor(Settlement? anchorSettlement, Settlement targetSettlement)
    {
        if (anchorSettlement == null)
        {
            return 0f;
        }

        if (anchorSettlement == targetSettlement)
        {
            return 1f;
        }

        float distanceSquared = anchorSettlement.GatePosition.DistanceSquared(targetSettlement.GatePosition);
        if (distanceSquared <= FrontClusterDistanceSquared)
        {
            return 0.55f;
        }

        if (distanceSquared >= FrontClusterDistanceSquared * 4f)
        {
            return -0.25f;
        }

        return ClampSigned(0.55f - (distanceSquared - FrontClusterDistanceSquared) / (FrontClusterDistanceSquared * 3f));
    }

    private static float GetClaimPressure(Kingdom attacker, Kingdom defender)
    {
        if (!defender.Fiefs.Any())
        {
            return 0f;
        }

        int matches = defender.Fiefs.Count(town => IsSameStrategicCulture(attacker.Culture?.StringId, town.Settlement.Culture?.StringId));
        return Math.Min(1f, matches / 3f);
    }

    private static float GetThreatenedFiefRatio(Kingdom kingdom)
    {
        if (!kingdom.Fiefs.Any())
        {
            return 0f;
        }

        int threatened = kingdom.Fiefs.Count(town =>
            town.Settlement.IsUnderSiege ||
            (town.Settlement.LastAttackerParty != null && town.Settlement.LastAttackerParty.IsActive && kingdom.IsAtWarWith(town.Settlement.LastAttackerParty.MapFaction)));

        return threatened / (float)kingdom.Fiefs.Count;
    }

    private static float GetTreasuryDistress(Kingdom kingdom)
    {
        float gold = kingdom.RulingClan?.Gold ?? 0f;
        if (gold >= 35000f)
        {
            return 0f;
        }

        return Math.Min(1f, (35000f - gold) / 35000f);
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

    private static bool IsFrontierSettlement(Kingdom kingdom, Settlement settlement)
    {
        foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
        {
            foreach (Town enemyTown in enemy.Fiefs)
            {
                if (enemyTown?.Settlement == null)
                {
                    continue;
                }

                if (settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition) <= 19600f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSameStrategicCulture(string? sourceCultureId, string? targetCultureId)
    {
        if (string.IsNullOrWhiteSpace(sourceCultureId) || string.IsNullOrWhiteSpace(targetCultureId))
        {
            return false;
        }

        string source = sourceCultureId!;
        string target = targetCultureId!;
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (source.StartsWith("empire", StringComparison.OrdinalIgnoreCase) || source == "south_realm" || source == "west_realm")
        {
            return target.StartsWith("empire", StringComparison.OrdinalIgnoreCase) || target == "empire" || target == "south_realm" || target == "west_realm";
        }

        return false;
    }

    private static bool IsAlignmentHostile(Kingdom attacker, Kingdom defender)
    {
        return IsAlignmentHostile(attacker.Culture?.StringId, defender.Culture?.StringId);
    }

    private static bool IsAlignmentHostile(string? attackerCultureId, string? defenderCultureId)
    {
        return IsGoodCulture(attackerCultureId) && IsEvilCulture(defenderCultureId)
            || IsEvilCulture(attackerCultureId) && IsGoodCulture(defenderCultureId);
    }

    private static bool IsGoodCulture(string? cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return false;
        }

        string id = cultureId!;
        return id == "battania"
            || id == "giant"
            || id == "dwarf"
            || id == "grimwatch"
            || id == "empire"
            || id == "south_realm"
            || id == "west_realm"
            || id == "vlandia";
    }

    private static bool IsEvilCulture(string? cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return false;
        }

        string id = cultureId!;
        return id == "sturgia"
            || id == "urkhai"
            || id == "aserai"
            || id == "mage"
            || id == "wulf"
            || id == "khuzait";
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
