using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RF_warsystem.Diagnostics;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Behaviors;

public sealed class RFWarCoalitionRoleBehavior : CampaignBehaviorBase
{
    private readonly Dictionary<string, string> _roleByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _holdUntilByPair = new(StringComparer.Ordinal);

    internal static RFWarCoalitionRoleBehavior? Instance { get; private set; }

    public RFWarCoalitionRoleBehavior()
    {
        Instance = this;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
    }

    public override void SyncData(IDataStore dataStore)
    {
        string roleData = string.Empty;
        string holdData = string.Empty;

        if (!dataStore.IsLoading)
        {
            roleData = SerializeStringDictionary(_roleByPair);
            holdData = SerializeFloatDictionary(_holdUntilByPair);
        }

        dataStore.SyncData("RFWarSystem_CoalitionRoles", ref roleData);
        dataStore.SyncData("RFWarSystem_CoalitionRoleHoldUntil", ref holdData);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeStringDictionary(roleData, _roleByPair);
        DeserializeFloatDictionary(holdData, _holdUntilByPair);
    }

    internal static RFWarCoalitionRole GetRole(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentRole(kingdom, enemy) ?? RFWarCoalitionRole.None;
    }

    internal static float GetTargetFactor(Kingdom kingdom, Kingdom enemy, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        return Instance?.GetRoleTargetFactor(kingdom, enemy, targetSettlement, missionType) ?? 0f;
    }

