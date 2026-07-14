using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Behaviors;

public sealed class RFWarTheaterBehavior : CampaignBehaviorBase
{
    private const float BorderDistanceSquared = 32400f;

    private readonly Dictionary<string, string> _focusModeByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _anchorSettlementByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _holdUntilByPair = new(StringComparer.Ordinal);

    internal static RFWarTheaterBehavior? Instance { get; private set; }

    public RFWarTheaterBehavior()
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
        string focusModeData = string.Empty;
        string anchorSettlementData = string.Empty;
        string holdData = string.Empty;

        if (!dataStore.IsLoading)
        {
            focusModeData = SerializeStringDictionary(_focusModeByPair);
            anchorSettlementData = SerializeStringDictionary(_anchorSettlementByPair);
            holdData = SerializeFloatDictionary(_holdUntilByPair);
        }

        dataStore.SyncData("RFWarSystem_TheaterFocusModes", ref focusModeData);
        dataStore.SyncData("RFWarSystem_TheaterAnchors", ref anchorSettlementData);
        dataStore.SyncData("RFWarSystem_TheaterHoldUntil", ref holdData);

        if (!dataStore.IsLoading)
        {
            return;
        }

        DeserializeStringDictionary(focusModeData, _focusModeByPair);
        DeserializeStringDictionary(anchorSettlementData, _anchorSettlementByPair);
        DeserializeFloatDictionary(holdData, _holdUntilByPair);
    }

    internal static float GetTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        return Instance?.GetTheaterTargetFactor(kingdom, targetSettlement, missionType) ?? 0f;
    }

    internal static RFWarTheaterFocusMode GetFocusMode(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentFocusMode(kingdom, enemy) ?? RFWarTheaterFocusMode.None;
    }

    internal static Settlement? GetAnchorSettlement(Kingdom kingdom, Kingdom enemy)
    {
        return Instance?.GetCurrentAnchor(kingdom, enemy);
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

                UpdateTheater(kingdom, enemy);
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

        ForceTheater(kingdom1, kingdom2);
        ForceTheater(kingdom2, kingdom1);
    }

    private void UpdateTheater(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        float now = GetCurrentDay();
        if (!_holdUntilByPair.TryGetValue(key, out float holdUntil) || now >= holdUntil || ShouldBreakHold(kingdom, enemy))
        {
            ForceTheater(kingdom, enemy);
        }
    }

    private void ForceTheater(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        (RFWarTheaterFocusMode mode, Settlement? anchor) = EvaluateTheater(kingdom, enemy);
        _focusModeByPair[key] = mode.ToString();
        if (anchor != null)
        {
            _anchorSettlementByPair[key] = anchor.StringId;
        }
        else
        {
            _anchorSettlementByPair.Remove(key);
        }

        _holdUntilByPair[key] = GetCurrentDay() + GetHoldDuration(mode);
    }

    private (RFWarTheaterFocusMode Mode, Settlement? Anchor) EvaluateTheater(Kingdom kingdom, Kingdom enemy)
    {
        float homeThreat = Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.2f);
        bool underDirectThreat = CountDirectlyThreatenedFiefs(kingdom, enemy) > 0;

        if (underDirectThreat || homeThreat >= 0.65f || RFWarOperationalRhythmBehavior.GetState(kingdom, enemy) == RFWarOperationalState.Defend)
        {
            return (RFWarTheaterFocusMode.HomelandDefense, GetBestDefensiveAnchor(kingdom, enemy));
        }

        return (RFWarTheaterFocusMode.OffensivePush, GetBestOffensiveAnchor(kingdom, enemy));
    }

    private float GetTheaterTargetFactor(Kingdom kingdom, Settlement targetSettlement, TaleWorlds.CampaignSystem.Army.ArmyTypes missionType)
    {
        if (targetSettlement == null)
        {
            return 0f;
        }

        Kingdom? enemy = missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender
            ? kingdom.FactionsAtWarWith.OfType<Kingdom>().OrderByDescending(x => x.CurrentTotalStrength).FirstOrDefault()
            : targetSettlement.MapFaction as Kingdom;

        if (enemy == null)
        {
            return 0f;
        }

        RFWarTheaterFocusMode mode = GetCurrentFocusMode(kingdom, enemy);
        Settlement? anchor = GetCurrentAnchor(kingdom, enemy);
        if (mode == RFWarTheaterFocusMode.None || anchor == null)
        {
            return 0f;
        }

        bool exactAnchor = anchor == targetSettlement;
        bool sameCluster = anchor.GatePosition.DistanceSquared(targetSettlement.GatePosition) <= BorderDistanceSquared;

        if (mode == RFWarTheaterFocusMode.HomelandDefense)
        {
            if (missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender)
            {
                if (exactAnchor)
                {
                    return 0.8f;
                }

                return sameCluster ? 0.4f : -0.12f;
            }

            return exactAnchor ? -0.4f : sameCluster ? -0.22f : -0.08f;
        }

        if (missionType == TaleWorlds.CampaignSystem.Army.ArmyTypes.Defender)
        {
            return 0f;
        }

        if (exactAnchor)
        {
            return 0.72f;
        }

        if (sameCluster)
        {
            return 0.34f;
        }

        return -0.16f;
    }

    private RFWarTheaterFocusMode GetCurrentFocusMode(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        if (_focusModeByPair.TryGetValue(key, out string? rawMode) &&
            Enum.TryParse(rawMode, ignoreCase: true, out RFWarTheaterFocusMode mode))
        {
            return mode;
        }

        return RFWarTheaterFocusMode.None;
    }

    private Settlement? GetCurrentAnchor(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        if (!_anchorSettlementByPair.TryGetValue(key, out string? settlementId) || string.IsNullOrWhiteSpace(settlementId))
        {
            return null;
        }

        return Settlement.All.FirstOrDefault(settlement => settlement.StringId == settlementId);
    }

    private static Settlement? GetBestDefensiveAnchor(Kingdom kingdom, Kingdom enemy)
    {
        return kingdom.Fiefs
            .Select(town => town?.Settlement)
            .Where(settlement => settlement != null)
            .OrderByDescending(settlement => GetDefensiveAnchorScore(kingdom, enemy, settlement!))
            .FirstOrDefault();
    }

    private static Settlement? GetBestOffensiveAnchor(Kingdom kingdom, Kingdom enemy)
    {
        return enemy.Fiefs
            .Select(town => town?.Settlement)
            .Where(settlement => settlement != null)
            .OrderByDescending(settlement => GetOffensiveAnchorScore(kingdom, enemy, settlement!))
            .FirstOrDefault();
    }

    private static float GetDefensiveAnchorScore(Kingdom kingdom, Kingdom enemy, Settlement settlement)
    {
        float sectorPriority = Math.Max(0f, RFWarFrontEvaluator.GetFrontSectorPriority(kingdom, enemy, settlement, homelandDefense: true));
        float phasePriority = Math.Max(0f, RFWarFrontEvaluator.GetPhaseAnchorPriority(kingdom, enemy, settlement, homelandDefense: true));
        float score = settlement.IsUnderSiege ? 4f : 0f;
        if (settlement.LastAttackerParty?.MapFaction == enemy)
        {
            score += 2.2f;
        }

        if (IsFrontierSettlement(kingdom, settlement))
        {
            score += 1.4f;
        }

        score += settlement.IsTown ? 0.9f : 0.5f;
        score += GetSettlementSoftness(settlement) * 1.2f;
        score += sectorPriority * 1.1f;
        score += phasePriority * 1.2f;
        score += Math.Min(1f, RFWarStrategicMemoryBehavior.GetSettlementHeat(kingdom, settlement) / 2f);
        score += Math.Max(0f, -RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, settlement)) * 1.35f;
        return score;
    }

    private static float GetOffensiveAnchorScore(Kingdom kingdom, Kingdom enemy, Settlement settlement)
    {
        float sectorPriority = Math.Max(0f, RFWarFrontEvaluator.GetFrontSectorPriority(kingdom, enemy, settlement, homelandDefense: false));
        float phasePriority = RFWarFrontEvaluator.GetPhaseAnchorPriority(kingdom, enemy, settlement, homelandDefense: false);
        float intentPriority = Math.Max(0f, RFWarStrategicIntent.GetTargetIntentFactor(kingdom, settlement, TaleWorlds.CampaignSystem.Army.ArmyTypes.Besieger));
        float score = settlement.IsTown ? 1.4f : settlement.IsCastle ? 0.95f : 0.35f;
        score += GetSettlementSoftness(settlement) * 1.25f;
        score += IsFrontierContact(kingdom, settlement) ? 1f : -0.35f;
        score += sectorPriority * 1.2f;
        score += phasePriority * 1.45f;
        score += intentPriority * 0.8f;
        score += Math.Min(1f, RFWarStrategicMemoryBehavior.GetSettlementHeat(kingdom, settlement) / 2f) * 0.65f;
        score += Math.Max(0f, RFWarStrategicMemoryBehavior.GetMomentum(kingdom, enemy)) * 0.35f;
        score += Math.Max(0f, RFWarStrategicMemoryBehavior.GetFrontMomentum(kingdom, enemy, settlement)) * 1.2f;
        score += RFWarExternalFrontContext.GetSacredTargetFactor(kingdom, enemy, settlement) * 1.8f;
        score += RFWarExternalFrontContext.GetHolyWarPressure(kingdom, enemy) * (settlement.IsTown ? 0.35f : 0.12f);
        return score;
    }

    private bool ShouldBreakHold(Kingdom kingdom, Kingdom enemy)
    {
        RFWarTheaterFocusMode current = GetCurrentFocusMode(kingdom, enemy);
        int threatenedFiefs = CountDirectlyThreatenedFiefs(kingdom, enemy);
        RFWarOperationalState operationalState = RFWarOperationalRhythmBehavior.GetState(kingdom, enemy);

        if (current == RFWarTheaterFocusMode.OffensivePush && threatenedFiefs > 0)
        {
            return true;
        }

        if (current == RFWarTheaterFocusMode.HomelandDefense && threatenedFiefs == 0 && operationalState == RFWarOperationalState.Exploit)
        {
            return true;
        }

        return false;
    }

    private static int CountDirectlyThreatenedFiefs(Kingdom kingdom, Kingdom enemy)
    {
        return kingdom.Fiefs.Count(town =>
            town?.Settlement != null &&
            (town.Settlement.IsUnderSiege && town.Settlement.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction == enemy
             || town.Settlement.LastAttackerParty?.MapFaction == enemy));
    }

    private void PruneInactivePairs()
    {
        if (_focusModeByPair.Count == 0)
        {
            return;
        }

        List<string> toRemove = new();
        foreach (KeyValuePair<string, string> pair in _focusModeByPair)
        {
            if (!IsPairStillActive(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (string key in toRemove)
        {
            _focusModeByPair.Remove(key);
            _anchorSettlementByPair.Remove(key);
            _holdUntilByPair.Remove(key);
        }
    }

    private void RemovePair(Kingdom kingdom, Kingdom enemy)
    {
        string key = GetPairKey(kingdom, enemy);
        _focusModeByPair.Remove(key);
        _anchorSettlementByPair.Remove(key);
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

                if (settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition) <= BorderDistanceSquared)
                {
                    return true;
                }
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

            if (ownTown.Settlement.GatePosition.DistanceSquared(settlement.GatePosition) <= BorderDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static float GetHoldDuration(RFWarTheaterFocusMode mode)
    {
        return mode switch
        {
            RFWarTheaterFocusMode.HomelandDefense => 2.5f,
            RFWarTheaterFocusMode.OffensivePush => 4f,
            _ => 2f
        };
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
