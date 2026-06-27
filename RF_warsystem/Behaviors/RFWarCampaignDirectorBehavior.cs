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

public sealed class RFWarCampaignDirectorBehavior : CampaignBehaviorBase
{
    private const float MinFocusHoldDays = 4.5f;
    private const float MaxFocusHoldDays = 18f;
    private const float BorderDistanceSquared = 32400f;

    private readonly Dictionary<string, string> _focusedEnemyByKingdom = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _focusStrengthByKingdom = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _holdUntilByKingdom = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _lastHoldTraceByKingdom = new(StringComparer.Ordinal);

    internal static RFWarCampaignDirectorBehavior? Instance { get; private set; }

    public RFWarCampaignDirectorBehavior()
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
        string enemyData = string.Empty;
        string strengthData = string.Empty;
        string holdData = string.Empty;

        if (!dataStore.IsLoading)
        {
            enemyData = SerializeStringDictionary(_focusedEnemyByKingdom);
            strengthData = SerializeFloatDictionary(_focusStrengthByKingdom);
            holdData = SerializeFloatDictionary(_holdUntilByKingdom);
        }

        dataStore.SyncData("RFWarSystem_DirectorFocusedEnemy", ref enemyData);
        dataStore.SyncData("RFWarSystem_DirectorFocusStrength", ref strengthData);
        dataStore.SyncData("RFWarSystem_DirectorHoldUntil", ref holdData);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeStringDictionary(enemyData, _focusedEnemyByKingdom);
        DeserializeFloatDictionary(strengthData, _focusStrengthByKingdom);
        DeserializeFloatDictionary(holdData, _holdUntilByKingdom);
    }

    internal static Kingdom? GetPrimaryEnemy(Kingdom kingdom)
    {
        return Instance?.GetFocusedEnemy(kingdom);
    }

    internal static float GetEnemyFocusFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetFocusFactor(kingdom, enemy) ?? 0f;
    }

    internal static float GetTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        return Instance?.GetDirectedTargetFactor(kingdom, targetSettlement, missionType) ?? 0f;
    }

    internal static float GetNewWarPenalty(Kingdom kingdom, Kingdom target)
    {
        return Instance?.GetDirectedNewWarPenalty(kingdom, target) ?? 0f;
    }

    internal static float GetPeaceHoldFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetDirectedPeaceHoldFactor(kingdom, enemy) ?? 0f;
    }

    internal static float GetCoalitionPullFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCoalitionAlignmentFactor(kingdom, enemy) ?? 0f;
    }

    internal static float GetCampaignLockFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCampaignLock(kingdom, enemy) ?? 0f;
    }

    internal static float GetDecisiveCampaignPressure(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetDecisiveOperationPressure(kingdom, enemy) ?? 0f;
    }

    internal static float GetEnemyCollapseFactor(Kingdom kingdom, Kingdom enemy)
    {
        return GetEnemyCollapsePressure(kingdom, enemy);
    }

    internal static float GetActiveDecisiveCampaignPressure(Kingdom kingdom)
    {
        return Instance?.GetExistingDecisiveCampaignPressure(kingdom) ?? 0f;
    }

    internal static float GetActiveCampaignLockFactor(Kingdom kingdom)
    {
        return Instance?.GetExistingCampaignLock(kingdom) ?? 0f;
    }

    internal static float GetUnresolvedFrontPressure(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetUnresolvedFrontFactor(kingdom, enemy) ?? 0f;
    }

    private void OnDailyTick()
    {
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null || kingdom.IsEliminated)
            {
                continue;
            }

            UpdateFocus(kingdom);
        }

        PruneDeadKingdoms();
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, TaleWorlds.CampaignSystem.Actions.MakePeaceAction.MakePeaceDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        if (GetFocusedEnemy(kingdom1) == kingdom2)
        {
            _holdUntilByKingdom.Remove(GetKingdomKey(kingdom1));
        }

        if (GetFocusedEnemy(kingdom2) == kingdom1)
        {
            _holdUntilByKingdom.Remove(GetKingdomKey(kingdom2));
        }
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, TaleWorlds.CampaignSystem.Actions.DeclareWarAction.DeclareWarDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        ForceFocus(kingdom1, kingdom2, 0.95f, 0f, "war_declared");
        ForceFocus(kingdom2, kingdom1, 1f, 0f, "war_declared");
    }

    private void UpdateFocus(Kingdom kingdom)
    {
        Kingdom? current = GetFocusedEnemy(kingdom);
        float now = GetCurrentDay();
        string key = GetKingdomKey(kingdom);
        bool holdActive = _holdUntilByKingdom.TryGetValue(key, out float holdUntil) && now < holdUntil;

        Kingdom? bestEnemy = null;
        float bestScore = float.MinValue;

        foreach (Kingdom candidate in Kingdom.All)
        {
            if (!IsCandidateValid(kingdom, candidate))
            {
                continue;
            }

            float score = GetEnemyScore(kingdom, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestEnemy = candidate;
            }
        }

        if (bestEnemy == null)
        {
            ClearFocus(kingdom);
            return;
        }

        if (holdActive && current != null && current != bestEnemy && !ShouldBreakHold(kingdom, current, bestEnemy))
        {
            TraceHoldIfNeeded(kingdom, current, bestEnemy, bestScore, holdUntil, now);
            return;
        }

        ForceFocus(kingdom, bestEnemy, NormalizeFocusStrength(bestScore), bestScore, "daily");
    }

    private void ForceFocus(Kingdom kingdom, Kingdom enemy, float strength, float rawScore, string source)
    {
        string key = GetKingdomKey(kingdom);
        Kingdom? previousEnemy = GetFocusedEnemy(kingdom);
        _focusStrengthByKingdom.TryGetValue(key, out float previousStrength);

        _focusedEnemyByKingdom[key] = GetKingdomKey(enemy);
        _focusStrengthByKingdom[key] = Math.Max(0.2f, Math.Min(1f, strength));
        _holdUntilByKingdom[key] = GetCurrentDay() + GetFocusHoldDuration(kingdom, enemy, _focusStrengthByKingdom[key], rawScore);

        bool changedEnemy = previousEnemy != enemy;
        bool changedStrength = Math.Abs(previousStrength - _focusStrengthByKingdom[key]) >= 0.08f;
        if (changedEnemy || changedStrength || string.Equals(source, "war_declared", StringComparison.Ordinal))
        {
            RFWarSystemTraceLog.FocusChanged(
                kingdom,
                enemy,
                _focusStrengthByKingdom[key],
                rawScore,
                source,
                ("threat", GetEnemyThreatScore(kingdom, enemy)),
                ("direct", GetDirectThreatPressure(kingdom, enemy)),
                ("frontier", GetFrontierPressure(kingdom, enemy)),
                ("claim", GetClaimPressure(kingdom, enemy)),
                ("rivalry", Math.Min(1f, RFWarStrategicMemoryBehavior.GetRivalry(kingdom, enemy) / 2.5f)),
                ("commitment", Math.Min(1f, RFWarStrategicMemoryBehavior.GetWarCommitment(kingdom, enemy))),
                ("theater", GetEnemyTheaterPressure(kingdom, enemy, kingdom.IsAtWarWith(enemy))),
                ("front", GetEnemyFrontPressure(kingdom, enemy, kingdom.IsAtWarWith(enemy))),
                ("external", Math.Max(0f, RFWarExternalFrontContext.GetEnemyPriority(kingdom, enemy))),
                ("holy", Math.Max(0f, RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy))),
                ("defense", Math.Max(0f, RFWarExternalFrontContext.GetCollectiveDefensePressure(kingdom, enemy))),
                ("alignment", Math.Max(0f, RFWarExternalFrontContext.GetAlignmentWarPressure(kingdom, enemy))));
        }
    }

    private void ClearFocus(Kingdom kingdom)
    {
        string key = GetKingdomKey(kingdom);
        _focusedEnemyByKingdom.Remove(key);
        _focusStrengthByKingdom.Remove(key);
        _holdUntilByKingdom.Remove(key);
    }

    private Kingdom? GetFocusedEnemy(Kingdom kingdom)
    {
        string key = GetKingdomKey(kingdom);
        if (!_focusedEnemyByKingdom.TryGetValue(key, out string? enemyKey) || string.IsNullOrWhiteSpace(enemyKey))
        {
            return null;
        }

        return Kingdom.All.FirstOrDefault(candidate => GetKingdomKey(candidate) == enemyKey && !candidate.IsEliminated);
    }

    private void TraceHoldIfNeeded(Kingdom kingdom, Kingdom currentEnemy, Kingdom candidateEnemy, float candidateScore, float holdUntil, float now)
    {
        string key = GetKingdomKey(kingdom);
        if (_lastHoldTraceByKingdom.TryGetValue(key, out float lastDay) && now - lastDay < 1f)
        {
            return;
        }

        _lastHoldTraceByKingdom[key] = now;
        RFWarSystemTraceLog.FocusHold(
            kingdom,
            currentEnemy,
            candidateEnemy,
            GetEnemyScore(kingdom, currentEnemy),
            candidateScore,
            GetCampaignLock(kingdom, currentEnemy),
            GetUnresolvedFrontFactor(kingdom, currentEnemy),
            Math.Max(0f, holdUntil - now));
    }

    private float GetFocusFactor(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetKingdomKey(kingdom);
        if (!_focusedEnemyByKingdom.TryGetValue(key, out string? focusedEnemyKey) || focusedEnemyKey != GetKingdomKey(enemy))
        {
            return 0f;
        }

        _focusStrengthByKingdom.TryGetValue(key, out float value);
        return value;
    }

    private float GetDirectedTargetFactor(Kingdom kingdom, Settlement targetSettlement, Army.ArmyTypes missionType)
    {
        Kingdom? focusedEnemy = GetFocusedEnemy(kingdom);
        if (focusedEnemy == null)
        {
            return 0f;
        }

        float focusFactor = GetFocusFactor(kingdom, focusedEnemy);
        if (focusFactor <= 0f)
        {
            return 0f;
        }

        Kingdom? targetEnemy = missionType == Army.ArmyTypes.Defender
            ? targetSettlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction as Kingdom
                ?? targetSettlement.LastAttackerParty?.MapFaction as Kingdom
            : targetSettlement.MapFaction as Kingdom;

        if (targetEnemy == null)
        {
            return 0f;
        }

        if (targetEnemy == focusedEnemy)
        {
            RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, focusedEnemy);
            float phaseFactor = phase switch
            {
                RFWarCampaignPhase.BreakFront => missionType == Army.ArmyTypes.Besieger && targetSettlement.IsFortification ? 0.1f : 0f,
                RFWarCampaignPhase.StripSupport => missionType == Army.ArmyTypes.Raider && targetSettlement.IsVillage ? 0.12f : 0f,
                RFWarCampaignPhase.PressCastle => missionType == Army.ArmyTypes.Besieger && targetSettlement.IsCastle ? 0.14f : 0f,
                RFWarCampaignPhase.PressTown => missionType == Army.ArmyTypes.Besieger && targetSettlement.IsTown ? 0.16f : 0f,
                RFWarCampaignPhase.DeepStrike => !targetSettlement.IsFortification ? 0.08f : 0f,
                RFWarCampaignPhase.Stabilize => missionType == Army.ArmyTypes.Defender ? 0.12f : -0.06f,
                _ => 0f
            };

            return missionType == Army.ArmyTypes.Defender
                ? 0.38f * focusFactor + phaseFactor
                : 0.46f * focusFactor + phaseFactor;
        }

        if (missionType == Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        if (!kingdom.IsAtWarWith(focusedEnemy))
        {
            return -0.18f * focusFactor;
        }

        float campaignLock = GetCampaignLock(kingdom, focusedEnemy);
        float unresolvedFront = GetUnresolvedFrontFactor(kingdom, focusedEnemy);
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, focusedEnemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, focusedEnemy);
        float coalitionRole = Math.Max(0f, RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(kingdom, focusedEnemy));
        float phasePenalty = RFWarCampaignPhaseBehavior.GetPhase(kingdom, focusedEnemy) switch
        {
            RFWarCampaignPhase.BreakFront => 0.08f,
            RFWarCampaignPhase.PressCastle => 0.12f,
            RFWarCampaignPhase.PressTown => 0.18f,
            _ => 0f
        };

        float penalty = (0.16f * focusFactor)
            + (0.14f * campaignLock)
            + (0.12f * unresolvedFront)
            + (0.08f * frontCommitment)
            + (0.1f * objectiveCommitment)
            + (0.05f * coalitionRole)
            + phasePenalty;
        return -Math.Min(0.72f, penalty);
    }

    private float GetDirectedNewWarPenalty(Kingdom kingdom, Kingdom target)
    {
        Kingdom? focusedEnemy = GetFocusedEnemy(kingdom);
        if (focusedEnemy == null || focusedEnemy == target || !kingdom.IsAtWarWith(focusedEnemy))
        {
            return 0f;
        }

        float focusStrength = GetFocusFactor(kingdom, focusedEnemy);
        int activeWars = kingdom.FactionsAtWarWith.OfType<Kingdom>().Count();
        float homePressure = Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.4f);
        float coalitionPull = Math.Max(0f, GetCoalitionAlignmentFactor(kingdom, focusedEnemy));
        float unresolvedFront = GetUnresolvedFrontFactor(kingdom, focusedEnemy);
        float campaignLock = GetCampaignLock(kingdom, focusedEnemy);
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, focusedEnemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, focusedEnemy);
        float consolidation = RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, focusedEnemy);
        float coalitionRole = Math.Max(0f, RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(kingdom, focusedEnemy));
        float phasePenalty = RFWarCampaignPhaseBehavior.GetPhase(kingdom, focusedEnemy) switch
        {
            RFWarCampaignPhase.BreakFront => 0.1f,
            RFWarCampaignPhase.PressCastle => 0.18f,
            RFWarCampaignPhase.PressTown => 0.24f,
            RFWarCampaignPhase.DeepStrike => 0.08f,
            _ => 0f
        };
        float operationalPenalty = RFWarOperationalRhythmBehavior.GetState(kingdom, focusedEnemy) switch
        {
            RFWarOperationalState.Besiege => 0.22f,
            RFWarOperationalState.Advance => 0.14f,
            RFWarOperationalState.Exploit => 0.1f,
            RFWarOperationalState.Defend => 0.08f,
            _ => 0f
        };
        float penalty = focusStrength * 0.36f;
        penalty += unresolvedFront * 0.32f;
        penalty += campaignLock * 0.34f;
        penalty += frontCommitment * 0.16f;
        penalty += objectiveCommitment * 0.18f;
        penalty += consolidation * 0.14f;
        penalty += Math.Min(1f, activeWars / 2f) * 0.22f;
        penalty += homePressure * 0.16f;
        penalty += coalitionPull * 0.12f;
        penalty += coalitionRole * 0.12f;
        penalty += phasePenalty;
        penalty += operationalPenalty;
        return Math.Min(1f, penalty);
    }

    private float GetDirectedPeaceHoldFactor(Kingdom kingdom, Kingdom enemy)
    {
        if (GetFocusedEnemy(kingdom) != enemy)
        {
            return 0f;
        }

        float focusStrength = GetFocusFactor(kingdom, enemy);
        float coalitionPull = Math.Max(0f, GetCoalitionAlignmentFactor(kingdom, enemy));
        float commitment = Math.Min(1f, RFWarStrategicMemoryBehavior.GetWarCommitment(kingdom, enemy));
        float momentum = Math.Max(0f, RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy) / 2.2f);
        float threat = Math.Min(1f, GetEnemyThreatScore(kingdom, enemy) / 1.15f);
        float campaignLock = GetCampaignLock(kingdom, enemy);
        float unresolvedFront = GetUnresolvedFrontFactor(kingdom, enemy);
        float consolidation = RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, enemy);
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, enemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy);
        float collapsePressure = GetEnemyCollapsePressure(kingdom, enemy);
        float coalitionRole = RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(kingdom, enemy);
        float decisiveOperation = GetDecisiveOperationPressure(kingdom, enemy);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        float phaseHold = phase switch
        {
            RFWarCampaignPhase.BreakFront => 0.12f,
            RFWarCampaignPhase.StripSupport => 0.08f,
            RFWarCampaignPhase.PressCastle => 0.2f,
            RFWarCampaignPhase.PressTown => 0.26f,
            RFWarCampaignPhase.DeepStrike => 0.09f,
            RFWarCampaignPhase.Stabilize => -0.08f,
            _ => 0f
        };
        float operationalHold = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.18f,
            RFWarOperationalState.Advance => 0.1f,
            RFWarOperationalState.Exploit => 0.08f,
            RFWarOperationalState.Defend => 0.1f,
            _ => 0f
        };

        return Math.Min(
            1f,
            focusStrength * 0.26f
            + coalitionPull * 0.16f
            + commitment * 0.14f
            + momentum * 0.08f
            + threat * 0.16f
            + campaignLock * 0.18f
            + unresolvedFront * 0.18f
            + consolidation * 0.14f
            + frontCommitment * 0.16f
            + objectiveCommitment * 0.18f
            + collapsePressure * 0.2f
            + decisiveOperation * 0.16f
            + Math.Max(0f, coalitionRole) * 0.16f
            + phaseHold
            + operationalHold);
    }

    private float GetCoalitionAlignmentFactor(Kingdom kingdom, Kingdom enemy)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        if (profile.CoalitionLoyalty <= 0f)
        {
            return 0f;
        }

        float directCoalitionPressure = Math.Max(
            RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy),
            Math.Max(
                RFWarExternalFrontContext.GetCollectiveDefensePressure(kingdom, enemy),
                RFWarExternalFrontContext.GetAlignmentWarPressure(kingdom, enemy) * 0.9f));

        int alignedPeers = 0;
        int totalPeers = 0;

        foreach (Kingdom otherKingdom in Kingdom.All)
        {
            if (otherKingdom == null || otherKingdom == kingdom || otherKingdom.IsEliminated)
            {
                continue;
            }

            if (!AreKingdomsStrategicallyAligned(kingdom, otherKingdom))
            {
                continue;
            }

            totalPeers++;
            if (otherKingdom.IsAtWarWith(enemy) || GetFocusedEnemy(otherKingdom) == enemy)
            {
                alignedPeers++;
            }
        }

        if (totalPeers == 0 || alignedPeers == 0)
        {
            return Math.Min(1f, directCoalitionPressure * Math.Max(0f, profile.CoalitionLoyalty + 0.2f));
        }

        float ratio = alignedPeers / (float)totalPeers;
        float peerFactor = ratio * Math.Max(0f, profile.CoalitionLoyalty + 0.15f);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        float phaseFactor = phase switch
        {
            RFWarCampaignPhase.BreakFront => 0.08f,
            RFWarCampaignPhase.PressCastle => 0.1f,
            RFWarCampaignPhase.PressTown => 0.14f,
            RFWarCampaignPhase.Stabilize => -0.06f,
            _ => 0f
        };

        return Math.Min(1f, peerFactor + directCoalitionPressure * 0.45f + phaseFactor);
    }

    private static bool IsCandidateValid(Kingdom kingdom, Kingdom candidate)
    {
        return candidate != null
            && candidate != kingdom
            && !candidate.IsEliminated;
    }

    private bool ShouldBreakHold(Kingdom kingdom, Kingdom currentEnemy, Kingdom bestEnemy)
    {
        if (currentEnemy == bestEnemy)
        {
            return false;
        }

        bool currentAtWar = kingdom.IsAtWarWith(currentEnemy);
        bool bestAtWar = kingdom.IsAtWarWith(bestEnemy);
        if (!currentAtWar && bestAtWar)
        {
            return true;
        }

        float currentThreat = GetEnemyThreatScore(kingdom, currentEnemy);
        float bestThreat = GetEnemyThreatScore(kingdom, bestEnemy);
        float currentScore = GetEnemyScore(kingdom, currentEnemy);
        float bestScore = GetEnemyScore(kingdom, bestEnemy);
        float currentFocusStrength = GetFocusFactor(kingdom, currentEnemy);
        float bestEmergency = GetHomelandEmergencyFactor(kingdom, bestEnemy);
        float unresolvedCurrentFront = GetUnresolvedFrontFactor(kingdom, currentEnemy);
        float currentCampaignLock = GetCampaignLock(kingdom, currentEnemy);
        float currentFrontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, currentEnemy);
        float currentObjectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, currentEnemy);
        float currentConsolidation = RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, currentEnemy);
        float currentSpecialMandate = GetSpecialWarMandate(kingdom, currentEnemy);
        float bestSpecialMandate = GetSpecialWarMandate(kingdom, bestEnemy);
        float currentCoalitionRole = Math.Max(0f, RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(kingdom, currentEnemy));
        float decisiveCurrentOperation = GetDecisiveOperationPressure(kingdom, currentEnemy);
        float profilePersistence = Math.Max(0f, RFWarStrategicProfiles.Get(kingdom).Persistence);
        RFWarCampaignPhase currentPhase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, currentEnemy);
        float currentOperationalPressure = RFWarOperationalRhythmBehavior.GetState(kingdom, currentEnemy) switch
        {
            RFWarOperationalState.Besiege => 0.28f,
            RFWarOperationalState.Advance => 0.2f,
            RFWarOperationalState.Exploit => 0.12f,
            RFWarOperationalState.Defend => 0.08f,
            _ => 0f
        };

        if (bestEmergency >= 0.9f && bestThreat - currentThreat >= 0.35f)
        {
            return true;
        }

        if (currentAtWar && !bestAtWar && currentCampaignLock >= 0.2f)
        {
            return false;
        }

        if (bestAtWar && !currentAtWar && bestThreat - currentThreat >= 0.28f)
        {
            return true;
        }

        if (currentAtWar
            && bestAtWar
            && currentCampaignLock >= 0.72f
            && unresolvedCurrentFront >= 0.62f
            && currentObjectiveCommitment >= 0.55f
            && bestEmergency < 0.92f)
        {
            return false;
        }

        if (currentAtWar
            && bestAtWar
            && decisiveCurrentOperation >= 0.58f
            && currentCampaignLock >= 0.6f
            && bestEmergency < 0.94f)
        {
            return false;
        }

        float requiredLead = unresolvedCurrentFront switch
        {
            >= 0.8f => 1.7f,
            >= 0.55f => 1.28f,
            _ => 0.82f
        };

        requiredLead += currentFocusStrength * 0.52f;
        requiredLead += profilePersistence * 0.62f;
        requiredLead += currentCampaignLock * 1.15f;
        requiredLead += currentFrontCommitment * 0.46f;
        requiredLead += currentObjectiveCommitment * 0.62f;
        requiredLead += currentConsolidation * 0.54f;
        requiredLead += currentSpecialMandate * 0.5f;
        requiredLead += currentCoalitionRole * 0.42f;
        requiredLead += decisiveCurrentOperation * 0.72f;
        requiredLead += currentOperationalPressure * 0.46f;
        requiredLead += currentPhase switch
        {
            RFWarCampaignPhase.BreakFront => 0.24f,
            RFWarCampaignPhase.PressCastle => 0.46f,
            RFWarCampaignPhase.PressTown => 0.62f,
            RFWarCampaignPhase.DeepStrike => 0.16f,
            _ => 0f
        };

        float bestLead = bestScore - currentScore;
        if (bestLead < requiredLead)
        {
            return false;
        }

        if (bestSpecialMandate - currentSpecialMandate >= 0.45f && bestLead >= requiredLead * 0.85f)
        {
            return true;
        }

        return bestThreat - currentThreat >= 0.5f || !currentAtWar;
    }

    private float GetEnemyScore(Kingdom kingdom, Kingdom enemy)
    {
        bool atWar = kingdom.IsAtWarWith(enemy);
        float strengthRatio = kingdom.CurrentTotalStrength / Math.Max(1f, enemy.CurrentTotalStrength);
        float momentum = RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy);
        float rivalry = Math.Min(1f, RFWarStrategicMemoryBehavior.GetRivalry(kingdom, enemy) / 2.5f);
        float commitment = Math.Min(1f, RFWarStrategicMemoryBehavior.GetWarCommitment(kingdom, enemy));
        float externalPriority = Math.Max(0f, RFWarExternalFrontContext.GetEnemyPriority(kingdom, enemy));
        float holyWarPressure = Math.Max(0f, RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy));
        float collectiveDefensePressure = Math.Max(0f, RFWarExternalFrontContext.GetCollectiveDefensePressure(kingdom, enemy));
        float alignmentWarPressure = Math.Max(0f, RFWarExternalFrontContext.GetAlignmentWarPressure(kingdom, enemy));
        float claimPressure = GetClaimPressure(kingdom, enemy);
        float frontier = GetFrontierPressure(kingdom, enemy);
        float threat = GetEnemyThreatScore(kingdom, enemy);
        float opportunity = GetEnemyOpportunityScore(kingdom, enemy, strengthRatio, momentum);
        float specialMandate = GetSpecialWarMandate(kingdom, enemy);
        float coalitionConvergence = GetAlignedCoalitionConvergence(kingdom, enemy);
        float sacredTargetPull = GetSacredTargetPull(kingdom, enemy);
        float intent = Math.Max(0f, RFWarStrategicIntent.GetWarIntentFactor(kingdom, enemy));
        float coalitionRole = RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(kingdom, enemy);
        float directThreat = GetDirectThreatPressure(kingdom, enemy);
        float theaterPressure = GetEnemyTheaterPressure(kingdom, enemy, atWar);
        float frontPressure = GetEnemyFrontPressure(kingdom, enemy, atWar);
        float decisiveOperation = GetDecisiveOperationPressure(kingdom, enemy);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);

        float score = 0f;
        score += atWar ? 2.4f : 0f;
        score += threat * 1.45f;
        score += directThreat * 1.35f;
        score += frontier * 1.2f;
        score += claimPressure * 0.95f;
        score += rivalry * 0.75f;
        score += commitment * 0.9f;
        score += externalPriority * 1.15f;
        score += holyWarPressure * 1.05f;
        score += collectiveDefensePressure * 1.2f;
        score += alignmentWarPressure * 0.95f;
        score += specialMandate * 1.2f;
        score += coalitionConvergence * 0.72f;
        score += Math.Max(0f, coalitionRole) * 0.65f;
        score += sacredTargetPull * 0.8f;
        score += intent * 0.55f;
        score += theaterPressure * 0.95f;
        score += frontPressure * 0.85f;
        score += opportunity * (atWar ? 0.65f : 1f);
        score += decisiveOperation * (atWar ? 0.46f : 0f);
        score += atWar && RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) == RFWarOperationalState.Besiege ? 0.55f : 0f;
        score += atWar && RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) == RFWarTheaterFocusMode.HomelandDefense ? 0.35f : 0f;
        score += atWar ? phase switch
        {
            RFWarCampaignPhase.BreakFront => 0.2f,
            RFWarCampaignPhase.StripSupport => 0.12f,
            RFWarCampaignPhase.PressCastle => 0.24f,
            RFWarCampaignPhase.PressTown => 0.3f,
            RFWarCampaignPhase.DeepStrike => 0.16f,
            RFWarCampaignPhase.Stabilize => -0.12f,
            _ => 0f
        } : 0f;
        score -= GetParallelWarDistractionPenalty(kingdom, enemy, atWar);
        if (GetFocusedEnemy(kingdom) == enemy)
        {
            score += GetFocusPersistenceBonus(kingdom, enemy, atWar);
        }

        return score;
    }

    private float GetFocusPersistenceBonus(Kingdom kingdom, Kingdom enemy, bool atWar)
    {
        float focusStrength = GetFocusFactor(kingdom, enemy);
        if (focusStrength <= 0f)
        {
            return 0f;
        }

        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        float campaignLock = GetCampaignLock(kingdom, enemy);
        float unresolvedFront = GetUnresolvedFrontFactor(kingdom, enemy);
        float decisiveOperation = GetDecisiveOperationPressure(kingdom, enemy);
        float sacredPressure = Math.Max(
            0f,
            Math.Max(
                RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy),
                RFWarExternalFrontContext.GetAlignmentWarPressure(kingdom, enemy)));
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, enemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy);
        float phaseBonus = atWar ? RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.BreakFront => 0.18f,
            RFWarCampaignPhase.PressCastle => 0.36f,
            RFWarCampaignPhase.PressTown => 0.48f,
            RFWarCampaignPhase.DeepStrike => 0.12f,
            _ => 0f
        } : 0f;
        float operationalBonus = atWar ? RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.32f,
            RFWarOperationalState.Advance => 0.18f,
            RFWarOperationalState.Exploit => 0.12f,
            RFWarOperationalState.Defend => 0.1f,
            _ => 0f
        } : 0f;
        float bonus = atWar ? 0.55f : 0.18f;
        bonus += focusStrength * (atWar ? 0.8f : 0.35f);
        bonus += campaignLock * 0.72f;
        bonus += unresolvedFront * 0.65f;
        bonus += frontCommitment * 0.24f;
        bonus += objectiveCommitment * 0.28f;
        bonus += decisiveOperation * 0.34f;
        bonus += unresolvedFront * Math.Max(0f, profile.SiegePatience) * 0.25f;
        bonus += atWar ? Math.Max(0f, profile.FrontierParanoia) * 0.12f : 0f;
        bonus += atWar ? Math.Max(0f, profile.SacredZeal) * sacredPressure * 0.18f : 0f;
        bonus += phaseBonus;
        bonus += operationalBonus;
        return bonus;
    }

    private float GetCampaignLock(Kingdom kingdom, Kingdom enemy)
    {
        if (GetFocusedEnemy(kingdom) != enemy || !kingdom.IsAtWarWith(enemy))
        {
            return 0f;
        }

        float focusStrength = GetFocusFactor(kingdom, enemy);
        float unresolvedFront = GetUnresolvedFrontFactor(kingdom, enemy);
        float threat = Math.Min(1f, GetEnemyThreatScore(kingdom, enemy) / 1.15f);
        float consolidation = RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, enemy);
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, enemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy);
        float collapsePressure = GetEnemyCollapsePressure(kingdom, enemy);
        float specialMandate = GetSpecialWarMandate(kingdom, enemy);
        float coalitionPull = Math.Max(0f, GetCoalitionAlignmentFactor(kingdom, enemy));
        float decisiveOperation = GetDecisiveOperationPressure(kingdom, enemy);
        float phaseLock = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.BreakFront => 0.16f,
            RFWarCampaignPhase.StripSupport => 0.08f,
            RFWarCampaignPhase.PressCastle => 0.26f,
            RFWarCampaignPhase.PressTown => 0.34f,
            RFWarCampaignPhase.DeepStrike => 0.1f,
            RFWarCampaignPhase.Stabilize => -0.08f,
            _ => 0f
        };

        float operationalLock = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.18f,
            RFWarOperationalState.Advance => 0.1f,
            RFWarOperationalState.Exploit => 0.08f,
            RFWarOperationalState.Defend => 0.14f,
            RFWarOperationalState.Regroup => 0.05f,
            _ => 0f
        };

        float value = (focusStrength * 0.34f)
            + (unresolvedFront * 0.26f)
            + (consolidation * 0.16f)
            + (frontCommitment * 0.22f)
            + (objectiveCommitment * 0.28f)
            + (collapsePressure * 0.18f)
            + (threat * 0.12f)
            + (specialMandate * 0.16f)
            + (coalitionPull * 0.1f)
            + (decisiveOperation * 0.22f)
            + phaseLock
            + operationalLock;
        return Math.Min(1f, Math.Max(0f, value));
    }

    private float GetFocusHoldDuration(Kingdom kingdom, Kingdom enemy, float focusStrength, float rawScore)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        float campaignLock = GetCampaignLock(kingdom, enemy);
        float unresolvedFront = GetUnresolvedFrontFactor(kingdom, enemy);
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, enemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy);
        float collapsePressure = GetEnemyCollapsePressure(kingdom, enemy);
        float specialMandate = GetSpecialWarMandate(kingdom, enemy);
        float coalitionRole = Math.Max(0f, RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(kingdom, enemy));
        float decisiveOperation = GetDecisiveOperationPressure(kingdom, enemy);
        float scoreSignal = Math.Max(0f, Math.Min(1f, rawScore / 6f));
        float phaseHold = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.BreakFront => 1.35f,
            RFWarCampaignPhase.StripSupport => 0.7f,
            RFWarCampaignPhase.PressCastle => 2.3f,
            RFWarCampaignPhase.PressTown => 2.9f,
            RFWarCampaignPhase.DeepStrike => 0.6f,
            RFWarCampaignPhase.Stabilize => -0.6f,
            _ => 0f
        };

        float holdDays = MinFocusHoldDays;
        holdDays += focusStrength * 2.1f;
        holdDays += Math.Max(0f, profile.Persistence) * 2.6f;
        holdDays += campaignLock * 3.9f;
        holdDays += unresolvedFront * 1.45f;
        holdDays += frontCommitment * 1.35f;
        holdDays += objectiveCommitment * 1.7f;
        holdDays += collapsePressure * 1.45f;
        holdDays += specialMandate * 1.45f;
        holdDays += coalitionRole * 0.85f;
        holdDays += decisiveOperation * 1.9f;
        holdDays += scoreSignal * 0.75f;
        holdDays += phaseHold;

        if (!kingdom.IsAtWarWith(enemy))
        {
            holdDays -= 1.25f;
        }

        return Math.Max(MinFocusHoldDays, Math.Min(MaxFocusHoldDays, holdDays));
    }

    private float GetExistingCampaignLock(Kingdom kingdom)
    {
        Kingdom? focusedEnemy = GetFocusedEnemy(kingdom);
        if (focusedEnemy == null || !kingdom.IsAtWarWith(focusedEnemy))
        {
            return 0f;
        }

        return GetCampaignLock(kingdom, focusedEnemy);
    }

    private float GetExistingDecisiveCampaignPressure(Kingdom kingdom)
    {
        Kingdom? focusedEnemy = GetFocusedEnemy(kingdom);
        if (focusedEnemy == null || !kingdom.IsAtWarWith(focusedEnemy))
        {
            return 0f;
        }

        return GetDecisiveOperationPressure(kingdom, focusedEnemy);
    }

    private float GetUnresolvedFrontFactor(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.IsAtWarWith(enemy))
        {
            return 0f;
        }

        float threat = Math.Min(1f, GetEnemyThreatScore(kingdom, enemy) / 1.1f);
        float frontier = Math.Max(0f, GetFrontierPressure(kingdom, enemy));
        float commitment = Math.Min(1f, RFWarStrategicMemoryBehavior.GetWarCommitment(kingdom, enemy));
        float negativeMomentum = Math.Min(1f, Math.Max(0f, -RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy)) / 1.9f);
        float consolidation = RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, enemy);
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, enemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy);
        float decisiveOperation = GetDecisiveOperationPressure(kingdom, enemy);
        float activeOperation = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.4f,
            RFWarOperationalState.Defend => 0.45f,
            RFWarOperationalState.Regroup => 0.35f,
            _ => 0.2f
        };

        float value = (threat * 0.34f)
            + (frontier * 0.13f)
            + (commitment * 0.14f)
            + (negativeMomentum * 0.08f)
            + (consolidation * 0.12f)
            + (frontCommitment * 0.18f)
            + (objectiveCommitment * 0.2f)
            + (decisiveOperation * 0.18f)
            + activeOperation;
        return Math.Min(1f, value);
    }

    private float GetDecisiveOperationPressure(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.IsAtWarWith(enemy))
        {
            return 0f;
        }

        int offensiveSieges = enemy.Fiefs.Count(town =>
            town?.Settlement != null
            && town.Settlement.IsUnderSiege
            && town.Settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == kingdom);
        int defensiveSieges = kingdom.Fiefs.Count(town =>
            town?.Settlement != null
            && town.Settlement.IsUnderSiege
            && town.Settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy);

        float siegePressure = offensiveSieges switch
        {
            >= 2 => 0.42f,
            1 => 0.26f,
            _ => 0f
        };

        float homelandPressure = defensiveSieges switch
        {
            >= 2 => 0.38f,
            1 => 0.22f,
            _ => 0f
        };

        float phasePressure = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.BreakFront => 0.12f,
            RFWarCampaignPhase.PressCastle => 0.22f,
            RFWarCampaignPhase.PressTown => 0.28f,
            RFWarCampaignPhase.DeepStrike => 0.08f,
            _ => 0f
        };

        float operationalPressure = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.28f,
            RFWarOperationalState.Advance => 0.14f,
            RFWarOperationalState.Exploit => 0.1f,
            RFWarOperationalState.Defend => 0.12f,
            _ => 0f
        };

        return Math.Min(1f, siegePressure + homelandPressure + phasePressure + operationalPressure);
    }

    private static float GetSpecialWarMandate(Kingdom kingdom, Kingdom enemy)
    {
        float holy = Math.Max(0f, RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy));
        float defense = Math.Max(0f, RFWarExternalFrontContext.GetCollectiveDefensePressure(kingdom, enemy));
        float alignment = Math.Max(0f, RFWarExternalFrontContext.GetAlignmentWarPressure(kingdom, enemy));
        float pending = Math.Max(0f, RFWarSpecialAuthorityBehavior.GetPendingWarPriority(kingdom, enemy));
        return Math.Min(1f, Math.Max(pending, Math.Max(holy, Math.Max(defense * 1.05f, alignment * 0.92f))));
    }

    private float GetParallelWarDistractionPenalty(Kingdom kingdom, Kingdom enemy, bool atWar)
    {
        Kingdom? focusedEnemy = GetFocusedEnemy(kingdom);
        if (focusedEnemy == null || focusedEnemy == enemy || !kingdom.IsAtWarWith(focusedEnemy))
        {
            return 0f;
        }

        float focusStrength = GetFocusFactor(kingdom, focusedEnemy);
        float campaignLock = GetCampaignLock(kingdom, focusedEnemy);
        float unresolvedFront = GetUnresolvedFrontFactor(kingdom, focusedEnemy);
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, focusedEnemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, focusedEnemy);
        float consolidation = RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, focusedEnemy);
        float specialMandate = GetSpecialWarMandate(kingdom, focusedEnemy);
        float decisiveOperation = GetDecisiveOperationPressure(kingdom, focusedEnemy);
        float operationalPenalty = RFWarOperationalRhythmBehavior.GetState(kingdom, focusedEnemy) switch
        {
            RFWarOperationalState.Besiege => 0.22f,
            RFWarOperationalState.Advance => 0.14f,
            RFWarOperationalState.Exploit => 0.1f,
            _ => 0f
        };
        float phasePenalty = RFWarCampaignPhaseBehavior.GetPhase(kingdom, focusedEnemy) switch
        {
            RFWarCampaignPhase.BreakFront => 0.12f,
            RFWarCampaignPhase.PressCastle => 0.2f,
            RFWarCampaignPhase.PressTown => 0.28f,
            RFWarCampaignPhase.DeepStrike => 0.08f,
            _ => 0f
        };

        float penalty = focusStrength * 0.42f
            + campaignLock * 0.4f
            + unresolvedFront * 0.26f
            + frontCommitment * 0.18f
            + objectiveCommitment * 0.22f
            + consolidation * 0.16f
            + specialMandate * 0.14f
            + decisiveOperation * 0.18f
            + operationalPenalty
            + phasePenalty;

        if (!atWar)
        {
            penalty += 0.45f + (campaignLock * 0.16f) + (objectiveCommitment * 0.1f);
        }

        return Math.Min(1.95f, penalty);
    }

    private static float GetAlignedCoalitionConvergence(Kingdom kingdom, Kingdom enemy)
    {
        int alignedPeers = 0;
        int convergingPeers = 0;

        foreach (Kingdom otherKingdom in Kingdom.All)
        {
            if (otherKingdom == null || otherKingdom == kingdom || otherKingdom.IsEliminated)
            {
                continue;
            }

            if (!AreKingdomsStrategicallyAligned(kingdom, otherKingdom))
            {
                continue;
            }

            alignedPeers++;
            if (otherKingdom.IsAtWarWith(enemy) || GetPrimaryEnemy(otherKingdom) == enemy)
            {
                convergingPeers++;
            }
        }

        if (alignedPeers == 0)
        {
            return 0f;
        }

        return convergingPeers / (float)alignedPeers;
    }

    private static float GetSacredTargetPull(Kingdom kingdom, Kingdom enemy)
    {
        if (!enemy.Fiefs.Any())
        {
            return 0f;
        }

        float best = 0f;
        foreach (Town town in enemy.Fiefs)
        {
            Settlement? settlement = town?.Settlement;
            if (settlement == null)
            {
                continue;
            }

            best = Math.Max(best, Math.Max(0f, RFWarExternalFrontContext.GetSacredTargetFactor(kingdom, enemy, settlement)));
        }

        return Math.Min(1f, best);
    }

    private float GetHomelandEmergencyFactor(Kingdom kingdom, Kingdom enemy)
    {
        float threat = Math.Min(1f, GetEnemyThreatScore(kingdom, enemy) / 1.05f);
        float homePressure = Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.1f);
        float homelandDefense = RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) == RFWarTheaterFocusMode.HomelandDefense ? 0.35f : 0f;
        float directThreat = GetDirectThreatPressure(kingdom, enemy);
        return Math.Min(1f, (threat * 0.34f) + (directThreat * 0.32f) + (homePressure * 0.24f) + homelandDefense);
    }

    private static float GetEnemyThreatScore(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.Fiefs.Any())
        {
            return 0f;
        }

        float threatened = 0f;
        foreach (Town town in kingdom.Fiefs)
        {
            Settlement settlement = town.Settlement;
            if (settlement.IsUnderSiege && settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy)
            {
                threatened += settlement.IsTown ? 1.15f : 0.9f;
                continue;
            }

            if (settlement.LastAttackerParty?.MapFaction == enemy)
            {
                threatened += settlement.IsTown ? 0.7f : 0.5f;
            }

            threatened += Math.Max(0f, -RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, settlement)) * 0.3f;
        }

        threatened += GetDirectThreatPressure(kingdom, enemy) * 0.65f;
        return Math.Min(1.65f, threatened / Math.Max(1f, kingdom.Fiefs.Count));
    }

    private static float GetEnemyOpportunityScore(Kingdom kingdom, Kingdom enemy, float strengthRatio, float momentum)
    {
        float value = 0f;
        value += Math.Max(0f, Math.Min(1f, (strengthRatio - 1f) / 0.55f)) * 0.5f;
        value += Math.Max(0f, Math.Min(1f, momentum / 2.2f)) * 0.3f;
        value += Math.Max(0f, Math.Min(1f, GetSoftEnemyBorderScore(enemy, kingdom))) * 0.2f;
        value += GetEnemyCollapsePressure(kingdom, enemy) * 0.42f;
        return value;
    }

    private static float GetEnemyCollapsePressure(Kingdom kingdom, Kingdom enemy)
    {
        if (!enemy.Fiefs.Any())
        {
            return 1f;
        }

        float lowFiefPressure = enemy.Fiefs.Count switch
        {
            <= 1 => 0.88f,
            2 => 0.68f,
            3 => 0.44f,
            4 => 0.22f,
            _ => 0f
        };

        int townsLeft = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsTown);
        float townLossPressure = townsLeft switch
        {
            0 => 0.34f,
            1 => 0.22f,
            2 => 0.08f,
            _ => 0f
        };

        float siegePressure = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsUnderSiege)
            / (float)Math.Max(1, enemy.Fiefs.Count());
        float fortificationSiegePressure = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsFortification && town.Settlement.IsUnderSiege)
            / (float)Math.Max(1, enemy.Fiefs.Count());
        float homePressure = Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(enemy) / 2.25f);
        float borderSoftness = Math.Max(0f, Math.Min(1f, GetSoftEnemyBorderScore(enemy, kingdom)));
        float enemyDistraction = Math.Min(1f, Math.Max(0, enemy.FactionsAtWarWith.OfType<Kingdom>().Count() - 1) / 3f);
        return Math.Min(1f, lowFiefPressure + townLossPressure + (siegePressure * 0.2f) + (fortificationSiegePressure * 0.14f) + (homePressure * 0.18f) + (borderSoftness * 0.12f) + (enemyDistraction * 0.1f));
    }

    private static float GetSoftEnemyBorderScore(Kingdom enemy, Kingdom observer)
    {
        if (!enemy.Fiefs.Any())
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;
        foreach (Town town in enemy.Fiefs)
        {
            if (town?.Settlement == null || !IsFrontierSettlement(observer, town.Settlement))
            {
                continue;
            }

            total += GetSettlementSoftness(town.Settlement);
            count++;
        }

        return count == 0 ? 0f : total / count;
    }

    private static float GetFrontierPressure(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.Fiefs.Any() || !enemy.Fiefs.Any())
        {
            return -0.2f;
        }

        if (kingdom.Fiefs.Any(town => town?.Settlement != null && IsNearEnemy(kingdom, enemy, town.Settlement)))
        {
            return 1f;
        }

        float minDistanceSquared = float.MaxValue;
        foreach (Town ownTown in kingdom.Fiefs)
        {
            foreach (Town enemyTown in enemy.Fiefs)
            {
                float distanceSquared = ownTown.Settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition);
                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                }
            }
        }

        float distance = (float)Math.Sqrt(minDistanceSquared);
        if (distance >= 420f)
        {
            return -0.65f;
        }

        if (distance <= 140f)
        {
            return 0.9f;
        }

        return Math.Max(-0.65f, Math.Min(0.9f, 0.65f - ((distance - 140f) / 280f)));
    }

    private static float GetDirectThreatPressure(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.Fiefs.Any())
        {
            return 0f;
        }

        float total = 0f;
        foreach (Town town in kingdom.Fiefs)
        {
            Settlement? settlement = town?.Settlement;
            if (settlement == null)
            {
                continue;
            }

            if (settlement.IsUnderSiege && settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy)
            {
                total += settlement.IsTown ? 1.2f : 0.95f;
                continue;
            }

            if (settlement.LastAttackerParty?.MapFaction == enemy)
            {
                total += settlement.IsTown ? 0.82f : 0.6f;
                continue;
            }

            if (IsNearEnemy(kingdom, enemy, settlement))
            {
                total += settlement.IsTown ? 0.28f : 0.18f;
            }
        }

        return Math.Min(1f, total / Math.Max(1f, kingdom.Fiefs.Count));
    }

    private static float GetEnemyTheaterPressure(Kingdom kingdom, Kingdom enemy, bool atWar)
    {
        if (!atWar)
        {
            return 0f;
        }

        float directThreat = GetDirectThreatPressure(kingdom, enemy);
        Settlement? anchor = RFWarTheaterBehavior.GetAnchorSettlement(kingdom, enemy);
        float anchorHeat = anchor != null
            ? Math.Min(1f, RFWarStrategicMemoryBehavior.GetSettlementHeat(kingdom, anchor) / 2f)
            : 0f;

        return RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) switch
        {
            RFWarTheaterFocusMode.HomelandDefense => Math.Min(1f, 0.42f + (directThreat * 0.4f) + (anchorHeat * 0.18f)),
            RFWarTheaterFocusMode.OffensivePush => Math.Min(1f, 0.18f + (GetSacredTargetPull(kingdom, enemy) * 0.22f) + (anchorHeat * 0.12f)),
            _ => 0f
        };
    }

    private static float GetEnemyFrontPressure(Kingdom kingdom, Kingdom enemy, bool atWar)
    {
        if (!atWar)
        {
            return 0f;
        }

        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(kingdom, enemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(kingdom, enemy);
        float consolidation = RFWarObjectiveChainBehavior.GetSectorConsolidationFactor(kingdom, enemy);
        return Math.Min(1f, (frontCommitment * 0.42f) + (objectiveCommitment * 0.42f) + (consolidation * 0.16f));
    }

    private static float GetClaimPressure(Kingdom kingdom, Kingdom enemy)
    {
        string cultureId = kingdom.Culture?.StringId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return 0f;
        }

        int matches = enemy.Fiefs.Count(town =>
            string.Equals(town.Settlement.Culture?.StringId, cultureId, StringComparison.OrdinalIgnoreCase));

        if (matches <= 0 && IsImperialCulture(cultureId))
        {
            matches = enemy.Fiefs.Count(town => IsImperialCulture(town.Settlement.Culture?.StringId));
        }

        return Math.Min(1f, matches / 3f);
    }

    private static bool IsFrontierSettlement(Kingdom kingdom, Settlement settlement)
    {
        foreach (Town ownTown in kingdom.Fiefs)
        {
            if (ownTown?.Settlement == null)
            {
                continue;
            }

            if (ownTown.Settlement.GatePosition.DistanceSquared(settlement.GatePosition) <= BorderDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearEnemy(Kingdom kingdom, Kingdom enemy, Settlement settlement)
    {
        foreach (Town enemyTown in enemy.Fiefs)
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

    private static bool AreKingdomsStrategicallyAligned(Kingdom left, Kingdom right)
    {
        string? leftCulture = left.Culture?.StringId;
        string? rightCulture = right.Culture?.StringId;
        if (IsImperialCulture(leftCulture) && IsImperialCulture(rightCulture))
        {
            return true;
        }

        return string.Equals(leftCulture, rightCulture, StringComparison.OrdinalIgnoreCase)
            || IsAlignmentHostileToSameSide(leftCulture, rightCulture);
    }

    private static bool IsAlignmentHostileToSameSide(string? leftCultureId, string? rightCultureId)
    {
        return IsGoodCulture(leftCultureId) && IsGoodCulture(rightCultureId)
            || IsEvilCulture(leftCultureId) && IsEvilCulture(rightCultureId);
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

    private static float NormalizeFocusStrength(float rawScore)
    {
        return Math.Max(0.2f, Math.Min(1f, rawScore / 5.2f));
    }

    private void PruneDeadKingdoms()
    {
        List<string> invalid = _focusedEnemyByKingdom
            .Where(pair => Kingdom.All.All(kingdom => GetKingdomKey(kingdom) != pair.Key || kingdom.IsEliminated))
            .Select(pair => pair.Key)
            .ToList();

        foreach (string key in invalid)
        {
            _focusedEnemyByKingdom.Remove(key);
            _focusStrengthByKingdom.Remove(key);
            _holdUntilByKingdom.Remove(key);
        }
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ElapsedDaysUntilNow;
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
