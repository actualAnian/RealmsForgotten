using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RF_warsystem.Diagnostics;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Behaviors;

public sealed class RFWarFrontlineBehavior : CampaignBehaviorBase
{
    private const float FrontClusterDistanceSquared = 32400f;

    private readonly Dictionary<string, string> _frontAnchorByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _holdUntilByPair = new(StringComparer.Ordinal);

    internal static RFWarFrontlineBehavior? Instance { get; private set; }

    public RFWarFrontlineBehavior()
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
        string anchorData = string.Empty;
        string holdData = string.Empty;

        if (!dataStore.IsLoading)
        {
            anchorData = SerializeStringDictionary(_frontAnchorByPair);
            holdData = SerializeFloatDictionary(_holdUntilByPair);
        }

        dataStore.SyncData("RFWarSystem_FrontlineAnchors", ref anchorData);
        dataStore.SyncData("RFWarSystem_FrontlineHoldUntil", ref holdData);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeStringDictionary(anchorData, _frontAnchorByPair);
        DeserializeFloatDictionary(holdData, _holdUntilByPair);
    }

    internal static Settlement? GetAnchorSettlement(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentAnchor(kingdom, enemy);
    }

    internal static float GetFrontCommitmentFactor(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentFrontCommitmentFactor(kingdom, enemy) ?? 0f;
    }

    internal static float GetTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        return Instance?.GetFrontTargetFactor(kingdom, targetSettlement, missionType) ?? 0f;
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

                UpdateFront(kingdom, enemy);
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

        ForceFront(kingdom1, kingdom2, "war_declared");
        ForceFront(kingdom2, kingdom1, "war_declared");
    }

    private void UpdateFront(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        float now = GetCurrentDay();
        if (!_holdUntilByPair.TryGetValue(key, out float holdUntil) || now >= holdUntil || ShouldBreakHold(kingdom, enemy))
        {
            ForceFront(kingdom, enemy, "daily");
        }
    }

    private void ForceFront(Kingdom kingdom, Kingdom enemy, string source)
    {
        string key = GetPairKey(kingdom, enemy);
        Settlement? previous = GetCurrentAnchor(kingdom, enemy);
        Settlement? next = SelectBestFrontAnchor(kingdom, enemy);

        if (next == null)
        {
            RemovePair(kingdom, enemy);
            return;
        }

        _frontAnchorByPair[key] = next.StringId;
        _holdUntilByPair[key] = GetCurrentDay() + GetHoldDuration(kingdom, enemy, next);

        if (previous == null || previous.StringId != next.StringId)
        {
            RFWarSystemTraceLog.FrontChanged(kingdom, enemy, previous, next, source);
        }
    }

    private float GetFrontTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        if (targetSettlement == null)
        {
            return 0f;
        }

        Kingdom? enemy = missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender
            ? targetSettlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction as Kingdom
                ?? targetSettlement.LastAttackerParty?.MapFaction as Kingdom
                ?? RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom)
            : targetSettlement.MapFaction as Kingdom;

        if (enemy == null)
        {
            return 0f;
        }

        Settlement? anchor = GetCurrentAnchor(kingdom, enemy);
        if (anchor == null)
        {
            return 0f;
        }

        bool exactAnchor = anchor == targetSettlement;
        bool sameCluster = anchor.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= FrontClusterDistanceSquared;
        bool homelandDefense = RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) == RFWarTheaterFocusMode.HomelandDefense;

        if (missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender)
        {
            if (!homelandDefense)
            {
                return 0f;
            }

            if (exactAnchor)
            {
                return 0.72f;
            }

            return sameCluster ? 0.38f : -0.08f;
        }

        if (homelandDefense)
        {
            return 0f;
        }

        if (exactAnchor)
        {
            return 0.64f;
        }

        if (sameCluster)
        {
            return 0.3f;
        }

        return -0.14f;
    }

    private Settlement? GetCurrentAnchor(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        if (!_frontAnchorByPair.TryGetValue(key, out string? settlementId) || string.IsNullOrWhiteSpace(settlementId))
        {
            return null;
        }

        // Settlement.Find is an O(1) MBObjectManager lookup. This getter sits
        // inside the army target-score hot path the vanilla AI calls tens of
        // thousands of times per game hour — a linear Settlement.All scan here
        // was the single biggest map-stutter source in the mod.
        return Settlement.Find(settlementId);
    }

    private static Settlement? SelectBestFrontAnchor(Kingdom kingdom, Kingdom enemy)
    {
        bool homelandDefense = RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) == RFWarTheaterFocusMode.HomelandDefense;
        IEnumerable<Settlement> candidates = homelandDefense
            ? kingdom.Fiefs.Select(town => town?.Settlement).OfType<Settlement>()
            : enemy.Fiefs.Select(town => town?.Settlement).OfType<Settlement>();

        return candidates
            .OrderByDescending(settlement => GetFrontAnchorScore(kingdom, enemy, settlement, homelandDefense))
            .FirstOrDefault();
    }

    private static float GetFrontAnchorScore(Kingdom kingdom, Kingdom enemy, Settlement settlement, bool homelandDefense)
    {
        float clusterSupport = GetClusterSupportScore(kingdom, enemy, settlement, homelandDefense);
        float sectorPriority = Math.Max(0f, RFWarFrontEvaluator.GetFrontSectorPriority(kingdom, enemy, settlement, homelandDefense));
        float phasePriority = RFWarFrontEvaluator.GetPhaseAnchorPriority(kingdom, enemy, settlement, homelandDefense);
        float softness = GetSettlementSoftness(settlement);
        float heat = Math.Min(1f, RFWarStrategicMemoryBehavior.GetSettlementHeat(kingdom, settlement) / 2.2f);
        float frontMomentum = RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, settlement);
        Settlement? currentObjective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);

        if (homelandDefense)
        {
            float score = settlement.IsUnderSiege ? 3.2f : 0f;
            if (settlement.LastAttackerParty?.MapFaction == enemy)
            {
                score += 1.8f;
            }

            score += IsFrontierSettlement(kingdom, enemy, settlement) ? 1.1f : -0.1f;
            score += clusterSupport * 1.35f;
            score += sectorPriority * 1.2f;
            score += Math.Max(0f, phasePriority) * 1.25f;
            score += softness * 0.85f;
            score += heat * 0.6f;
            score += Math.Max(0f, -frontMomentum) * 1.15f;
            score += settlement.IsTown ? 0.75f : 0.35f;
            return score;
        }

        float offensiveScore = settlement.IsTown ? 1.2f : settlement.IsCastle ? 0.9f : 0.2f;
        offensiveScore += IsFrontierContact(kingdom, settlement) ? 1f : -0.4f;
        offensiveScore += clusterSupport * 1.45f;
        offensiveScore += sectorPriority * 1.25f;
        offensiveScore += phasePriority * 1.4f;
        offensiveScore += softness * 1.05f;
        offensiveScore += heat * 0.45f;
        offensiveScore += Math.Max(0f, frontMomentum) * 1.2f;
        offensiveScore += Math.Max(0f, RFWarExternalFrontContext.GetSacredTargetFactor(kingdom, enemy, settlement)) * 1.5f;
        offensiveScore += Math.Max(0f, RFWarStrategicIntent.GetTargetIntentFactor(kingdom, settlement, TaleWorlds.CampaignSystem.Army.ArmyTypes.Besieger)) * 0.7f;
        if (currentObjective != null)
        {
            if (settlement == currentObjective)
            {
                offensiveScore += 1.15f;
            }
            else if (IsSameCluster(settlement, currentObjective))
            {
                offensiveScore += 0.55f;
            }
        }

        return offensiveScore;
    }

    private static float GetClusterSupportScore(Kingdom kingdom, Kingdom enemy, Settlement anchor, bool homelandDefense)
    {
        IEnumerable<Town> towns = homelandDefense ? kingdom.Fiefs : enemy.Fiefs;
        float score = 0f;

        foreach (Town town in towns)
        {
            Settlement? settlement = town?.Settlement;
            if (settlement == null)
            {
                continue;
            }

            if (settlement.GatePosition.DistanceSquared(anchor.GatePosition) > FrontClusterDistanceSquared)
            {
                continue;
            }

            float value = settlement.IsTown ? 0.95f : settlement.IsCastle ? 0.7f : 0.3f;
            value += GetSettlementSoftness(settlement) * 0.4f;
            if (homelandDefense)
            {
                value += settlement.IsUnderSiege ? 1f : 0f;
                value += settlement.LastAttackerParty?.MapFaction == enemy ? 0.65f : 0f;
            }
            else
            {
                value += settlement.IsVillage ? 0.1f : 0f;
                value += Math.Max(0f, RFWarExternalFrontContext.GetSacredTargetFactor(kingdom, enemy, settlement)) * 0.45f;
            }

            score += value;
        }

        return score;
    }

    private bool ShouldBreakHold(Kingdom kingdom, Kingdom enemy)
    {
        Settlement? current = GetCurrentAnchor(kingdom, enemy);
        if (current == null)
        {
            return true;
        }

        bool homelandDefense = RFWarTheaterBehavior.GetFocusMode(kingdom, enemy) == RFWarTheaterFocusMode.HomelandDefense;
        if (homelandDefense)
        {
            float phasePriority = RFWarFrontEvaluator.GetPhaseAnchorPriority(kingdom, enemy, current, homelandDefense: true);
            if (current.MapFaction != kingdom)
            {
                return true;
            }

            if (current.IsUnderSiege || current.LastAttackerParty?.MapFaction == enemy)
            {
                return false;
            }

            if (phasePriority >= 0.45f)
            {
                return false;
            }

            return !IsFrontierSettlement(kingdom, enemy, current)
                && GetClusterSupportScore(kingdom, enemy, current, homelandDefense: true) < 0.95f;
        }

        float offensivePhasePriority = RFWarFrontEvaluator.GetPhaseAnchorPriority(kingdom, enemy, current, homelandDefense: false);
        if (current.MapFaction != enemy)
        {
            return true;
        }

        float clusterSupport = GetClusterSupportScore(kingdom, enemy, current, homelandDefense: false);
        float sacredTarget = Math.Max(0f, RFWarExternalFrontContext.GetSacredTargetFactor(kingdom, enemy, current));
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        bool objectiveAligned = objective != null && (objective == current || IsSameCluster(objective, current));
        if (offensivePhasePriority >= 0.42f || objectiveAligned)
        {
            return false;
        }

        return !IsFrontierContact(kingdom, current)
            && !current.IsTown
            && clusterSupport < 1.1f
            && sacredTarget < 0.2f
            && !objectiveAligned;
    }

    private float GetCurrentFrontCommitmentFactor(Kingdom kingdom, Kingdom enemy)
    {
        if (!kingdom.IsAtWarWith(enemy))
        {
            return 0f;
        }

        Settlement? current = GetCurrentAnchor(kingdom, enemy);
        if (current == null)
        {
            return 0f;
        }

        bool homelandDefense = current.MapFaction == kingdom;
        float holdFactor = 0f;
        string key = GetPairKey(kingdom, enemy);
        if (_holdUntilByPair.TryGetValue(key, out float holdUntil))
        {
            float remaining = Math.Max(0f, holdUntil - GetCurrentDay());
            float window = homelandDefense ? 4f : current.IsTown ? 6.25f : 5f;
            holdFactor = Math.Min(1f, remaining / window);
        }

        float clusterSupport = GetClusterSupportScore(kingdom, enemy, current, homelandDefense);
        float supportFactor = Math.Min(1f, clusterSupport / (homelandDefense ? 3.2f : 3.8f));
        float sectorPriority = Math.Max(0f, Math.Min(1f, RFWarFrontEvaluator.GetFrontSectorPriority(kingdom, enemy, current, homelandDefense)));
        float frontMomentum = RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, current);
        float momentumFactor = homelandDefense ? Math.Max(0f, -frontMomentum) : Math.Max(0f, frontMomentum);
        float objectiveAlignment = 0f;
        Settlement? objective = RFWarObjectiveChainBehavior.GetObjectiveSettlement(kingdom, enemy);
        if (objective != null)
        {
            objectiveAlignment = objective == current ? 1f : IsSameCluster(objective, current) ? 0.6f : 0f;
        }

        float stateFactor = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) switch
        {
            RFWarOperationalState.Besiege => 0.2f,
            RFWarOperationalState.Advance => 0.14f,
            RFWarOperationalState.Defend => 0.18f,
            RFWarOperationalState.Exploit => 0.1f,
            _ => 0.04f
        };

        float value = 0.18f
            + (holdFactor * 0.24f)
            + (supportFactor * 0.26f)
            + (sectorPriority * 0.16f)
            + (momentumFactor * 0.12f)
            + (objectiveAlignment * 0.16f)
            + stateFactor;
        return Math.Min(1f, Math.Max(0f, value));
    }

    private void PruneInactivePairs()
    {
        if (_frontAnchorByPair.Count == 0)
        {
            return;
        }

        List<string> toRemove = new();
        foreach (string key in _frontAnchorByPair.Keys)
        {
            if (!IsPairStillActive(key))
            {
                toRemove.Add(key);
            }
        }

        foreach (string key in toRemove)
        {
            _frontAnchorByPair.Remove(key);
            _holdUntilByPair.Remove(key);
        }
    }

    private void RemovePair(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        _frontAnchorByPair.Remove(key);
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

    private static bool IsFrontierSettlement(Kingdom kingdom, Kingdom enemy, Settlement settlement)
    {
        foreach (Town enemyTown in enemy.Fiefs)
        {
            if (enemyTown?.Settlement == null)
            {
                continue;
            }

            if (settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition) <= FrontClusterDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFrontierContact(Kingdom kingdom, Settlement settlement)
    {
        foreach (Town ownTown in kingdom.Fiefs)
        {
            if (ownTown?.Settlement == null)
            {
                continue;
            }

            if (ownTown.Settlement.GatePosition.DistanceSquared(settlement.GatePosition) <= FrontClusterDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameCluster(Settlement left, Settlement right)
    {
        return left != null
            && right != null
            && left.GatePosition.DistanceSquared(right.GatePosition) <= FrontClusterDistanceSquared;
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

    private static float GetHoldDuration(Kingdom kingdom, Kingdom enemy, Settlement anchor)
    {
        bool homelandDefense = anchor.MapFaction == kingdom;
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(kingdom, enemy);
        if (homelandDefense)
        {
            float baseDuration = anchor.IsUnderSiege ? 2.8f : 3.8f;
            baseDuration += Math.Max(0f, profile.FrontierParanoia) * 0.9f;
            baseDuration += phase == RFWarCampaignPhase.Stabilize ? 0.4f : 0f;
            return baseDuration;
        }

        float duration = anchor.IsTown ? 5.2f : 4.1f;
        duration += phase switch
        {
            RFWarCampaignPhase.PressTown => 1.35f,
            RFWarCampaignPhase.PressCastle => 1.05f,
            RFWarCampaignPhase.BreakFront => 0.55f,
            RFWarCampaignPhase.StripSupport => 0.4f,
            RFWarCampaignPhase.DeepStrike => 0.2f,
            _ => 0f
        };
        duration += Math.Max(0f, profile.SiegePatience) * 1f;
        return duration;
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
