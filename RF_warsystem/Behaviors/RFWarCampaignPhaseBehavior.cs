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

public sealed class RFWarCampaignPhaseBehavior : CampaignBehaviorBase
{
    private const float ClusterDistanceSquared = 32400f;

    private readonly Dictionary<string, string> _phaseByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _holdUntilByPair = new(StringComparer.Ordinal);

    internal static RFWarCampaignPhaseBehavior? Instance { get; private set; }

    public RFWarCampaignPhaseBehavior()
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
        string phaseData = string.Empty;
        string holdData = string.Empty;

        if (!dataStore.IsLoading)
        {
            phaseData = SerializeStringDictionary(_phaseByPair);
            holdData = SerializeFloatDictionary(_holdUntilByPair);
        }

        dataStore.SyncData("RFWarSystem_CampaignPhases", ref phaseData);
        dataStore.SyncData("RFWarSystem_CampaignPhaseHoldUntil", ref holdData);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeStringDictionary(phaseData, _phaseByPair);
        DeserializeFloatDictionary(holdData, _holdUntilByPair);
    }

    internal static RFWarCampaignPhase GetPhase(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentPhase(kingdom, enemy) ?? RFWarCampaignPhase.None;
    }

    internal static float GetTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        return Instance?.GetCampaignTargetFactor(kingdom, targetSettlement, missionType) ?? 0f;
    }

    internal static float GetObjectiveFactor(Kingdom kingdom, Kingdom enemy, Settlement settlement)
    {
        return Instance?.GetCampaignObjectiveFactor(kingdom, enemy, settlement) ?? 0f;
    }

    private void OnDailyTick()
    {
            using var _rfPerf = RF_warsystem.Diagnostics.RFPerfProbe.Measure("WarPhase");
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

                UpdatePhase(kingdom, enemy);
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

        ForcePhase(kingdom1, kingdom2, EvaluateDesiredPhase(kingdom1, kingdom2), "war_declared");
        ForcePhase(kingdom2, kingdom1, EvaluateDesiredPhase(kingdom2, kingdom1), "war_declared");
    }

    private void UpdatePhase(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        RFWarCampaignPhase desired = EvaluateDesiredPhase(kingdom, enemy);
        RFWarCampaignPhase current = GetCurrentPhase(kingdom, enemy);
        float now = GetCurrentDay();

        if (current == desired)
        {
            if (!_holdUntilByPair.ContainsKey(key))
            {
                _holdUntilByPair[key] = now + GetHoldDuration(desired);
            }

            return;
        }

        if (!_holdUntilByPair.TryGetValue(key, out float holdUntil) || now >= holdUntil || CanBreakHoldEarly(kingdom, enemy, current, desired))
        {
            ForcePhase(kingdom, enemy, desired, "daily");
        }
    }

    private void ForcePhase(Kingdom kingdom, Kingdom enemy, RFWarCampaignPhase phase, string source)
    {
        string key = GetPairKey(kingdom, enemy);
        RFWarCampaignPhase previous = GetCurrentPhase(kingdom, enemy);
        _phaseByPair[key] = phase.ToString();
        _holdUntilByPair[key] = GetCurrentDay() + GetHoldDuration(phase);

        if (previous != phase)
        {
            Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
            Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
            RFWarSystemTraceLog.CampaignPhaseChanged(
                kingdom,
                enemy,
                previous.ToString(),
                phase.ToString(),
                source,
                ("frontline", frontlineAnchor != null ? 1f : 0f),
                ("objective", objective != null ? 1f : 0f),
                ("momentum", RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy)),
                ("homeThreat", GetHomeThreatRatio(kingdom, enemy)));
        }
    }

    private RFWarCampaignPhase GetCurrentPhase(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        if (_phaseByPair.TryGetValue(key, out string? rawPhase) &&
            Enum.TryParse(rawPhase, ignoreCase: true, out RFWarCampaignPhase phase))
        {
            return phase;
        }

        return RFWarCampaignPhase.None;
    }

    private static RFWarCampaignPhase EvaluateDesiredPhase(Kingdom kingdom, Kingdom enemy)
    {
        if (RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) != RFWarTheaterFocusMode.OffensivePush)
        {
            return RFWarCampaignPhase.Stabilize;
        }

        RFWarOperationalState state = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy);
        float momentum = RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy);
        float homeThreat = GetHomeThreatRatio(kingdom, enemy);

        if (state == RFWarOperationalState.Defend || state == RFWarOperationalState.Regroup || homeThreat >= 0.35f)
        {
            return RFWarCampaignPhase.Stabilize;
        }

        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        bool frontierStrongholdAlive = HasEnemyFrontierStronghold(kingdom, enemy);

        if (objective == null)
        {
            return frontierStrongholdAlive ? RFWarCampaignPhase.BreakFront : RFWarCampaignPhase.DeepStrike;
        }

        if (objective.MapFaction != enemy)
        {
            return frontierStrongholdAlive ? RFWarCampaignPhase.BreakFront : RFWarCampaignPhase.DeepStrike;
        }

        bool objectiveFrontier = IsFrontierTarget(kingdom, objective);
        bool hasSupportVillages = HasEnemySupportVillages(enemy, objective);

        if (frontierStrongholdAlive && !objectiveFrontier && state != RFWarOperationalState.Exploit)
        {
            return RFWarCampaignPhase.BreakFront;
        }

        if (hasSupportVillages && objective.IsFortification && !IsSettlementSoft(objective))
        {
            return RFWarCampaignPhase.StripSupport;
        }

        if (objective.IsTown)
        {
            return RFWarCampaignPhase.PressTown;
        }

        if (objective.IsCastle)
        {
            return RFWarCampaignPhase.PressCastle;
        }

        if (!objectiveFrontier && (state == RFWarOperationalState.Exploit || momentum >= 0.35f))
        {
            return RFWarCampaignPhase.DeepStrike;
        }

        if (objective.IsVillage && frontlineAnchor != null && !IsSameCluster(frontlineAnchor, objective))
        {
            return RFWarCampaignPhase.DeepStrike;
        }

        return RFWarCampaignPhase.StripSupport;
    }

    private bool CanBreakHoldEarly(Kingdom kingdom, Kingdom enemy, RFWarCampaignPhase current, RFWarCampaignPhase desired)
    {
        RFWarOperationalState state = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy);

        if (desired == RFWarCampaignPhase.Stabilize && GetHomeThreatRatio(kingdom, enemy) >= 0.5f)
        {
            return true;
        }

        if (state == RFWarOperationalState.Defend || state == RFWarOperationalState.Regroup)
        {
            return desired == RFWarCampaignPhase.Stabilize;
        }

        if (state == RFWarOperationalState.Besiege && (desired == RFWarCampaignPhase.PressCastle || desired == RFWarCampaignPhase.PressTown))
        {
            return true;
        }

        if (state == RFWarOperationalState.Exploit && desired == RFWarCampaignPhase.DeepStrike)
        {
            return true;
        }

        if (current == RFWarCampaignPhase.DeepStrike && desired == RFWarCampaignPhase.BreakFront)
        {
            return true;
        }

        if (current == RFWarCampaignPhase.StripSupport && (desired == RFWarCampaignPhase.PressCastle || desired == RFWarCampaignPhase.PressTown))
        {
            return !HasEnemySupportVillages(enemy, RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy));
        }

        return false;
    }

    private float GetCampaignTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        if (targetSettlement == null)
        {
            return 0f;
        }

        Kingdom? enemy = missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender
            ? targetSettlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction as Kingdom
                ?? targetSettlement.LastAttackerParty?.MapFaction as Kingdom
            : targetSettlement.MapFaction as Kingdom;

        if (enemy == null)
        {
            return 0f;
        }

        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        RFWarCampaignPhase phase = GetCurrentPhase(kingdom, enemy);
        bool isVillage = targetSettlement.IsVillage;
        bool isFrontier = IsFrontierTarget(kingdom, targetSettlement);
        bool sameClusterAsObjective = objective != null && IsSameCluster(objective, targetSettlement);
        bool sameClusterAsFrontline = frontlineAnchor != null && IsSameCluster(frontlineAnchor, targetSettlement);
        bool boundToObjective = objective != null && isVillage && targetSettlement.Village.Bound == objective;
        bool boundToFrontline = frontlineAnchor != null && isVillage && targetSettlement.Village.Bound == frontlineAnchor;

        if (missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender)
        {
            return phase == RFWarCampaignPhase.Stabilize && isFrontier ? 0.32f : 0f;
        }

        float factor = phase switch
        {
            RFWarCampaignPhase.BreakFront => targetSettlement.IsFortification && isFrontier
                ? 0.72f + (sameClusterAsFrontline ? 0.12f : 0f)
                : isVillage ? -0.32f : -0.18f,
            RFWarCampaignPhase.StripSupport => boundToObjective || boundToFrontline
                ? 0.82f
                : isVillage && (sameClusterAsObjective || sameClusterAsFrontline)
                    ? 0.46f
                    : targetSettlement.IsFortification ? -0.14f : -0.08f,
            RFWarCampaignPhase.PressCastle => targetSettlement.IsCastle
                ? (objective == targetSettlement ? 0.9f : sameClusterAsObjective ? 0.52f : 0.2f)
                : boundToObjective ? 0.24f : -0.12f,
            RFWarCampaignPhase.PressTown => targetSettlement.IsTown
                ? (objective == targetSettlement ? 0.95f : sameClusterAsObjective ? 0.48f : 0.18f)
                : boundToObjective ? 0.34f : targetSettlement.IsCastle ? 0.08f : -0.1f,
            RFWarCampaignPhase.DeepStrike => !isFrontier
                ? (isVillage ? 0.72f : targetSettlement.IsFortification ? 0.3f : 0.18f) + (sameClusterAsObjective ? 0.12f : 0f)
                : targetSettlement.IsFortification ? -0.22f : -0.12f,
            RFWarCampaignPhase.Stabilize => targetSettlement.IsFortification && isFrontier
                ? 0.2f
                : isVillage ? -0.38f : -0.24f,
            _ => 0f
        };

        return ClampSigned(factor);
    }

    private float GetCampaignObjectiveFactor(Kingdom kingdom, Kingdom enemy, Settlement settlement)
    {
        RFWarCampaignPhase phase = GetCurrentPhase(kingdom, enemy);
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        Settlement? frontlineAnchor = RFWarFrontlineBehavior.GetAnchorSettlement(kingdom, enemy);
        bool isVillage = settlement.IsVillage;
        bool isFrontier = IsFrontierTarget(kingdom, settlement);
        bool sameClusterAsObjective = objective != null && IsSameCluster(objective, settlement);
        bool sameClusterAsFrontline = frontlineAnchor != null && IsSameCluster(frontlineAnchor, settlement);
        bool boundToObjective = objective != null && isVillage && settlement.Village.Bound == objective;
        bool boundToFrontline = frontlineAnchor != null && isVillage && settlement.Village.Bound == frontlineAnchor;

        float factor = phase switch
        {
            RFWarCampaignPhase.BreakFront => settlement.IsFortification && isFrontier
                ? 0.95f
                : isVillage ? -0.42f : -0.16f,
            RFWarCampaignPhase.StripSupport => boundToObjective || boundToFrontline
                ? 0.88f
                : isVillage && (sameClusterAsObjective || sameClusterAsFrontline)
                    ? 0.52f
                    : settlement.IsFortification ? -0.18f : -0.1f,
            RFWarCampaignPhase.PressCastle => settlement.IsCastle
                ? 0.82f + (sameClusterAsFrontline ? 0.14f : 0f)
                : boundToObjective ? 0.22f : settlement.IsTown ? -0.18f : -0.08f,
            RFWarCampaignPhase.PressTown => settlement.IsTown
                ? 0.86f + (sameClusterAsFrontline ? 0.1f : 0f)
                : boundToObjective ? 0.28f : settlement.IsCastle ? 0.12f : -0.08f,
            RFWarCampaignPhase.DeepStrike => !isFrontier
                ? (isVillage ? 0.74f : 0.24f)
                : settlement.IsFortification ? -0.26f : -0.12f,
            RFWarCampaignPhase.Stabilize => settlement.IsFortification && isFrontier
                ? 0.24f
                : isVillage ? -0.4f : -0.3f,
            _ => 0f
        };

        return ClampSigned(factor);
    }

    private void PruneInactivePairs()
    {
        if (_phaseByPair.Count == 0)
        {
            return;
        }

        List<string> toRemove = new();
        foreach (string key in _phaseByPair.Keys)
        {
            if (!IsPairStillActive(key))
            {
                toRemove.Add(key);
            }
        }

        foreach (string key in toRemove)
        {
            _phaseByPair.Remove(key);
            _holdUntilByPair.Remove(key);
        }
    }

    private void RemovePair(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        _phaseByPair.Remove(key);
        _holdUntilByPair.Remove(key);
    }

    private static bool HasEnemyFrontierStronghold(Kingdom kingdom, Kingdom enemy)
    {
        return enemy.Fiefs.Any(town => town?.Settlement != null && town.Settlement.IsFortification && IsFrontierTarget(kingdom, town.Settlement));
    }

    private static bool HasEnemySupportVillages(Kingdom enemy, Settlement? settlement)
    {
        if (enemy == null || settlement == null || !settlement.IsFortification)
        {
            return false;
        }

        return enemy.Fiefs.Any(town =>
            town?.Settlement != null &&
            town.Settlement.IsVillage &&
            town.Settlement.Village.Bound == settlement &&
            town.Settlement.MapFaction == enemy);
    }

    private static bool IsSettlementSoft(Settlement settlement)
    {
        return GetSettlementSoftness(settlement) >= 0.45f;
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

    private static float GetHomeThreatRatio(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.Fiefs.Any())
        {
            return 0f;
        }

        int threatened = 0;
        foreach (Town town in kingdom.Fiefs)
        {
            if (town?.Settlement == null)
            {
                continue;
            }

            if (town.Settlement.IsUnderSiege && town.Settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy)
            {
                threatened++;
                continue;
            }

            MobileParty? attacker = town.Settlement.LastAttackerParty;
            if (attacker != null && attacker.IsActive && attacker.MapFaction == enemy)
            {
                threatened++;
            }
        }

        return threatened / (float)Math.Max(1, kingdom.Fiefs.Count);
    }

    private static bool IsFrontierTarget(Kingdom kingdom, Settlement targetSettlement)
    {
        foreach (Town ownTown in kingdom.Fiefs)
        {
            if (ownTown?.Settlement == null)
            {
                continue;
            }

            if (ownTown.Settlement.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= ClusterDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameCluster(Settlement left, Settlement right)
    {
        return left.GatePosition.DistanceSquared(right.GatePosition) <= ClusterDistanceSquared;
    }

    private static float GetHoldDuration(RFWarCampaignPhase phase)
    {
        float duration = phase switch
        {
            RFWarCampaignPhase.BreakFront => 3.4f,
            RFWarCampaignPhase.StripSupport => 2.8f,
            RFWarCampaignPhase.PressCastle => 3.6f,
            RFWarCampaignPhase.PressTown => 4.2f,
            RFWarCampaignPhase.DeepStrike => 2.4f,
            RFWarCampaignPhase.Stabilize => 3f,
            _ => 2f
        };

        return duration;
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

    private static float ClampSigned(float value)
    {
        return Math.Max(-1f, Math.Min(1f, value));
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
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