    internal static float GetCampaignRoleFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentCampaignRoleFactor(kingdom, enemy) ?? 0f;
    }

    private void OnDailyTick()
    {
        PruneInactivePairs();

        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null || kingdom.IsEliminated)
            {
                continue;
            }

            foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
            {
                if (enemy == null || enemy.IsEliminated)
                {
                    continue;
                }

                UpdateRole(kingdom, enemy);
            }
        }
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, TaleWorlds.CampaignSystem.Actions.MakePeaceAction.MakePeaceDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        RemovePair(kingdom1, kingdom2);
        RemovePair(kingdom2, kingdom1);
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, TaleWorlds.CampaignSystem.Actions.DeclareWarAction.DeclareWarDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        ForceRole(kingdom1, kingdom2, EvaluateRole(kingdom1, kingdom2), "war_declared");
        ForceRole(kingdom2, kingdom1, EvaluateRole(kingdom2, kingdom1), "war_declared");
    }

    private void UpdateRole(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        RFWarCoalitionRole desired = EvaluateRole(kingdom, enemy);
        RFWarCoalitionRole current = GetCurrentRole(kingdom, enemy);
        float now = GetCurrentDay();

        if (current == desired)
        {
            if (!_holdUntilByPair.ContainsKey(key))
            {
                _holdUntilByPair[key] = now + GetHoldDuration(desired);
            }

            return;
        }

        if (!_holdUntilByPair.TryGetValue(key, out float holdUntil) || now >= holdUntil || ShouldBreakHold(kingdom, enemy, current, desired))
        {
            ForceRole(kingdom, enemy, desired, "daily");
        }
    }

    private void ForceRole(Kingdom kingdom, Kingdom enemy, RFWarCoalitionRole role, string source)
    {
        string key = GetPairKey(kingdom, enemy);
        RFWarCoalitionRole previous = GetCurrentRole(kingdom, enemy);
        _roleByPair[key] = role.ToString();
        _holdUntilByPair[key] = GetCurrentDay() + GetHoldDuration(role);

        if (previous != role)
        {
            RFWarSystemTraceLog.CoalitionRoleChanged(
                kingdom,
                enemy,
                previous.ToString(),
                role.ToString(),
                source,
                ("holy", RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy)),
                ("defense", RFWarExternalFrontContext.GetCollectiveDefensePressure(kingdom, enemy)),
                ("alignment", RFWarExternalFrontContext.GetAlignmentWarPressure(kingdom, enemy)),
                ("peerSpear", GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.Spearhead)),
                ("peerShield", GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.BorderShield)),
                ("peerSiege", GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.SiegeFinisher)),
                ("peerRaid", GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.Raider)),
                ("readiness", GetReadiness(kingdom)),
                ("strength", kingdom.CurrentTotalStrength / Math.Max(1f, enemy.CurrentTotalStrength)));
        }
    }

    private RFWarCoalitionRole GetCurrentRole(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        if (_roleByPair.TryGetValue(key, out string? rawRole) &&
            Enum.TryParse(rawRole, ignoreCase: true, out RFWarCoalitionRole role))
        {
            return role;
        }

        return RFWarCoalitionRole.None;
    }

    private RFWarCoalitionRole EvaluateRole(Kingdom kingdom, Kingdom enemy)
    {
        float holyWarPressure = RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy);
        float collectiveDefensePressure = RFWarExternalFrontContext.GetCollectiveDefensePressure(kingdom, enemy);
        float alignmentWarPressure = RFWarExternalFrontContext.GetAlignmentWarPressure(kingdom, enemy);
        float coalitionContext = Math.Max(holyWarPressure, Math.Max(collectiveDefensePressure, alignmentWarPressure));
        float coalitionPull = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCoalitionPullFactor(kingdom, enemy));

        if (coalitionContext < 0.2f && coalitionPull < 0.18f)
        {
            return RFWarCoalitionRole.None;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        float readiness = GetReadiness(kingdom);
        float strengthRatio = kingdom.CurrentTotalStrength / Math.Max(1f, enemy.CurrentTotalStrength);
        float homeThreat = GetHomeThreatRatio(kingdom, enemy);
        float borderContact = GetBorderContactFactor(kingdom, enemy);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        RFWarOperationalState operationalState = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy);
        float campaignLock = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, enemy));
        float objectiveCommitment = Math.Max(0f, RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy));
        float decisivePressure = Math.Max(0f, RFWarCampaignDirectorBehavior.GetDecisiveCampaignPressure(kingdom, enemy));
        float enemyCollapse = GetEnemyCollapsePressure(enemy);
        float peerLeadFactor = GetPeerLeadFactor(kingdom, enemy);
        float peerFrontierFactor = GetPeerFrontierFactor(kingdom, enemy);
        float peerSiegeFactor = GetPeerSiegeFactor(kingdom, enemy);
        float peerRaidFactor = GetPeerRaidFactor(kingdom, enemy);
        float spearheadShare = GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.Spearhead);
        float shieldShare = GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.BorderShield);
        float siegeShare = GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.SiegeFinisher);
        float raiderShare = GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.Raider);
        float reserveShare = GetPeerRoleShare(kingdom, enemy, RFWarCoalitionRole.Reserve);
        float assaultShare = spearheadShare + siegeShare;
        float coordinationPressure = Math.Min(1f, coalitionPull * 0.34f + coalitionContext * 0.26f + campaignLock * 0.18f + objectiveCommitment * 0.12f + decisivePressure * 0.1f);

        float spearheadScore =
            Math.Max(0f, strengthRatio - 0.9f) * 1.1f +
            readiness * 0.8f +
            Math.Max(0f, profile.OffensiveDrive) * 0.7f +
            Math.Max(0f, holyWarPressure) * 0.35f +
            peerLeadFactor * 0.95f +
            Math.Max(0f, 0.45f - spearheadShare) * 0.35f +
            Math.Max(0f, 0.35f - siegeShare) * 0.12f +
            Math.Max(0f, spearheadShare - 0.55f) * -0.24f +
            Math.Max(0f, siegeShare - 0.55f) * -0.16f +
            Math.Max(0f, shieldShare - 0.45f) * 0.08f +
            Math.Max(0f, homeThreat - 0.25f) * -0.26f +
            campaignLock * 0.18f +
            objectiveCommitment * 0.16f +
            decisivePressure * 0.12f +
            Math.Max(0f, 0.32f - spearheadShare) * coordinationPressure * 0.22f +
            Math.Max(0f, spearheadShare - 0.46f) * coordinationPressure * -0.18f +
            (operationalState == RFWarOperationalState.Advance ? 0.16f : 0f) +
            (operationalState == RFWarOperationalState.Exploit ? 0.12f : 0f) +
            enemyCollapse * 0.12f +
            (phase == RFWarCampaignPhase.BreakFront ? 0.3f : 0f) +
            (phase == RFWarCampaignPhase.PressTown ? 0.18f : 0f);

        float shieldScore =
            homeThreat * 1.2f +
            borderContact * 0.8f +
            Math.Max(0f, profile.DefensiveDiscipline) * 0.55f +
            Math.Max(0f, profile.HomeGuardBias) * 0.7f +
            Math.Max(0f, collectiveDefensePressure) * 0.5f +
            peerFrontierFactor * 0.9f +
            Math.Max(0f, 0.28f - shieldShare) * 0.45f +
            Math.Max(0f, shieldShare - 0.42f) * -0.18f +
            Math.Max(0f, 0.18f - reserveShare) * 0.12f +
            spearheadShare * 0.1f +
            Math.Max(0f, 0.24f - shieldShare) * coordinationPressure * 0.24f +
            Math.Max(0f, shieldShare - 0.34f) * coordinationPressure * -0.14f +
            (operationalState == RFWarOperationalState.Defend ? 0.28f : 0f) +
            (operationalState == RFWarOperationalState.Regroup ? 0.14f : 0f) +
            (phase == RFWarCampaignPhase.Stabilize ? 0.4f : 0f);

        float siegeScore =
            readiness * 0.65f +
            Math.Max(0f, profile.SiegePreference) * 0.95f +
            GetEnemyFortificationOpportunity(enemy) * 0.75f +
            holyWarPressure * 0.2f +
            peerSiegeFactor * 1f +
            spearheadShare * 0.28f +
            Math.Max(0f, 0.18f - spearheadShare) * -0.28f +
            Math.Max(0f, assaultShare - 0.92f) * -0.2f +
            Math.Max(0f, 0.35f - siegeShare) * 0.26f +
            Math.Max(0f, siegeShare - 0.48f) * -0.18f +
            Math.Max(0f, spearheadShare - 0.58f) * -0.12f +
            campaignLock * 0.12f +
            objectiveCommitment * 0.34f +
            decisivePressure * 0.2f +
            Math.Max(0f, 0.28f - siegeShare) * coordinationPressure * 0.26f +
            Math.Max(0f, siegeShare - 0.4f) * coordinationPressure * -0.18f +
            (operationalState == RFWarOperationalState.Besiege ? 0.34f : 0f) +
            (operationalState == RFWarOperationalState.Muster ? 0.1f : 0f) +
            enemyCollapse * 0.32f +
            (phase == RFWarCampaignPhase.PressCastle ? 0.55f : 0f) +
            (phase == RFWarCampaignPhase.PressTown ? 0.25f : 0f);

        float raiderScore =
            readiness * 0.45f +
            Math.Max(0f, profile.RaidPreference) * 1.05f +
            Math.Max(0f, profile.OffensiveDrive) * 0.25f +
            Math.Max(0f, alignmentWarPressure) * 0.2f +
            peerRaidFactor * 1f +
            spearheadShare * 0.16f +
            Math.Max(0f, 0.3f - raiderShare) * 0.22f +
            Math.Max(0f, raiderShare - 0.38f) * -0.16f +
            Math.Max(0f, siegeShare - 0.46f) * -0.12f +
            Math.Max(0f, shieldShare - 0.42f) * -0.08f +
            homeThreat * -0.34f +
            campaignLock * -0.08f +
            objectiveCommitment * -0.24f +
            decisivePressure * -0.18f +
            Math.Max(0f, 0.22f - raiderShare) * coordinationPressure * 0.18f +
            Math.Max(0f, raiderShare - 0.3f) * coordinationPressure * -0.14f +
            (operationalState == RFWarOperationalState.Exploit ? 0.22f : 0f) +
            (operationalState == RFWarOperationalState.Advance ? 0.08f : 0f) +
            enemyCollapse * -0.2f +
            (phase == RFWarCampaignPhase.StripSupport ? 0.65f : 0f) +
            (phase == RFWarCampaignPhase.DeepStrike ? 0.45f : 0f);

        float reserveScore =
            Math.Max(0f, 0.7f - readiness) * 0.9f +
            Math.Max(0f, 1f - strengthRatio) * 0.9f +
            Math.Max(0f, profile.Caution) * 0.55f +
            Math.Max(0f, 0.55f - peerLeadFactor) * 0.35f +
            Math.Max(0f, siegeShare - 0.45f) * 0.12f +
            Math.Max(0f, raiderShare - 0.35f) * 0.1f +
            Math.Max(0f, assaultShare - 0.82f) * 0.24f +
            Math.Max(0f, reserveShare - 0.3f) * -0.14f +
            Math.Max(0f, 0.2f - reserveShare) * coordinationPressure * 0.16f +
            Math.Max(0f, reserveShare - 0.26f) * coordinationPressure * -0.12f +
            (operationalState == RFWarOperationalState.Regroup ? 0.24f : 0f) +
            (operationalState == RFWarOperationalState.Muster ? 0.16f : 0f) +
            enemyCollapse * -0.24f +
            (phase == RFWarCampaignPhase.Stabilize ? 0.28f : 0f);

        RFWarCoalitionRole bestRole = RFWarCoalitionRole.Spearhead;
        float bestScore = spearheadScore;

        EvaluateCandidate(RFWarCoalitionRole.BorderShield, shieldScore, ref bestRole, ref bestScore);
        EvaluateCandidate(RFWarCoalitionRole.SiegeFinisher, siegeScore, ref bestRole, ref bestScore);
        EvaluateCandidate(RFWarCoalitionRole.Raider, raiderScore, ref bestRole, ref bestScore);
        EvaluateCandidate(RFWarCoalitionRole.Reserve, reserveScore, ref bestRole, ref bestScore);

        return bestRole;
    }

    private float GetCurrentCampaignRoleFactor(Kingdom kingdom, Kingdom enemy)
    {
        return GetCurrentRole(kingdom, enemy) switch
        {
            RFWarCoalitionRole.Spearhead => 0.22f,
            RFWarCoalitionRole.BorderShield => 0.16f,
            RFWarCoalitionRole.SiegeFinisher => 0.2f,
            RFWarCoalitionRole.Raider => 0.12f,
            RFWarCoalitionRole.Reserve => -0.1f,
            _ => 0f
        };
    }

    private static void EvaluateCandidate(RFWarCoalitionRole role, float score, ref RFWarCoalitionRole bestRole, ref float bestScore)
    {
        if (score > bestScore)
        {
            bestRole = role;
            bestScore = score;
        }
    }

    private static float GetHoldDuration(RFWarCoalitionRole role)
    {
        return role switch
        {
            RFWarCoalitionRole.BorderShield => 4.5f,
            RFWarCoalitionRole.Spearhead => 4f,
            RFWarCoalitionRole.SiegeFinisher => 4.2f,
            RFWarCoalitionRole.Raider => 3.4f,
            RFWarCoalitionRole.Reserve => 3.8f,
            _ => 2.5f
        };
    }

    private bool ShouldBreakHold(Kingdom kingdom, Kingdom enemy, RFWarCoalitionRole current, RFWarCoalitionRole desired)
    {
        float campaignLock = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, enemy));
        float objectiveCommitment = Math.Max(0f, RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy));

        if (current == RFWarCoalitionRole.BorderShield && desired != current && GetHomeThreatRatio(kingdom, enemy) >= 0.45f)
        {
            return false;
        }

        if (current == RFWarCoalitionRole.Spearhead
            && desired != current
            && desired != RFWarCoalitionRole.BorderShield
            && campaignLock >= 0.62f
            && objectiveCommitment >= 0.48f)
        {
            return false;
        }

        if (current == RFWarCoalitionRole.SiegeFinisher
            && desired != current
            && desired != RFWarCoalitionRole.BorderShield
            && campaignLock >= 0.58f
            && objectiveCommitment >= 0.54f)
        {
            return false;
        }

        if (desired == RFWarCoalitionRole.BorderShield && GetHomeThreatRatio(kingdom, enemy) >= 0.6f)
        {
            return true;
        }

        if (desired == RFWarCoalitionRole.Reserve && GetReadiness(kingdom) < 0.28f)
        {
            return true;
        }

        return false;
    }

    private float GetRoleTargetFactor(Kingdom kingdom, Kingdom enemy, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        bool isVillage = targetSettlement.IsVillage;
        bool isFortification = targetSettlement.IsFortification;
        bool isFrontier = IsFrontierTarget(kingdom, targetSettlement);
        float sacredFactor = RFWarExternalFrontContext.GetSacredTargetFactor(kingdom, enemy, targetSettlement);
        float coalitionConvergence = GetCoalitionTargetConvergence(kingdom, enemy, targetSettlement, missionType);
        float laneCrowding = Math.Max(0f, coalitionConvergence - 0.5f);
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        Settlement? frontline = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        Settlement? theater = RFWarTheaterBehavior.GetAnchorSettlement(kingdom, enemy);
        bool objectiveExact = objective == targetSettlement;
        bool frontlineExact = frontline == targetSettlement;
        bool objectiveCluster = IsSameClusterOrExact(targetSettlement, objective);
        bool frontlineCluster = IsSameClusterOrExact(targetSettlement, frontline);
        bool theaterCluster = IsSameClusterOrExact(targetSettlement, theater);
        bool boundObjective = isVillage && objective != null && targetSettlement.Village?.Bound == objective;
        bool boundFrontline = isVillage && frontline != null && targetSettlement.Village?.Bound == frontline;
        float laneAlignment = objectiveExact || frontlineExact
            ? 1f
            : objectiveCluster || frontlineCluster
                ? 0.6f
                : theaterCluster
                    ? 0.28f
                    : 0f;
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        RFWarOperationalState operationalState = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy);
        float campaignLock = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, enemy));
        float objectiveCommitment = Math.Max(0f, RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy));
        float coalitionDiscipline = Math.Min(1f, campaignLock * 0.55f + objectiveCommitment * 0.45f);

        return GetCurrentRole(kingdom, enemy) switch
        {
            RFWarCoalitionRole.Spearhead => missionType == Army.ArmyTypes.Defender
                ? -0.08f
                : (isFortification ? 0.3f : objectiveCluster || frontlineCluster ? 0.14f : 0.04f) + sacredFactor * 0.2f + laneAlignment * (0.3f + coalitionDiscipline * 0.08f) + coalitionConvergence * 0.12f - laneCrowding * (0.16f + coalitionDiscipline * 0.08f) + (operationalState == RFWarOperationalState.Advance ? 0.08f : 0f) + (phase == RFWarCampaignPhase.BreakFront ? 0.12f : 0f) + (phase == RFWarCampaignPhase.PressCastle || phase == RFWarCampaignPhase.PressTown ? laneAlignment * 0.1f : 0f),
            RFWarCoalitionRole.BorderShield => missionType == Army.ArmyTypes.Defender
                ? (isFrontier ? 0.32f : 0.18f) + FrontierAlignment(targetSettlement, frontline, theater) * 0.18f + coalitionConvergence * 0.12f + (operationalState == RFWarOperationalState.Defend ? 0.08f : 0f) + (phase == RFWarCampaignPhase.Stabilize ? 0.1f : 0f)
                : (isFrontier && isFortification ? 0.02f : isFrontier ? -0.06f : -0.3f),
            RFWarCoalitionRole.SiegeFinisher => missionType == Army.ArmyTypes.Defender
                ? 0f
                : (isFortification ? 0.42f : isVillage ? -0.28f : -0.02f) + sacredFactor * 0.12f + laneAlignment * (0.38f + coalitionDiscipline * 0.1f) + coalitionConvergence * 0.14f - laneCrowding * (0.18f + coalitionDiscipline * 0.1f) + (operationalState == RFWarOperationalState.Besiege ? 0.12f : 0f) + (phase == RFWarCampaignPhase.PressCastle || phase == RFWarCampaignPhase.PressTown ? 0.16f + (objectiveExact ? 0.12f : 0f) : 0f),
            RFWarCoalitionRole.Raider => missionType == Army.ArmyTypes.Raider
                ? (isVillage ? 0.46f : -0.16f) + ((boundObjective || boundFrontline) ? 0.3f : 0f) + ((objectiveCluster || frontlineCluster) && isVillage ? 0.16f : 0f) + coalitionConvergence * 0.02f - laneCrowding * (0.34f + coalitionDiscipline * 0.1f) + (objectiveExact || frontlineExact ? -0.3f - coalitionDiscipline * 0.12f : 0f) + (operationalState == RFWarOperationalState.Exploit ? 0.08f : 0f) + (phase == RFWarCampaignPhase.StripSupport || phase == RFWarCampaignPhase.DeepStrike ? 0.14f : 0f)
                : (isVillage ? 0.14f : -0.12f),
            RFWarCoalitionRole.Reserve => missionType == Army.ArmyTypes.Defender
                ? (isFrontier ? 0.18f : 0.08f) + FrontierAlignment(targetSettlement, frontline, theater) * 0.12f + (operationalState == RFWarOperationalState.Regroup ? 0.06f : 0f)
                : (isFrontier && isFortification ? 0f : isFrontier ? -0.04f : -0.22f),
            _ => 0f
        };
    }

    private static float FrontierAlignment(Settlement targetSettlement, Settlement? frontline, Settlement? theater)
    {
        if (frontline != null && (frontline == targetSettlement || frontline.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= 32400f))
        {
            return 1f;
        }

        if (theater != null && (theater == targetSettlement || theater.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= 32400f))
        {
            return 0.5f;
        }

        return 0f;
    }

    private void PruneInactivePairs()
    {
        if (_roleByPair.Count == 0)
        {
            return;
        }

        List<string> toRemove = new();
        foreach (KeyValuePair<string, string> pair in _roleByPair)
        {
            if (!IsPairStillActive(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (string key in toRemove)
        {
            _roleByPair.Remove(key);
            _holdUntilByPair.Remove(key);
        }
    }

    private static bool IsPairStillActive(string key)
    {
        string[] split = key.Split(new[] { "->" }, StringSplitOptions.None);
        if (split.Length != 2)
        {
            return false;
        }

        Kingdom? left = Kingdom.All.FirstOrDefault(kingdom => GetKingdomKey(kingdom) == split[0]);
        Kingdom? right = Kingdom.All.FirstOrDefault(kingdom => GetKingdomKey(kingdom) == split[1]);
        return left != null && right != null && !left.IsEliminated && !right.IsEliminated && left.IsAtWarWith(right);
    }

    private void RemovePair(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        _roleByPair.Remove(key);
        _holdUntilByPair.Remove(key);
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ElapsedDaysUntilNow;
    }

    private static float GetReadiness(Kingdom kingdom)
    {
        List<MobileParty> parties = kingdom.WarPartyComponents
            .Select(component => component.MobileParty)
            .Where(party => party != null && party.IsActive && party.LeaderHero != null && !party.IsCaravan && !party.IsMilitia)
            .ToList();

        if (parties.Count == 0)
        {
            return 0f;
        }

        float averageFood = parties.Average(party => Math.Min(18f, Math.Max(0f, party.GetNumDaysForFoodToLast()))) / 18f;
        float averageSize = parties.Average(party => Math.Min(1f, Math.Max(0f, party.PartySizeRatio)));
        return (averageFood * 0.55f) + (averageSize * 0.45f);
    }

    private static float GetHomeThreatRatio(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.Fiefs.Any())
        {
            return 0f;
        }

        int threatened = kingdom.Fiefs.Count(town =>
            town?.Settlement != null &&
            (town.Settlement.IsUnderSiege && town.Settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy
             || town.Settlement.LastAttackerParty?.MapFaction == enemy));

        return threatened / (float)Math.Max(1, kingdom.Fiefs.Count);
    }

    private static float GetBorderContactFactor(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.Fiefs.Any() || !enemy.Fiefs.Any())
        {
            return 0f;
        }

        foreach (Town ownTown in kingdom.Fiefs)
        {
            if (ownTown?.Settlement == null)
            {
                continue;
            }

            foreach (Town enemyTown in enemy.Fiefs)
            {
                if (enemyTown?.Settlement == null)
                {
                    continue;
                }

                if (ownTown.Settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition) <= 32400f)
                {
                    return 1f;
                }
            }
        }

        return 0.25f;
    }

    private static float GetEnemyFortificationOpportunity(Kingdom enemy)
    {
        if (!enemy.Fiefs.Any())
        {
            return 0f;
        }

        int fortifications = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsFortification);
        int softFortifications = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsFortification && GetSettlementSoftness(town.Settlement) >= 0.25f);
        if (fortifications <= 0)
        {
            return 0f;
        }

        return Math.Min(1f, softFortifications / (float)fortifications);
    }

    private static float GetEnemyCollapsePressure(Kingdom enemy)
    {
        if (!enemy.Fiefs.Any())
        {
            return 1f;
        }

        float lowFiefPressure = enemy.Fiefs.Count switch
        {
            <= 1 => 0.88f,
            2 => 0.68f,
            3 => 0.42f,
            4 => 0.18f,
            _ => 0f
        };

        float siegePressure = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsUnderSiege)
            / (float)Math.Max(1, enemy.Fiefs.Count());
        float fortPressure = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsFortification && GetSettlementSoftness(town.Settlement) >= 0.25f)
            / (float)Math.Max(1, enemy.Fiefs.Count());
        return Math.Min(1f, lowFiefPressure + (siegePressure * 0.28f) + (fortPressure * 0.16f));
    }

    private static float GetPeerLeadFactor(Kingdom kingdom, Kingdom enemy)
    {
        return GetRelativePeerFactor(
            kingdom,
            enemy,
            candidate =>
            {
                RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(candidate);
                float strengthRatio = candidate.CurrentTotalStrength / Math.Max(1f, enemy.CurrentTotalStrength);
                return Math.Max(0f, strengthRatio - 0.85f) * 0.9f
                    + GetReadiness(candidate) * 0.7f
                    + Math.Max(0f, profile.OffensiveDrive) * 0.55f
                    + Math.Max(0f, profile.Persistence) * 0.25f;
            });
    }

    private static float GetPeerFrontierFactor(Kingdom kingdom, Kingdom enemy)
    {
        return GetRelativePeerFactor(
            kingdom,
            enemy,
            candidate =>
            {
                RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(candidate);
                return GetHomeThreatRatio(candidate, enemy) * 0.95f
                    + GetBorderContactFactor(candidate, enemy) * 0.8f
                    + Math.Max(0f, profile.DefensiveDiscipline) * 0.45f
                    + Math.Max(0f, profile.HomeGuardBias) * 0.55f
                    + Math.Max(0f, profile.FrontierParanoia) * 0.3f;
            });
    }

    private static float GetPeerSiegeFactor(Kingdom kingdom, Kingdom enemy)
    {
        return GetRelativePeerFactor(
            kingdom,
            enemy,
            candidate =>
            {
                RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(candidate);
                return GetReadiness(candidate) * 0.55f
                    + Math.Max(0f, profile.SiegePreference) * 0.75f
                    + Math.Max(0f, profile.SiegePatience) * 0.4f
                    + GetEnemyFortificationOpportunity(enemy) * 0.55f;
            });
    }

    private static float GetPeerRaidFactor(Kingdom kingdom, Kingdom enemy)
    {
        return GetRelativePeerFactor(
            kingdom,
            enemy,
            candidate =>
            {
                RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(candidate);
                return GetReadiness(candidate) * 0.4f
                    + Math.Max(0f, profile.RaidPreference) * 0.85f
                    + Math.Max(0f, profile.DeepStrikeBias) * 0.55f
                    + Math.Max(0f, profile.Opportunism) * 0.35f;
            });
    }

    private static float GetPeerRoleShare(Kingdom kingdom, Kingdom enemy, RFWarCoalitionRole role)
    {
        List<Kingdom> peers = GetCoalitionPeers(kingdom, enemy);
        if (peers.Count <= 1)
        {
            return 0f;
        }

        int matching = 0;
        int total = 0;
        foreach (Kingdom peer in peers)
        {
            if (peer == kingdom)
            {
                continue;
            }

            total++;
            if (GetRole(peer, enemy) == role)
            {
                matching++;
            }
        }

        return total == 0 ? 0f : matching / (float)total;
    }

    private static float GetCoalitionTargetConvergence(Kingdom kingdom, Kingdom enemy, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        if (missionType == Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        List<Kingdom> peers = GetCoalitionPeers(kingdom, enemy);
        if (peers.Count <= 1)
        {
            return 0f;
        }

        int aligned = 0;
        int total = 0;
        foreach (Kingdom peer in peers)
        {
            if (peer == kingdom)
            {
                continue;
            }

            total++;
            if (IsTargetInsidePeerCampaignLane(peer, enemy, targetSettlement))
            {
                aligned++;
            }
        }

        return total == 0 ? 0f : aligned / (float)total;
    }

    private static bool IsTargetInsidePeerCampaignLane(Kingdom peer, Kingdom enemy, Settlement targetSettlement)
    {
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(peer, enemy);
        if (IsSameClusterOrExact(targetSettlement, objective))
        {
            return true;
        }

        Settlement? front = RFWarFrontlineBehavior.GetAnchorSettlement(peer, enemy);
        if (IsSameClusterOrExact(targetSettlement, front))
        {
            return true;
        }

        Settlement? theater = RFWarTheaterBehavior.GetAnchorSettlement(peer, enemy);
        return IsSameClusterOrExact(targetSettlement, theater);
    }

    private static bool IsSameClusterOrExact(Settlement targetSettlement, Settlement? anchor)
    {
        return anchor != null
            && (anchor == targetSettlement || anchor.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= 32400f);
    }

    private static float GetRelativePeerFactor(Kingdom kingdom, Kingdom enemy, Func<Kingdom, float> selector)
    {
        List<Kingdom> peers = GetCoalitionPeers(kingdom, enemy);
        if (peers.Count == 0)
        {
            return 0f;
        }

        float own = selector(kingdom);
        float best = peers.Max(selector);
        if (best <= 0f)
        {
            return 0f;
        }

        return Math.Max(0f, Math.Min(1f, own / best));
    }

    private static List<Kingdom> GetCoalitionPeers(Kingdom kingdom, Kingdom enemy)
    {
        List<Kingdom> peers = new();
        foreach (Kingdom candidate in Kingdom.All)
        {
            if (candidate == null || candidate.IsEliminated || candidate == kingdom)
            {
                continue;
            }

            if (!candidate.IsAtWarWith(enemy))
            {
                continue;
            }

            if (!AreKingdomsStrategicallyAligned(kingdom, candidate))
            {
                continue;
            }

            peers.Add(candidate);
        }

        peers.Add(kingdom);
        return peers;
    }

    private static bool AreKingdomsStrategicallyAligned(Kingdom left, Kingdom right)
    {
        string? leftCulture = left.Culture?.StringId;
        string? rightCulture = right.Culture?.StringId;
        if (IsImperialCulture(leftCulture) && IsImperialCulture(rightCulture))
        {
            return true;
        }

        return string.Equals(leftCulture, rightCulture, StringComparison.OrdinalIgnoreCase)
            || IsAlignmentFriendlyPair(leftCulture, rightCulture);
    }

    private static bool IsAlignmentFriendlyPair(string? leftCultureId, string? rightCultureId)
    {
        return IsGoodCulture(leftCultureId) && IsGoodCulture(rightCultureId)
            || IsEvilCulture(leftCultureId) && IsEvilCulture(rightCultureId);
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

    private static bool IsFrontierTarget(Kingdom kingdom, Settlement targetSettlement)
    {
        foreach (Town town in kingdom.Fiefs)
        {
            if (town?.Settlement == null)
            {
                continue;
            }

            if (town.Settlement.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= 19600f)
            {
                return true;
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

        return Math.Max(0f, Math.Min(1f, (threshold - defenders) / (float)threshold));
    }

    private static string GetPairKey(Kingdom kingdom, Kingdom enemy)
    {
        return $"{GetKingdomKey(kingdom)}->{GetKingdomKey(enemy)}";
    }

    private static string GetKingdomKey(Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private static string SerializeStringDictionary(Dictionary<string, string> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(";", values.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string SerializeFloatDictionary(Dictionary<string, float> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(";", values.Select(pair => $"{pair.Key}={pair.Value.ToString("R", CultureInfo.InvariantCulture)}"));
    }

    private static void DeserializeStringDictionary(string serialized, Dictionary<string, string> target)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        foreach (string entry in serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1)
            {
                continue;
            }

            target[entry.Substring(0, separatorIndex)] = entry.Substring(separatorIndex + 1);
        }
    }

    private static void DeserializeFloatDictionary(string serialized, Dictionary<string, float> target)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        foreach (string entry in serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1)
            {
                continue;
            }

            if (float.TryParse(entry.Substring(separatorIndex + 1), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float value))
            {
                target[entry.Substring(0, separatorIndex)] = value;
            }
        }
    }
}
