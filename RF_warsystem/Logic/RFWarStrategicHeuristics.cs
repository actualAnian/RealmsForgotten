using System;
using System.Collections.Generic;
using System.Linq;
using RF_warsystem.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Logic;

internal static class RFWarStrategicHeuristics
{
    private static readonly HashSet<string> GoodCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "battania",
        "giant",
        "dwarf",
        "grimwatch",
        "empire",
        "south_realm",
        "west_realm",
        "vlandia"
    };

    private static readonly HashSet<string> EvilCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "sturgia",
        "urkhai",
        "aserai",
        "mage",
        "wulf",
        "khuzait"
    };

    public static float AdjustWarScore(DiplomacyModel baseModel, float baseScore, IFaction attackerFaction, IFaction defenderFaction, Clan evaluatingClan)
    {
        if (attackerFaction is not Kingdom attacker || defenderFaction is not Kingdom defender)
        {
            return baseScore;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float threshold = Math.Max(1000f, baseModel.GetDecisionMakingThreshold(attacker));
        float? objectivePolicy = RFWarExternalFrontContext.GetWarProposalPolicy(attacker, defender);
        if (objectivePolicy < 0f)
        {
            return -1000000f;
        }

        float adjustment = 0f;
        float alignmentHostility = Math.Max(0f, GetAlignmentHostility(attacker, defender));
        float holyPressure = Math.Max(0f, RFWarExternalFrontContext.GetHolyWarPressure(attacker, defender));
        float alignmentPressure = Math.Max(0f, RFWarExternalFrontContext.GetAlignmentWarPressure(attacker, defender));
        float specialAuthorityPressure = Math.Max(0f, RFWarSpecialAuthorityBehavior.GetPendingWarPriority(attacker, defender));
        adjustment += threshold * 0.18f * GetStrengthOpportunity(attacker, defender);
        adjustment += threshold * 0.12f * GetReadinessSignal(attacker);
        adjustment += threshold * 0.12f * GetMomentumMemory(attacker, defender);
        adjustment += threshold * 0.1f * GetRivalryMemory(attacker, defender);
        adjustment += threshold * 0.14f * RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(attacker, defender);
        adjustment += threshold * 0.12f * RFWarFrontEvaluator.GetWarFrontOpportunity(attacker, defender);
        adjustment += threshold * 0.1f * GetFrontMomentumOpportunity(attacker, defender);
        adjustment += threshold * 0.14f * RFWarStrategicIntent.GetWarIntentFactor(attacker, defender);
        adjustment += threshold * 0.12f * RFWarStrategicIntent.GetOpportunityWindow(attacker, defender);
        adjustment += threshold * 0.08f * RFWarCampaignDirectorBehavior.GetCoalitionPullFactor(attacker, defender);
        adjustment += threshold * 0.08f * GetCoalitionDutyWarPressure(attacker, defender);
        adjustment += threshold * 0.2f * specialAuthorityPressure;
        adjustment += threshold * 0.18f * GetClaimPressure(attacker, defender);
        adjustment += threshold * 0.14f * GetFrontierPressure(attacker, defender);
        adjustment += threshold * 0.35f * objectivePolicy.GetValueOrDefault();
        adjustment += threshold * 0.08f * GetAlignmentHostility(attacker, defender);
        adjustment += threshold * 0.08f * profile.OffensiveDrive;
        adjustment += threshold * 0.05f * GetProfileCampaignIdentityWarPressure(attacker, defender);
        adjustment += threshold * 0.04f * profile.Persistence * Math.Max(0f, GetMomentumMemory(attacker, defender));
        adjustment += threshold * 0.03f * profile.Persistence * GetRivalryMemory(attacker, defender);
        adjustment += threshold * 0.05f * profile.RevengeBias * GetRivalryMemory(attacker, defender);
        adjustment += threshold * 0.08f * profile.RevengeBias * RFWarStrategicAssessment.GetHistoricalGrievance(attacker, defender);
        adjustment += threshold * 0.05f * profile.Opportunism * Math.Max(0f, GetStrengthOpportunity(attacker, defender));
        adjustment += threshold * 0.04f * profile.Opportunism * Math.Max(0f, RFWarStrategicIntent.GetOpportunityWindow(attacker, defender));
        adjustment += threshold * 0.05f * profile.SacredZeal * Math.Max(alignmentHostility, Math.Max(holyPressure, alignmentPressure));
        adjustment += threshold * 0.04f * profile.FrontierParanoia * Math.Max(0f, GetFrontierPressure(attacker, defender));
        adjustment += threshold * 0.06f * GetTreasuryConfidence(attacker);
        adjustment -= threshold * 0.12f * GetMultiFrontPenalty(attacker);
        adjustment -= threshold * 0.16f * GetHomeThreatPenalty(attacker);
        adjustment -= threshold * 0.18f * RFWarCampaignDirectorBehavior.GetNewWarPenalty(attacker, defender);
        adjustment -= threshold * 0.14f * GetExistingCampaignLockPenalty(attacker);
        adjustment -= threshold * 0.1f * RFWarCampaignDirectorBehavior.GetActiveDecisiveCampaignPressure(attacker);
        adjustment -= threshold * 0.08f * profile.Caution;
        adjustment -= threshold * 0.06f * profile.HomeGuardBias * GetHomeThreatPenalty(attacker);
        adjustment -= threshold * 0.05f * profile.FrontierParanoia * GetHomeThreatPenalty(attacker);
        adjustment -= threshold * 0.1f * GetHomeFrontMemoryPressure(attacker);
        adjustment -= threshold * 0.12f * GetWarFatiguePenalty(baseModel, attacker);

        if (evaluatingClan == attacker.RulingClan)
        {
            adjustment *= 1.05f;
        }

        float finalScore = baseScore + adjustment;

        // Young World calm: during a young world's first weeks the war director
        // is held back. Dampen only a POSITIVE (pro-war) score — never scale a
        // peace-leaning negative score toward war. Inert (multiplier == 1) unless
        // RealmsForgotten pushed a young world active, so the OFF state is exact.
        float calmMultiplier = RF_warsystem.RFYoungWorldWarBridge.GetWarScoreMultiplier();
        if (calmMultiplier < 1f && finalScore > 0f)
        {
            finalScore *= calmMultiplier;
        }

        return finalScore;
    }

    public static float AdjustPeaceScore(DiplomacyModel baseModel, float baseScore, IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
    {
        if (factionDeclaresPeace is not Kingdom attacker || factionDeclaredPeace is not Kingdom defender)
        {
            return baseScore;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float threshold = Math.Max(1000f, baseModel.GetDecisionMakingThreshold(attacker));
        float adjustment = 0f;
        float phasePeaceFactor = GetCampaignPhasePeaceFactor(attacker, defender);
        float alignmentHostility = Math.Max(0f, GetAlignmentHostility(attacker, defender));
        float holyPressure = Math.Max(0f, RFWarExternalFrontContext.GetHolyWarPressure(attacker, defender));
        float alignmentPressure = Math.Max(0f, RFWarExternalFrontContext.GetAlignmentWarPressure(attacker, defender));
        float specialAuthorityPeacePressure = Math.Max(0f, RFWarSpecialAuthorityBehavior.GetPendingPeacePriority(attacker, defender));
        float collapsePressure = Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyCollapseFactor(attacker, defender));
        adjustment += threshold * 0.24f * GetDirectWarExhaustion(baseModel, attacker, defender);
        adjustment += threshold * 0.16f * GetPeacePressureFromMomentum(attacker, defender);
        adjustment -= threshold * 0.1f * GetWarCommitment(attacker, defender);
        adjustment -= threshold * 0.12f * Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(attacker, defender));
        adjustment += threshold * 0.12f * RFWarFrontEvaluator.GetPeacePressure(attacker, defender);
        adjustment += threshold * 0.1f * GetFrontMomentumPeacePressure(attacker, defender);
        adjustment += threshold * 0.16f * RFWarStrategicIntent.GetPeaceExitFactor(attacker, defender);
        adjustment += threshold * 0.14f * GetMultiFrontPenalty(attacker);
        adjustment += threshold * 0.14f * GetHomeThreatPenalty(attacker);
        adjustment += threshold * 0.1f * GetHomeFrontMemoryPressure(attacker);
        adjustment += threshold * 0.08f * profile.Caution;
        adjustment += threshold * 0.18f * specialAuthorityPeacePressure;
        adjustment += threshold * 0.06f * profile.HomeGuardBias * GetHomeThreatPenalty(attacker);
        adjustment += threshold * 0.05f * profile.FrontierParanoia * GetHomeThreatPenalty(attacker);
        adjustment += threshold * 0.08f * GetTreasuryDistress(attacker);
        adjustment += threshold * RFWarOperationalRhythmBehavior.GetPeaceFactor(attacker, defender);
        adjustment += threshold * 0.12f * phasePeaceFactor;
        adjustment += threshold * 0.18f * RFWarStrategicAssessment.GetPeacePressure(attacker, defender);
        adjustment -= threshold * 0.12f * Math.Max(0f, GetStrengthOpportunity(attacker, defender));
        adjustment -= threshold * 0.08f * GetClaimPressure(attacker, defender);
        adjustment -= threshold * 0.08f * GetCoalitionDutyWarPressure(attacker, defender);
        adjustment -= threshold * 0.05f * GetProfileCampaignIdentityWarPressure(attacker, defender);
        adjustment -= threshold * 0.06f * GetAlignmentHostility(attacker, defender);
        adjustment -= threshold * 0.08f * GetRivalryMemory(attacker, defender);
        adjustment -= threshold * 0.14f * RFWarCampaignDirectorBehavior.GetPeaceHoldFactor(attacker, defender);
        adjustment -= threshold * 0.12f * RFWarCampaignDirectorBehavior.GetCampaignLockFactor(attacker, defender);
        adjustment -= threshold * 0.1f * RFWarCampaignDirectorBehavior.GetDecisiveCampaignPressure(attacker, defender);
        adjustment -= threshold * 0.12f * collapsePressure;
        adjustment -= threshold * 0.12f * RFWarStrategicAssessment.GetWarWill(attacker, defender);
        adjustment -= threshold * 0.16f * RFWarStrategicAssessment.GetFinishPressure(attacker, defender);
        adjustment -= threshold * 0.04f * profile.Persistence * Math.Max(0f, GetMomentumMemory(attacker, defender));
        adjustment -= threshold * 0.05f * profile.RevengeBias * GetRivalryMemory(attacker, defender);
        adjustment -= threshold * 0.05f * profile.SacredZeal * Math.Max(alignmentHostility, Math.Max(holyPressure, alignmentPressure));
        adjustment -= threshold * 0.05f * profile.SiegePatience * Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(attacker, defender));
        adjustment -= threshold * 0.04f * profile.SiegePatience * Math.Max(0f, RFWarCampaignDirectorBehavior.GetPeaceHoldFactor(attacker, defender));
        return baseScore + adjustment;
    }

    public static float AdjustTargetScore(float baseScore, Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty)
    {
        if (baseScore <= 0f || mobileParty?.MapFaction is not Kingdom kingdom || targetSettlement == null)
        {
            return baseScore;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        float multiplier = 1f;
        multiplier *= 1f + (0.35f * GetDefenseUrgencyFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.18f * GetClaimTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.16f * GetDoctrineFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.14f * GetFrontierTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.08f * GetAlignmentTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.16f * GetSettlementMemoryFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.12f * GetFrontMemoryTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.1f * GetPairRivalryTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.08f * GetPairCommitmentTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.18f * RFWarFrontEvaluator.GetTargetFrontPriority(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.18f * RFWarStrategicIntent.GetTargetIntentFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.16f * RFWarTheaterBehavior.GetTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.2f * RFWarFrontlineBehavior.GetTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.18f * RFWarCampaignPhaseBehavior.GetTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.18f * RFWarCampaignDirectorBehavior.GetTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.22f * RFWarObjectiveChainBehavior.GetTargetFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.36f * GetTargetCampaignSequenceFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.32f * GetFocusedEnemyRailFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f + (0.12f * GetProfileMissionFactor(profile, missionType));
        multiplier *= 1f + GetOperationalStateFactor(kingdom, targetSettlement, missionType);
        multiplier *= 1f + (0.22f * GetCoalitionRoleFactor(kingdom, targetSettlement, missionType));
        multiplier *= 1f - (0.68f * GetCampaignDriftPenalty(kingdom, targetSettlement, missionType));
        multiplier *= 1f - (0.38f * GetOffensiveOverextensionPenalty(kingdom, targetSettlement, missionType));

        multiplier = Math.Max(0.25f, Math.Min(2.4f, multiplier));
        return Math.Max(0f, baseScore * multiplier);
    }

    private static float GetTargetCampaignSequenceFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        Kingdom? enemy = GetRelevantEnemyForTarget(kingdom, targetSettlement, missionType);
        if (enemy == null)
        {
            return 0f;
        }

        RFWarTheaterFocusMode theaterMode = RFWarTheaterBehavior.GetFocusMode(kingdom, enemy);
        Settlement? theaterAnchor = RFWarTheaterBehavior.GetAnchorSettlement(kingdom, enemy);
        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        float sequencePressure = GetActiveCampaignSequencePressure(kingdom, enemy);

        if (missionType == Army.ArmyTypes.Defender)
        {
            if (theaterMode != RFWarTheaterFocusMode.HomelandDefense)
            {
                return 0f;
            }

            float defensiveBonus = 0f;
            defensiveBonus += GetAnchorAlignment(
                targetSettlement,
                objective,
                1f,
                0.55f + (0.1f * sequencePressure),
                0.32f + (0.08f * sequencePressure));
            defensiveBonus += GetAnchorAlignment(
                targetSettlement,
                frontlineAnchor,
                0.72f + (0.1f * sequencePressure),
                0.38f + (0.08f * sequencePressure),
                0.18f + (0.06f * sequencePressure));
            defensiveBonus += GetAnchorAlignment(
                targetSettlement,
                theaterAnchor,
                0.5f + (0.08f * sequencePressure),
                0.24f + (0.06f * sequencePressure),
                0.1f + (0.04f * sequencePressure));
            return Math.Min(1f, defensiveBonus);
        }

        if (theaterMode == RFWarTheaterFocusMode.HomelandDefense)
        {
            return -0.82f;
        }

        if (objective != null)
        {
            return GetAnchorAlignment(
                targetSettlement,
                objective,
                1f + (0.12f * sequencePressure),
                0.52f + (0.18f * sequencePressure),
                -0.58f - (0.42f * sequencePressure));
        }

        if (frontlineAnchor != null)
        {
            return GetAnchorAlignment(
                targetSettlement,
                frontlineAnchor,
                0.82f + (0.12f * sequencePressure),
                0.34f + (0.16f * sequencePressure),
                -0.4f - (0.34f * sequencePressure));
        }

        if (theaterAnchor != null)
        {
            return GetAnchorAlignment(
                targetSettlement,
                theaterAnchor,
                0.56f + (0.08f * sequencePressure),
                0.22f + (0.12f * sequencePressure),
                -0.26f - (0.24f * sequencePressure));
        }

        return 0f;
    }

    private static float GetFocusedEnemyRailFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        Kingdom? focusedEnemy = RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom);
        Kingdom? targetEnemy = GetRelevantEnemyForTarget(kingdom, targetSettlement, missionType);
        if (focusedEnemy == null || targetEnemy == null || missionType == Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        if (!kingdom.IsAtWarWith(focusedEnemy))
        {
            return targetEnemy == focusedEnemy ? 0.08f : 0f;
        }

        float campaignLock = RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, focusedEnemy);
        float unresolvedFront = RFWarCampaignDirectorBehavior.GetUnresolvedFrontPressure(kingdom, focusedEnemy);
        float sequencePressure = GetActiveCampaignSequencePressure(kingdom, focusedEnemy);
        float activePressure = Math.Min(1f, campaignLock * 0.38f + unresolvedFront * 0.24f + sequencePressure * 0.38f);

        if (targetEnemy == focusedEnemy)
        {
            return Math.Min(1.2f, 0.34f + activePressure * 0.82f);
        }

        return -Math.Min(1.35f, 0.55f + activePressure * 0.95f);
    }

    private static float GetCampaignDriftPenalty(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        Kingdom? enemy = GetRelevantEnemyForTarget(kingdom, targetSettlement, missionType);
        if (enemy == null)
        {
            return 0f;
        }

        Kingdom? focusedEnemy = RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom);
        float focusedPressure = 0f;
        if (focusedEnemy != null && kingdom.IsAtWarWith(focusedEnemy))
        {
            float focusedLock = RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, focusedEnemy);
            float focusedSequence = GetActiveCampaignSequencePressure(kingdom, focusedEnemy);
            focusedPressure = Math.Min(1f, focusedLock * 0.42f + focusedSequence * 0.58f);
        }

        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        Settlement? theaterAnchor = RFWarTheaterBehavior.GetAnchorSettlement(kingdom, enemy);
        float sequencePressure = GetActiveCampaignSequencePressure(kingdom, enemy);
        float decisivePressure = RFWarCampaignDirectorBehavior.GetDecisiveCampaignPressure(kingdom, enemy);
        float pressure = Math.Min(1f, sequencePressure * 0.62f + decisivePressure * 0.38f);
        bool isVillage = targetSettlement.IsVillage;
        bool frontier = TouchesKingdomFrontier(kingdom, targetSettlement);
        bool exactObjective = objective != null && targetSettlement == objective;
        bool exactFrontline = frontlineAnchor != null && targetSettlement == frontlineAnchor;
        bool exactTheater = theaterAnchor != null && targetSettlement == theaterAnchor;
        bool boundObjective = objective != null && isVillage && targetSettlement.Village?.Bound == objective;
        bool boundFrontline = frontlineAnchor != null && isVillage && targetSettlement.Village?.Bound == frontlineAnchor;
        bool objectiveCluster = objective != null && IsSameCluster(objective, targetSettlement);
        bool frontlineCluster = frontlineAnchor != null && IsSameCluster(frontlineAnchor, targetSettlement);
        bool theaterCluster = theaterAnchor != null && IsSameCluster(theaterAnchor, targetSettlement);
        bool breaksObjectiveAxis = objective != null && !exactObjective && !boundObjective && !objectiveCluster;
        bool breaksFrontlineAxis = frontlineAnchor != null && !exactFrontline && !boundFrontline && !frontlineCluster;
        bool breaksTheaterAxis = theaterAnchor != null && !exactTheater && !theaterCluster;

        float penalty = 0f;
        if (focusedEnemy != null && focusedEnemy != enemy && focusedPressure > 0f)
        {
            penalty += 0.22f + (focusedPressure * 0.36f);
        }

        if (objective != null)
        {
            if (breaksObjectiveAxis)
            {
                penalty += 0.42f + (pressure * 0.34f);
            }
        }
        else if (frontlineAnchor != null)
        {
            if (breaksFrontlineAxis)
            {
                penalty += 0.3f + (pressure * 0.28f);
            }
        }
        else if (theaterAnchor != null)
        {
            if (breaksTheaterAxis)
            {
                penalty += 0.18f + (pressure * 0.22f);
            }
        }

        if (breaksObjectiveAxis && breaksFrontlineAxis)
        {
            penalty += 0.12f + (pressure * 0.16f);
        }

        if (breaksFrontlineAxis && breaksTheaterAxis)
        {
            penalty += 0.08f + (pressure * 0.1f);
        }

        if (focusedEnemy == enemy && breaksObjectiveAxis && breaksFrontlineAxis && focusedPressure > 0f)
        {
            penalty += 0.1f + (focusedPressure * 0.18f);
        }

        if (!frontier && !isVillage && pressure >= 0.35f && (breaksObjectiveAxis || breaksFrontlineAxis))
        {
            penalty += 0.08f + (pressure * 0.12f);
        }

        penalty += RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.BreakFront => targetSettlement.IsFortification && frontier ? 0f : 0.26f + (pressure * 0.16f),
            RFWarCampaignPhase.StripSupport => boundObjective || boundFrontline || (isVillage && (objectiveCluster || frontlineCluster))
                ? 0f
                : 0.28f + (pressure * 0.18f),
            RFWarCampaignPhase.PressCastle => (targetSettlement.IsCastle && (objectiveCluster || frontlineCluster || frontier)) || boundObjective
                ? 0f
                : 0.34f + (pressure * 0.18f),
            RFWarCampaignPhase.PressTown => (targetSettlement.IsTown && (objectiveCluster || frontlineCluster || frontier)) || boundObjective
                ? 0f
                : 0.36f + (pressure * 0.2f),
            RFWarCampaignPhase.DeepStrike => !frontier ? 0f : 0.22f + (pressure * 0.14f),
            RFWarCampaignPhase.Stabilize => frontier && targetSettlement.IsFortification ? 0f : 0.18f + (pressure * 0.1f),
            _ => 0f
        };

        return Math.Min(1f, Math.Max(0f, penalty));
    }

    private static Kingdom? GetRelevantEnemyForTarget(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            return targetSettlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction as Kingdom
                ?? targetSettlement.LastAttackerParty?.MapFaction as Kingdom
                ?? RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom);
        }

        return targetSettlement.MapFaction as Kingdom;
    }

    private static float GetAnchorAlignment(Settlement targetSettlement, Settlement? anchor, float exactBonus, float clusterBonus, float offPenalty)
    {
        if (anchor == null)
        {
            return 0f;
        }

        if (targetSettlement == anchor)
        {
            return exactBonus;
        }

        if (targetSettlement.IsVillage && targetSettlement.Village?.Bound == anchor)
        {
            return exactBonus * 0.92f;
        }

        if (IsSameCluster(targetSettlement, anchor))
        {
            return clusterBonus;
        }

        return offPenalty;
    }

    private static bool IsSameCluster(Settlement left, Settlement right)
    {
        return left.GatePosition.DistanceSquared(right.GatePosition) <= 32400f;
    }

    private static float GetActiveCampaignSequencePressure(Kingdom kingdom, Kingdom enemy)
    {
        if (RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom) != enemy || !kingdom.IsAtWarWith(enemy))
        {
            return 0f;
        }

        float campaignLock = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, enemy));
        float frontCommitment = Math.Max(0f, RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, enemy));
        float objectiveCommitment = Math.Max(0f, RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy));
        float consolidation = Math.Max(0f, RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, enemy));
        float phasePressure = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.BreakFront => 0.2f,
            RFWarCampaignPhase.StripSupport => 0.12f,
            RFWarCampaignPhase.PressCastle => 0.34f,
            RFWarCampaignPhase.PressTown => 0.42f,
            RFWarCampaignPhase.DeepStrike => 0.14f,
            _ => 0f
        };

        float operationalPressure = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.28f,
            RFWarOperationalState.Advance => 0.2f,
            RFWarOperationalState.Exploit => 0.16f,
            RFWarOperationalState.Defend => 0.08f,
            _ => 0f
        };

        return Math.Min(
            1f,
            (campaignLock * 0.34f)
            + (frontCommitment * 0.18f)
            + (objectiveCommitment * 0.24f)
            + (consolidation * 0.1f)
            + phasePressure
            + operationalPressure);
    }

    private static float GetStrengthOpportunity(Kingdom attacker, Kingdom defender)
    {
        float ratio = attacker.CurrentTotalStrength / Math.Max(1f, defender.CurrentTotalStrength);
        return ClampSigned((ratio - 1f) / 0.65f);
    }

    private static float GetReadinessSignal(Kingdom kingdom)
    {
        List<MobileParty> parties = GetEligibleWarParties(kingdom);
        if (parties.Count == 0)
        {
            return -0.65f;
        }

        float averageFood = parties.Average(party => Math.Min(18f, Math.Max(0f, party.GetNumDaysForFoodToLast()))) / 18f;
        float averageSize = parties.Average(party => Math.Min(1f, Math.Max(0f, party.PartySizeRatio)));
        float readiness = (averageFood * 0.55f) + (averageSize * 0.45f);
        return ClampSigned((readiness - 0.5f) * 2f);
    }

    private static float GetClaimPressure(Kingdom attacker, Kingdom defender)
    {
        string attackerCulture = attacker.Culture?.StringId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(attackerCulture))
        {
            return 0f;
        }

        int sharedCultureFiefs = defender.Fiefs.Count(town => string.Equals(town.Settlement.Culture?.StringId, attackerCulture, StringComparison.OrdinalIgnoreCase));
        if (sharedCultureFiefs <= 0)
        {
            if (attackerCulture.StartsWith("empire", StringComparison.OrdinalIgnoreCase))
            {
                sharedCultureFiefs = defender.Fiefs.Count(town => IsImperialCulture(town.Settlement.Culture?.StringId));
            }
            else if (attackerCulture == "aserai")
            {
                sharedCultureFiefs = defender.Fiefs.Count(town => string.Equals(town.Settlement.Culture?.StringId, "aserai", StringComparison.OrdinalIgnoreCase));
            }
        }

        return Math.Min(1f, sharedCultureFiefs / 3f);
    }

    private static float GetMomentumMemory(Kingdom attacker, Kingdom defender)
    {
        return ClampSigned(RFWarStrategicMemoryBehavior.GetMomentum(attacker, defender) / 2.2f);
    }

    private static float GetRivalryMemory(Kingdom attacker, Kingdom defender)
    {
        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetRivalry(attacker, defender) / 2.25f);
    }

    private static float GetWarCommitment(Kingdom attacker, Kingdom defender)
    {
        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetWarCommitment(attacker, defender) / 0.9f);
    }

    private static float GetFrontierPressure(Kingdom attacker, Kingdom defender)
    {
        if (!attacker.Fiefs.Any() || !defender.Fiefs.Any())
        {
            return -0.25f;
        }

        if (RFWarPoliticalBorderContext.IsAvailable)
        {
            float adjacency = RFWarPoliticalBorderContext.GetAdjacency(attacker, defender);
            return adjacency > 0f ? 0.35f + (adjacency * 0.65f) : -0.6f;
        }

        if (HasNeighborContact(attacker, defender))
        {
            return 1f;
        }

        float minDistanceSquared = float.MaxValue;
        foreach (Town ownTown in attacker.Fiefs)
        {
            foreach (Town enemyTown in defender.Fiefs)
            {
                float distanceSquared = ownTown.Settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition);
                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                }
            }
        }

        if (minDistanceSquared <= 0f)
        {
            return 0.75f;
        }

        float distance = (float)Math.Sqrt(minDistanceSquared);
        if (distance <= 120f)
        {
            return 0.45f;
        }

        if (distance >= 360f)
        {
            return -0.6f;
        }

        return ClampSigned(0.6f - ((distance - 120f) / 240f));
    }

    private static float GetAlignmentHostility(Kingdom attacker, Kingdom defender)
    {
        bool attackerGood = IsGoodCulture(attacker.Culture?.StringId);
        bool attackerEvil = IsEvilCulture(attacker.Culture?.StringId);
        bool defenderGood = IsGoodCulture(defender.Culture?.StringId);
        bool defenderEvil = IsEvilCulture(defender.Culture?.StringId);

        if ((attackerGood && defenderEvil) || (attackerEvil && defenderGood))
        {
            return 1f;
        }

        return 0f;
    }

    private static float GetTreasuryConfidence(Kingdom kingdom)
    {
        float gold = kingdom.RulingClan?.Gold ?? 0f;
        if (gold <= 12000f)
        {
            return -0.75f;
        }

        if (gold >= 90000f)
        {
            return 0.45f;
        }

        return ClampSigned((gold - 35000f) / 55000f);
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

    private static float GetMultiFrontPenalty(Kingdom kingdom)
    {
        int activeWars = kingdom.FactionsAtWarWith.Count(faction => faction.IsKingdomFaction);
        return Math.Min(1f, Math.Max(0, activeWars - 1) / 3f);
    }

    private static float GetHomeThreatPenalty(Kingdom kingdom)
    {
        int threatened = CountThreatenedFiefs(kingdom);
        if (threatened == 0)
        {
            return 0f;
        }

        return Math.Min(1f, threatened / 3f);
    }

    private static float GetHomeFrontMemoryPressure(Kingdom kingdom)
    {
        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.5f);
    }

    private static float GetExistingCampaignLockPenalty(Kingdom kingdom)
    {
        return RFWarCampaignDirectorBehavior.GetActiveCampaignLockFactor(kingdom);
    }

    private static float GetWarFatiguePenalty(DiplomacyModel baseModel, Kingdom kingdom)
    {
        List<Kingdom> activeEnemies = kingdom.FactionsAtWarWith.OfType<Kingdom>().ToList();
        if (activeEnemies.Count == 0)
        {
            return 0f;
        }

        float averageLoss = activeEnemies
            .Select(enemy => baseModel.GetWarProgressScore(kingdom, enemy).ResultNumber)
            .Where(score => score < 0f)
            .DefaultIfEmpty(0f)
            .Average();

        if (averageLoss >= 0f)
        {
            return 0f;
        }

        return Math.Min(1f, Math.Abs(averageLoss) / 85f);
    }

    private static float GetDirectWarExhaustion(DiplomacyModel baseModel, Kingdom attacker, Kingdom defender)
    {
        if (!attacker.IsAtWarWith(defender))
        {
            return 0f;
        }

        float progress = baseModel.GetWarProgressScore(attacker, defender).ResultNumber;
        if (progress >= 10f)
        {
            return 0f;
        }

        return Math.Min(1f, (10f - progress) / 95f);
    }

    private static float GetPeacePressureFromMomentum(Kingdom attacker, Kingdom defender)
    {
        float momentum = RFWarStrategicMemoryBehavior.GetMomentum(attacker, defender);
        if (momentum >= 0f)
        {
            return 0f;
        }

        return Math.Min(1f, Math.Abs(momentum) / 1.8f);
    }

    private static float GetDefenseUrgencyFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType != Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        if (targetSettlement.IsUnderSiege)
        {
            return 1f;
        }

        if (targetSettlement.LastAttackerParty != null && targetSettlement.LastAttackerParty.IsActive)
        {
            return 0.75f;
        }

        if (string.Equals(targetSettlement.Culture?.StringId, kingdom.Culture?.StringId, StringComparison.OrdinalIgnoreCase))
        {
            return 0.35f;
        }

        return 0f;
    }

    private static float GetClaimTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            return string.Equals(targetSettlement.Culture?.StringId, kingdom.Culture?.StringId, StringComparison.OrdinalIgnoreCase) ? 0.4f : 0f;
        }

        string ownCulture = kingdom.Culture?.StringId ?? string.Empty;
        string targetCulture = targetSettlement.Culture?.StringId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ownCulture) || string.IsNullOrWhiteSpace(targetCulture))
        {
            return 0f;
        }

        if (string.Equals(ownCulture, targetCulture, StringComparison.OrdinalIgnoreCase))
        {
            return missionType == Army.ArmyTypes.Besieger ? 1f : 0.55f;
        }

        if (ownCulture.StartsWith("empire", StringComparison.OrdinalIgnoreCase) && IsImperialCulture(targetCulture))
        {
            return missionType == Army.ArmyTypes.Besieger ? 0.85f : 0.4f;
        }

        if (ownCulture == "aserai" && targetCulture == "aserai")
        {
            return missionType == Army.ArmyTypes.Besieger ? 0.75f : 0.35f;
        }

        return 0f;
    }

    private static float GetDoctrineFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        string cultureId = kingdom.Culture?.StringId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return 0f;
        }

        return cultureId.ToLowerInvariant() switch
        {
            "khuzait" => missionType == Army.ArmyTypes.Raider ? 0.8f : missionType == Army.ArmyTypes.Besieger ? -0.3f : 0f,
            "vlandia" => missionType == Army.ArmyTypes.Besieger && targetSettlement.IsTown ? 0.7f : 0f,
            "battania" => missionType == Army.ArmyTypes.Defender ? 0.85f : targetSettlement.IsVillage ? 0.3f : 0f,
            "sturgia" => missionType == Army.ArmyTypes.Defender ? 0.7f : missionType == Army.ArmyTypes.Besieger ? 0.25f : 0f,
            "empire" => missionType == Army.ArmyTypes.Besieger && targetSettlement.IsFortification ? 0.65f : 0f,
            "south_realm" => missionType == Army.ArmyTypes.Besieger && targetSettlement.IsFortification ? 0.65f : 0f,
            "west_realm" => missionType == Army.ArmyTypes.Besieger && targetSettlement.IsFortification ? 0.65f : 0f,
            "aserai" => missionType == Army.ArmyTypes.Besieger && string.Equals(targetSettlement.Culture?.StringId, "aserai", StringComparison.OrdinalIgnoreCase) ? 0.75f : 0f,
            "grimwatch" => missionType == Army.ArmyTypes.Defender ? 0.9f : 0f,
            "wulf" => missionType == Army.ArmyTypes.Besieger ? 0.45f : missionType == Army.ArmyTypes.Raider ? 0.25f : 0f,
            _ => 0f
        };
    }

    private static float GetFrontierTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        if (TouchesKingdomFrontier(kingdom, targetSettlement))
        {
            return 1f;
        }

        return targetSettlement.IsVillage ? -0.2f : -0.35f;
    }

    private static float GetAlignmentTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        bool ownGood = IsGoodCulture(kingdom.Culture?.StringId);
        bool ownEvil = IsEvilCulture(kingdom.Culture?.StringId);
        bool targetGood = IsGoodCulture(targetSettlement.Culture?.StringId);
        bool targetEvil = IsEvilCulture(targetSettlement.Culture?.StringId);
        return ((ownGood && targetEvil) || (ownEvil && targetGood)) ? 0.65f : 0f;
    }

    private static float GetSettlementMemoryFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        float heat = RFWarStrategicMemoryBehavior.GetSettlementHeat(kingdom, targetSettlement);
        if (heat <= 0f)
        {
            return 0f;
        }

        float normalized = Math.Min(1f, heat / 1.8f);
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        return missionType == Army.ArmyTypes.Defender
            ? normalized
            : normalized * (0.75f + (0.25f * Math.Max(0f, profile.RevengeBias)));
    }

    private static float GetFrontMemoryTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        Kingdom? enemy = missionType == Army.ArmyTypes.Defender
            ? kingdom.FactionsAtWarWith.OfType<Kingdom>().OrderByDescending(x => x.CurrentTotalStrength).FirstOrDefault()
            : targetSettlement.MapFaction as Kingdom;

        if (enemy == null)
        {
            return 0f;
        }

        float frontMomentum = RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, targetSettlement);
        float normalized = ClampSigned(frontMomentum / 2.2f);
        return missionType == Army.ArmyTypes.Defender ? -normalized : normalized;
    }

    private static float GetPairRivalryTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender || targetSettlement.MapFaction is not Kingdom defender)
        {
            return 0f;
        }

        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetRivalry(kingdom, defender) / 2.5f);
    }

    private static float GetPairCommitmentTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender || targetSettlement.MapFaction is not Kingdom defender)
        {
            return 0f;
        }

        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetWarCommitment(kingdom, defender) / 1f);
    }

    private static float GetOffensiveOverextensionPenalty(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        int threatenedFiefs = CountThreatenedFiefs(kingdom);
        if (threatenedFiefs <= 0)
        {
            return 0f;
        }

        if (TouchesKingdomFrontier(kingdom, targetSettlement))
        {
            return Math.Min(0.35f, threatenedFiefs / 6f);
        }

        return Math.Min(1f, 0.45f + (threatenedFiefs / 4f));
    }

    private static float GetFrontMomentumOpportunity(Kingdom attacker, Kingdom defender)
    {
        if (!defender.Fiefs.Any())
        {
            return 0f;
        }

        float best = defender.Fiefs
            .Select(town => RFWarStrategicMemoryBehavior.GetFrontMomentum(attacker, defender, town.Settlement))
            .DefaultIfEmpty(0f)
            .Max();

        return ClampSigned(best / 2.5f);
    }

    private static float GetFrontMomentumPeacePressure(Kingdom attacker, Kingdom defender)
    {
        if (!attacker.Fiefs.Any())
        {
            return 0f;
        }

        float worst = attacker.Fiefs
            .Select(town => RFWarStrategicMemoryBehavior.GetFrontMomentum(attacker, defender, town.Settlement))
            .DefaultIfEmpty(0f)
            .Min();

        return Math.Min(1f, Math.Max(0f, -worst) / 2.25f);
    }

    private static float GetOperationalStateFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        Kingdom? enemy = missionType == Army.ArmyTypes.Defender
            ? kingdom.FactionsAtWarWith.OfType<Kingdom>().OrderByDescending(x => x.CurrentTotalStrength).FirstOrDefault()
            : targetSettlement.MapFaction as Kingdom;

        if (enemy == null)
        {
            return 0f;
        }

        return RFWarOperationalRhythmBehavior.GetTargetFactor(kingdom, enemy, missionType, targetSettlement);
    }

    private static float GetCampaignPhasePeaceFactor(Kingdom attacker, Kingdom defender)
    {
        if (!attacker.IsAtWarWith(defender))
        {
            return 0f;
        }

        return RFWarCampaignPhaseBehavior.GetPhase(attacker, defender) switch
        {
            RFWarCampaignPhase.BreakFront => -0.18f,
            RFWarCampaignPhase.StripSupport => -0.08f,
            RFWarCampaignPhase.PressCastle => -0.2f,
            RFWarCampaignPhase.PressTown => -0.26f,
            RFWarCampaignPhase.DeepStrike => -0.1f,
            RFWarCampaignPhase.Stabilize => 0.2f,
            _ => 0f
        };
    }

    private static float GetCoalitionRoleFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        Kingdom? enemy = missionType == Army.ArmyTypes.Defender
            ? kingdom.FactionsAtWarWith.OfType<Kingdom>().OrderByDescending(x => x.CurrentTotalStrength).FirstOrDefault()
            : targetSettlement.MapFaction as Kingdom;

        if (enemy == null)
        {
            return 0f;
        }

        float roleFactor = RFWarCoalitionRoleBehavior.GetTargetFactor(kingdom, enemy, targetSettlement, missionType);
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        float profileRoleSynergy = GetProfileRoleSynergyFactor(profile, RFWarCoalitionRoleBehavior.GetRole(kingdom, enemy), missionType);
        return ClampSigned(roleFactor + (profileRoleSynergy * 0.45f));
    }

    private static float GetCoalitionDutyWarPressure(Kingdom attacker, Kingdom defender)
    {
        RFWarCoalitionRole role = RFWarCoalitionRoleBehavior.GetRole(attacker, defender);
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float coalitionPull = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCoalitionPullFactor(attacker, defender));
        float loyalty = Math.Max(0f, profile.CoalitionLoyalty);
        float rolePressure = role switch
        {
            RFWarCoalitionRole.Spearhead => 0.9f + Math.Max(0f, profile.OffensiveDrive) * 0.25f,
            RFWarCoalitionRole.SiegeFinisher => 0.75f + Math.Max(0f, profile.SiegePreference) * 0.25f,
            RFWarCoalitionRole.BorderShield => 0.45f + Math.Max(0f, profile.DefensiveDiscipline) * 0.15f,
            RFWarCoalitionRole.Raider => 0.55f + Math.Max(0f, profile.RaidPreference) * 0.2f,
            RFWarCoalitionRole.Reserve => 0.18f + Math.Max(0f, profile.Caution) * 0.1f,
            _ => 0f
        };

        return Math.Min(1f, coalitionPull * (0.55f + loyalty * 0.35f) + rolePressure * (0.18f + loyalty * 0.12f));
    }

    private static float GetProfileCampaignIdentityWarPressure(Kingdom attacker, Kingdom defender)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(attacker, defender);

        float offensiveIdentity = phase switch
        {
            RFWarCampaignPhase.BreakFront => Math.Max(0f, profile.OffensiveDrive) * 0.3f + Math.Max(0f, profile.FrontierParanoia) * 0.2f,
            RFWarCampaignPhase.StripSupport => Math.Max(0f, profile.RaidPreference) * 0.32f + Math.Max(0f, profile.Opportunism) * 0.18f,
            RFWarCampaignPhase.PressCastle => Math.Max(0f, profile.SiegePreference) * 0.34f + Math.Max(0f, profile.SiegePatience) * 0.18f,
            RFWarCampaignPhase.PressTown => Math.Max(0f, profile.SiegePreference) * 0.28f + Math.Max(0f, profile.Persistence) * 0.2f,
            RFWarCampaignPhase.DeepStrike => Math.Max(0f, profile.DeepStrikeBias) * 0.34f + Math.Max(0f, profile.Opportunism) * 0.18f,
            RFWarCampaignPhase.Stabilize => -(Math.Max(0f, profile.DefensiveDiscipline) * 0.18f + Math.Max(0f, profile.HomeGuardBias) * 0.18f + Math.Max(0f, profile.Caution) * 0.12f),
            _ => 0f
        };

        return ClampSigned(offensiveIdentity);
    }

    private static float GetProfileRoleSynergyFactor(RFWarStrategicProfile profile, RFWarCoalitionRole role, Army.ArmyTypes missionType)
    {
        float value = role switch
        {
            RFWarCoalitionRole.Spearhead => missionType == Army.ArmyTypes.Besieger
                ? Math.Max(0f, profile.OffensiveDrive) * 0.45f + Math.Max(0f, profile.Persistence) * 0.25f
                : missionType == Army.ArmyTypes.Raider
                    ? Math.Max(0f, profile.OffensiveDrive) * 0.18f
                    : -Math.Max(0f, profile.HomeGuardBias) * 0.12f,
            RFWarCoalitionRole.BorderShield => missionType == Army.ArmyTypes.Defender
                ? Math.Max(0f, profile.DefensiveDiscipline) * 0.42f + Math.Max(0f, profile.HomeGuardBias) * 0.32f + Math.Max(0f, profile.FrontierParanoia) * 0.18f
                : -Math.Max(0f, profile.Caution) * 0.16f,
            RFWarCoalitionRole.SiegeFinisher => missionType == Army.ArmyTypes.Besieger
                ? Math.Max(0f, profile.SiegePreference) * 0.5f + Math.Max(0f, profile.SiegePatience) * 0.28f
                : missionType == Army.ArmyTypes.Raider
                    ? -0.18f
                    : 0f,
            RFWarCoalitionRole.Raider => missionType == Army.ArmyTypes.Raider
                ? Math.Max(0f, profile.RaidPreference) * 0.48f + Math.Max(0f, profile.DeepStrikeBias) * 0.24f + Math.Max(0f, profile.Opportunism) * 0.18f
                : missionType == Army.ArmyTypes.Besieger
                    ? -0.16f
                    : 0f,
            RFWarCoalitionRole.Reserve => missionType == Army.ArmyTypes.Defender
                ? Math.Max(0f, profile.Caution) * 0.2f + Math.Max(0f, profile.DefensiveDiscipline) * 0.16f
                : -Math.Max(0f, profile.Caution) * 0.18f,
            _ => 0f
        };

        return ClampSigned(value);
    }

    private static float GetProfileMissionFactor(RFWarStrategicProfile profile, Army.ArmyTypes missionType)
    {
        return missionType switch
        {
            Army.ArmyTypes.Defender => ClampSigned(profile.DefensiveDiscipline * 0.6f + profile.HomeGuardBias * 0.25f + profile.FrontierParanoia * 0.15f),
            Army.ArmyTypes.Besieger => ClampSigned(profile.SiegePreference * 0.55f + profile.OffensiveDrive * 0.2f + profile.SiegePatience * 0.25f),
            Army.ArmyTypes.Raider => ClampSigned(profile.RaidPreference * 0.55f + profile.OffensiveDrive * 0.15f + profile.DeepStrikeBias * 0.3f),
            _ => 0f
        };
    }

    private static bool HasNeighborContact(Kingdom attacker, Kingdom defender)
    {
        foreach (Town town in attacker.Fiefs)
        {
            if (town?.Settlement == null)
            {
                continue;
            }

            foreach (Town enemyTown in defender.Fiefs)
            {
                if (enemyTown?.Settlement == null)
                {
                    continue;
                }

                if (town.Settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition) <= 19600f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TouchesKingdomFrontier(Kingdom kingdom, Settlement targetSettlement)
    {
        foreach (Town ownTown in kingdom.Fiefs)
        {
            float distanceSquared = ownTown.Settlement.GatePosition.DistanceSquared(targetSettlement.GatePosition);
            if (distanceSquared <= 19600f)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountThreatenedFiefs(Kingdom kingdom)
    {
        int threatened = 0;
        foreach (Town town in kingdom.Fiefs)
        {
            if (town.Settlement.IsUnderSiege)
            {
                threatened++;
                continue;
            }

            MobileParty? attacker = town.Settlement.LastAttackerParty;
            if (attacker != null && attacker.IsActive && attacker.MapFaction != null && kingdom.IsAtWarWith(attacker.MapFaction))
            {
                threatened++;
            }
        }

        return threatened;
    }

    private static List<MobileParty> GetEligibleWarParties(Kingdom kingdom)
    {
        return kingdom.WarPartyComponents
            .Select(component => component.MobileParty)
            .Where(party => party != null && party.IsActive && party.LeaderHero != null && !party.IsCaravan && !party.IsMilitia)
            .ToList();
    }

    private static bool IsGoodCulture(string? cultureId)
    {
        return !string.IsNullOrWhiteSpace(cultureId) && GoodCultures.Contains(cultureId!);
    }

    private static bool IsEvilCulture(string? cultureId)
    {
        return !string.IsNullOrWhiteSpace(cultureId) && EvilCultures.Contains(cultureId!);
    }

    private static bool IsImperialCulture(string? cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return false;
        }

        string id = cultureId!;
        return string.Equals(id, "empire", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "south_realm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "west_realm", StringComparison.OrdinalIgnoreCase)
            || id.StartsWith("empire_", StringComparison.OrdinalIgnoreCase);
    }

    private static float ClampSigned(float value)
    {
        return Math.Max(-1f, Math.Min(1f, value));
    }
}
