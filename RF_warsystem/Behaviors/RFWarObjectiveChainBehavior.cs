using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RF_warsystem.Diagnostics;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Behaviors;

public sealed class RFWarObjectiveChainBehavior : CampaignBehaviorBase
{
    private const float ClusterDistanceSquared = 32400f;
    private const float DeepPocketDistanceSquared = 129600f;

    private readonly Dictionary<string, string> _objectiveSettlementByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _lastCompletedObjectiveByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _holdUntilByPair = new(StringComparer.Ordinal);

    internal static RFWarObjectiveChainBehavior? Instance { get; private set; }

    public RFWarObjectiveChainBehavior()
    {
        Instance = this;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
    }

    public override void SyncData(IDataStore dataStore)
    {
        string objectiveState = string.Empty;
        string completedState = string.Empty;
        string holdState = string.Empty;

        if (!dataStore.IsLoading)
        {
            objectiveState = SerializeStringDictionary(_objectiveSettlementByPair);
            completedState = SerializeStringDictionary(_lastCompletedObjectiveByPair);
            holdState = SerializeFloatDictionary(_holdUntilByPair);
        }

        dataStore.SyncData("RFWarSystem_ObjectiveSettlements", ref objectiveState);
        dataStore.SyncData("RFWarSystem_LastCompletedObjectives", ref completedState);
        dataStore.SyncData("RFWarSystem_ObjectiveHoldUntil", ref holdState);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeStringDictionary(objectiveState, _objectiveSettlementByPair);
        DeserializeStringDictionary(completedState, _lastCompletedObjectiveByPair);
        DeserializeFloatDictionary(holdState, _holdUntilByPair);
    }

    internal static float GetTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        return Instance?.GetObjectiveTargetFactor(kingdom, targetSettlement, missionType) ?? 0f;
    }

    internal static Settlement? GetObjectiveSettlement(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentObjective(kingdom, enemy);
    }

    internal static float GetObjectiveCommitmentFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentObjectiveCommitmentFactor(kingdom, enemy) ?? 0f;
    }

    internal static float GetSectorConsolidationFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentSectorConsolidationFactor(kingdom, enemy) ?? 0f;
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

                UpdateObjective(kingdom, enemy);
            }
        }
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, TaleWorlds.CampaignSystem.Actions.DeclareWarAction.DeclareWarDetail detail)
    {
        if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
        {
            return;
        }

        ForceUpdate(kingdom1, kingdom2, "war_declared");
        ForceUpdate(kingdom2, kingdom1, "war_declared");
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

    private void UpdateObjective(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        Settlement? currentObjective = GetCurrentObjective(kingdom, enemy);
        Settlement? lastCompletedObjective = GetLastCompletedObjective(kingdom, enemy);

        if (currentObjective != null && currentObjective.MapFaction == kingdom)
        {
            _lastCompletedObjectiveByPair[key] = currentObjective.StringId;
            currentObjective = null;
            _objectiveSettlementByPair.Remove(key);
        }

        if (!ShouldRunOffensiveChain(kingdom, enemy))
        {
            if (currentObjective != null)
            {
                _objectiveSettlementByPair.Remove(key);
                _holdUntilByPair.Remove(key);
            }

            return;
        }

        float now = GetCurrentDay();
        if (currentObjective != null &&
            currentObjective.MapFaction == enemy &&
            _holdUntilByPair.TryGetValue(key, out float holdUntil) &&
            now < holdUntil &&
            !ShouldBreakHold(kingdom, enemy, currentObjective))
        {
            return;
        }

        Settlement? newObjective = SelectBestObjective(kingdom, enemy, currentObjective, lastCompletedObjective);
        if (newObjective == null)
        {
            _objectiveSettlementByPair.Remove(key);
            _holdUntilByPair.Remove(key);
            return;
        }

        string? previousId = currentObjective?.StringId;
        _objectiveSettlementByPair[key] = newObjective.StringId;
        _holdUntilByPair[key] = now + GetHoldDuration(kingdom, enemy, newObjective);

        if (!string.Equals(previousId, newObjective.StringId, StringComparison.Ordinal))
        {
            RFWarSystemTraceLog.ObjectiveChanged(kingdom, enemy, currentObjective, newObjective, "daily");
        }
    }

    private void ForceUpdate(Kingdom kingdom, Kingdom enemy, string source)
    {
        Settlement? previous = GetCurrentObjective(kingdom, enemy);
        Settlement? next = SelectBestObjective(kingdom, enemy, previous, GetLastCompletedObjective(kingdom, enemy));
        string key = GetPairKey(kingdom, enemy);

        if (next == null)
        {
            RemovePair(kingdom, enemy);
            return;
        }

        _objectiveSettlementByPair[key] = next.StringId;
        _holdUntilByPair[key] = GetCurrentDay() + GetHoldDuration(kingdom, enemy, next);
        if (previous == null || previous.StringId != next.StringId)
        {
            RFWarSystemTraceLog.ObjectiveChanged(kingdom, enemy, previous, next, source);
        }
    }

    private float GetObjectiveTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        if (targetSettlement == null || missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        if (targetSettlement.MapFaction is not Kingdom enemy)
        {
            return 0f;
        }

        Settlement? objective = GetCurrentObjective(kingdom, enemy);
        if (objective == null)
        {
            return 0f;
        }

        Settlement? lastCompletedObjective = GetLastCompletedObjective(kingdom, enemy);
        if (missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Raider)
        {
            if (targetSettlement.IsVillage && targetSettlement.Village.Bound == objective)
            {
                return 0.88f;
            }

            if (targetSettlement == objective)
            {
                return objective.IsTown || objective.IsCastle ? -0.12f : 0.15f;
            }
        }

        if (targetSettlement == objective)
        {
            return 0.95f;
        }

        if (targetSettlement.IsVillage && targetSettlement.Village.Bound == objective)
        {
            return missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Besieger ? 0.18f : 0.28f;
        }

        float score = 0f;
        if (IsSameCluster(objective, targetSettlement))
        {
            score += 0.34f;
        }

        if (lastCompletedObjective != null && IsSameCluster(lastCompletedObjective, targetSettlement))
        {
            score += 0.24f;
        }

        if (!IsFrontierContact(kingdom, targetSettlement) && objective.GatePosition.DistanceSquared(targetSettlement.GatePosition) >= DeepPocketDistanceSquared)
        {
            score -= 0.22f;
        }

        return ClampSigned(score);
    }

    private static bool ShouldRunOffensiveChain(Kingdom kingdom, Kingdom enemy)
    {
        if (RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) != RFWarTheaterFocusMode.OffensivePush)
        {
            return false;
        }

        RFWarOperationalState state = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy);
        if (state == RFWarOperationalState.Defend || state == RFWarOperationalState.Regroup)
        {
            return false;
        }

        return RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(kingdom, enemy) > -0.15f;
    }

    private static bool ShouldBreakHold(Kingdom kingdom, Kingdom enemy, Settlement objective)
    {
        if (objective.MapFaction != enemy)
        {
            return true;
        }

        if (RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) != RFWarTheaterFocusMode.OffensivePush)
        {
            return true;
        }

        RFWarOperationalState state = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy);
        if (state == RFWarOperationalState.Defend)
        {
            return true;
        }

        if (state == RFWarOperationalState.Regroup)
        {
            Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
            if (frontlineAnchor != null && (frontlineAnchor == objective || IsSameCluster(frontlineAnchor, objective)))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private static Settlement? SelectBestObjective(Kingdom kingdom, Kingdom enemy, Settlement? currentObjective, Settlement? lastCompletedObjective)
    {
        Settlement? theaterObjective = RFWarTheaterBehavior.GetAnchorSettlement(kingdom, enemy);
        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);

        return enemy.Fiefs
            .Select(town => town?.Settlement)
            .Where(settlement => settlement != null)
            .OrderByDescending(settlement => GetObjectiveScore(kingdom, enemy, settlement!, currentObjective, lastCompletedObjective, theaterObjective, frontlineAnchor))
            .FirstOrDefault();
    }

    private static float GetObjectiveScore(Kingdom kingdom, Kingdom enemy, Settlement settlement, Settlement? currentObjective, Settlement? lastCompletedObjective, Settlement? theaterObjective, Settlement? frontlineAnchor)
    {
        float sectorPriority = Math.Max(0f, RFWarFrontEvaluator.GetFrontSectorPriority(kingdom, enemy, settlement, homelandDefense: false));
        float collapsePressure = GetEnemyCollapsePressure(enemy, settlement);
        float campaignCollapse = Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyCollapseFactor(kingdom, enemy));
        float score = settlement.IsTown ? 2.6f : settlement.IsCastle ? 2.1f : 1f;
        score += GetSettlementSoftness(settlement) * 1.5f;
        score += Math.Max(0f, RFWarFrontEvaluator.GetTargetFrontPriority(kingdom, settlement, TaleWorlds.CampaignSystem.Army.ArmyTypes.Besieger)) * 1.7f;
        score += sectorPriority * 1.15f;
        score += Math.Max(0f, RFWarStrategicIntent.GetTargetIntentFactor(kingdom, settlement, TaleWorlds.CampaignSystem.Army.ArmyTypes.Besieger)) * 1.35f;
        score += RFWarCampaignPhaseBehavior.GetObjectiveFactor(kingdom, enemy, settlement) * 1.55f;
        score += Math.Min(1f, RFWarStrategicMemoryBehavior.GetSettlementHeat(kingdom, settlement) / 2.4f) * 0.7f;
        score += Math.Max(0f, RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, settlement)) * 1.25f;
        score += collapsePressure * 1.15f;
        score += campaignCollapse * (settlement.IsTown ? 1.4f : settlement.IsCastle ? 1.08f : 0.24f);

        if (theaterObjective != null)
        {
            if (settlement == theaterObjective)
            {
                score += 1.7f;
            }
            else if (IsSameCluster(theaterObjective, settlement))
            {
                score += 0.8f;
            }
        }

        if (frontlineAnchor != null)
        {
            if (settlement == frontlineAnchor)
            {
                score += 1.65f;
            }
            else if (IsSameCluster(frontlineAnchor, settlement))
            {
                score += 1.15f;
            }
            else
            {
                score -= 0.18f;
            }
        }

        if (currentObjective != null)
        {
            if (settlement == currentObjective)
            {
                score += 1.5f;
            }
            else if (IsSameCluster(currentObjective, settlement))
            {
                score += 0.72f;
            }
        }

        if (campaignCollapse >= 0.55f)
        {
            bool closesOnCurrentAxis =
                (currentObjective != null && (settlement == currentObjective || IsSameCluster(currentObjective, settlement)))
                || (frontlineAnchor != null && (settlement == frontlineAnchor || IsSameCluster(frontlineAnchor, settlement)));

            if (settlement.IsTown && closesOnCurrentAxis)
            {
                score += 1.02f + (campaignCollapse * 0.44f);
            }
            else if (settlement.IsCastle && closesOnCurrentAxis)
            {
                score += 0.82f + (campaignCollapse * 0.38f);
            }
            else if (settlement.IsFortification && closesOnCurrentAxis)
            {
                score += 0.78f + (campaignCollapse * 0.34f);
            }
            else if (settlement.IsVillage && !closesOnCurrentAxis)
            {
                score -= 0.5f + (campaignCollapse * 0.22f);
            }
        }

        if (campaignCollapse >= 0.72f && !settlement.IsFortification)
        {
            score -= 0.22f + (campaignCollapse * 0.12f);
        }

        if (lastCompletedObjective != null)
        {
            if (IsSameCluster(lastCompletedObjective, settlement))
            {
                score += 0.95f;
            }
            else if (!IsFrontierContact(kingdom, settlement))
            {
                score -= 0.35f;
            }
        }

        if (IsSameStrategicCulture(kingdom.Culture?.StringId, settlement.Culture?.StringId))
        {
            score += 0.65f;
        }

        if (!IsFrontierContact(kingdom, settlement))
        {
            score -= 0.2f;
        }

        return score;
    }

    private static float GetEnemyCollapsePressure(Kingdom enemy, Settlement settlement)
    {
        if (!enemy.Fiefs.Any())
        {
            return 1f;
        }

        float lowFiefPressure = enemy.Fiefs.Count switch
        {
            <= 1 => 0.9f,
            2 => 0.7f,
            3 => 0.42f,
            4 => 0.18f,
            _ => 0f
        };

        float siegePressure = enemy.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsUnderSiege)
            / (float)Math.Max(1, enemy.Fiefs.Count());
        float localClusterPressure = enemy.Fiefs.Count(town => town?.Settlement != null && IsSameCluster(town.Settlement, settlement))
            switch
            {
                <= 1 => 0.36f,
                2 => 0.2f,
                _ => 0f
            };

        float fortificationWeight = settlement.IsTown ? 0.18f : settlement.IsCastle ? 0.12f : 0f;
        return Math.Min(1f, lowFiefPressure + (siegePressure * 0.22f) + localClusterPressure + fortificationWeight);
    }

    private Settlement? GetCurrentObjective(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        return _objectiveSettlementByPair.TryGetValue(key, out string? settlementId)
            ? GetSettlementById(settlementId)
            : null;
    }

    private Settlement? GetLastCompletedObjective(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        return _lastCompletedObjectiveByPair.TryGetValue(key, out string? settlementId)
            ? GetSettlementById(settlementId)
            : null;
    }

    private float GetCurrentSectorConsolidationFactor(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.IsAtWarWith(enemy))
        {
            return 0f;
        }

        Settlement? lastCompletedObjective = GetLastCompletedObjective(kingdom, enemy);
        if (lastCompletedObjective == null || lastCompletedObjective.MapFaction != kingdom)
        {
            return 0f;
        }

        int nearbyEnemyFiefs = enemy.Fiefs.Count(town =>
            town?.Settlement != null &&
            IsSameCluster(lastCompletedObjective, town.Settlement));

        Settlement? currentObjective = GetCurrentObjective(kingdom, enemy);
        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        float activeFollowUp = 0f;
        if (currentObjective != null && IsSameCluster(lastCompletedObjective, currentObjective))
        {
            activeFollowUp += 0.35f;
        }

        if (frontlineAnchor != null && IsSameCluster(lastCompletedObjective, frontlineAnchor))
        {
            activeFollowUp += 0.2f;
        }

        float remainingEnemyPressure = Math.Min(1f, nearbyEnemyFiefs / 2.5f);
        float operationalPressure = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.3f,
            RFWarOperationalState.Advance => 0.18f,
            RFWarOperationalState.Exploit => 0.16f,
            _ => 0f
        };

        return Math.Min(1f, remainingEnemyPressure * 0.55f + activeFollowUp + operationalPressure);
    }

    private float GetCurrentObjectiveCommitmentFactor(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.IsAtWarWith(enemy))
        {
            return 0f;
        }

        Settlement? currentObjective = GetCurrentObjective(kingdom, enemy);
        if (currentObjective == null || currentObjective.MapFaction != enemy)
        {
            return 0f;
        }

        float holdFactor = 0f;
        string key = GetPairKey(kingdom, enemy);
        if (_holdUntilByPair.TryGetValue(key, out float holdUntil))
        {
            float remaining = Math.Max(0f, holdUntil - GetCurrentDay());
            float window = currentObjective.IsTown ? 7.4f : currentObjective.IsCastle ? 6f : 4.5f;
            holdFactor = Math.Min(1f, remaining / window);
        }

        float frontlineAlignment = 0f;
        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        if (frontlineAnchor != null)
        {
            frontlineAlignment = frontlineAnchor == currentObjective ? 1f : IsSameCluster(frontlineAnchor, currentObjective) ? 0.65f : 0f;
        }

        float momentum = Math.Max(0f, RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, currentObjective));
        float heat = Math.Min(1f, RFWarStrategicMemoryBehavior.GetSettlementHeat(kingdom, currentObjective) / 2.4f);
        float phaseFactor = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.PressTown => 0.2f,
            RFWarCampaignPhase.PressCastle => 0.16f,
            RFWarCampaignPhase.StripSupport => 0.12f,
            RFWarCampaignPhase.BreakFront => 0.08f,
            _ => 0.04f
        };

        float value = 0.16f
            + (holdFactor * 0.28f)
            + (frontlineAlignment * 0.28f)
            + (momentum * 0.18f)
            + (heat * 0.12f)
            + phaseFactor;
        return Math.Min(1f, Math.Max(0f, value));
    }

    private static Settlement? GetSettlementById(string? settlementId)
    {
        if (string.IsNullOrWhiteSpace(settlementId))
        {
            return null;
        }

        return Settlement.All.FirstOrDefault(settlement => settlement.StringId == settlementId);
    }

    private void PruneInactivePairs()
    {
        List<string> toRemove = new();
        foreach (string key in _objectiveSettlementByPair.Keys)
        {
            if (!IsPairStillActive(key))
            {
                toRemove.Add(key);
            }
        }

        foreach (string key in toRemove)
        {
            _objectiveSettlementByPair.Remove(key);
            _lastCompletedObjectiveByPair.Remove(key);
            _holdUntilByPair.Remove(key);
        }
    }

    private void RemovePair(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        _objectiveSettlementByPair.Remove(key);
        _lastCompletedObjectiveByPair.Remove(key);
        _holdUntilByPair.Remove(key);
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

    private static bool IsSameCluster(Settlement left, Settlement right)
    {
        return left.GatePosition.DistanceSquared(right.GatePosition) <= ClusterDistanceSquared;
    }

    private static bool IsFrontierContact(Kingdom kingdom, Settlement settlement)
    {
        foreach (Town ownTown in kingdom.Fiefs)
        {
            if (ownTown?.Settlement == null)
            {
                continue;
            }

            if (ownTown.Settlement.GatePosition.DistanceSquared(settlement.GatePosition) <= ClusterDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameStrategicCulture(string? attackerCulture, string? settlementCulture)
    {
        if (string.IsNullOrWhiteSpace(attackerCulture) || string.IsNullOrWhiteSpace(settlementCulture))
        {
            return false;
        }

        if (string.Equals(attackerCulture, settlementCulture, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsImperialCulture(attackerCulture) && IsImperialCulture(settlementCulture);
    }

    private static bool IsImperialCulture(string? cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return false;
        }

        string value = cultureId!;
        return value.IndexOf("empire", StringComparison.OrdinalIgnoreCase) >= 0;
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

    private static float GetHoldDuration(Kingdom kingdom, Kingdom enemy, Settlement settlement)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        float duration = settlement.IsTown ? 6.4f : settlement.IsCastle ? 5f : 3.8f;
        duration += RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy) switch
        {
            RFWarCampaignPhase.PressTown => 1.55f,
            RFWarCampaignPhase.PressCastle => 1.15f,
            RFWarCampaignPhase.StripSupport => 0.45f,
            RFWarCampaignPhase.BreakFront => 0.35f,
            RFWarCampaignPhase.DeepStrike => Math.Max(0f, profile.DeepStrikeBias) * 0.35f,
            _ => 0f
        };
        duration += Math.Max(0f, profile.SiegePatience) * (settlement.IsVillage ? 0.2f : 1f);
        return duration;
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
    }

    private static float ClampSigned(float value)
    {
        return Math.Max(-1f, Math.Min(1f, value));
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
