using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Logic;

internal static class RFWarExternalFrontContext
{
    private static readonly Dictionary<string, FrontEntry> TargetPriorityByKey = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, FrontEntry> EnemyPriorityByKey = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, FrontEntry> HolyWarPressureByPair = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, FrontEntry> CollectiveDefensePressureByPair = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, FrontEntry> AlignmentWarPressureByPair = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SacredTargetEntry> SacredTargetByPair = new(StringComparer.Ordinal);

    // True only while the quest-driven global alignment war is active (pushed by
    // RealmsForgotten AlignmentWarBehavior via RFWarExternalIntentApi). While
    // false, the war director must NOT treat good/evil culture sides as strategic
    // blocs — otherwise the alignment war effectively starts on day 1.
    internal static bool AlignmentDoctrineActive;

    private const float FrontClusterDistanceSquared = 32400f;

    /// <summary>
    /// Clears all cross-campaign static state. The dictionaries are keyed by
    /// kingdom StringId, which is identical across saves, so without this a
    /// second campaign in the same session inherits the first's holy-war /
    /// collective-defense / sacred-target pressures. Call on new game / load.
    /// </summary>
    public static void Reset()
    {
        TargetPriorityByKey.Clear();
        EnemyPriorityByKey.Clear();
        HolyWarPressureByPair.Clear();
        CollectiveDefensePressureByPair.Clear();
        AlignmentWarPressureByPair.Clear();
        SacredTargetByPair.Clear();
        AlignmentDoctrineActive = false;
    }

    public static void ReinforceTarget(Kingdom kingdom, Settlement settlement, float priority, float durationDays)
    {
        if (kingdom == null || settlement == null)
        {
            return;
        }

        string key = $"{GetKingdomKey(kingdom)}::{settlement.StringId}";
        float expiresAt = GetCurrentDay() + Math.Max(0.25f, durationDays);
        if (TargetPriorityByKey.TryGetValue(key, out FrontEntry current))
        {
            TargetPriorityByKey[key] = new FrontEntry(Math.Max(current.Priority, priority), Math.Max(current.ExpiresAtDay, expiresAt));
            return;
        }

        TargetPriorityByKey[key] = new FrontEntry(priority, expiresAt);
    }

    public static void ReinforceEnemy(Kingdom kingdom, Kingdom enemy, float priority, float durationDays)
    {
        if (kingdom == null || enemy == null)
        {
            return;
        }

        string key = $"{GetKingdomKey(kingdom)}->{GetKingdomKey(enemy)}";
        float expiresAt = GetCurrentDay() + Math.Max(0.25f, durationDays);
        if (EnemyPriorityByKey.TryGetValue(key, out FrontEntry current))
        {
            EnemyPriorityByKey[key] = new FrontEntry(Math.Max(current.Priority, priority), Math.Max(current.ExpiresAtDay, expiresAt));
            return;
        }

        EnemyPriorityByKey[key] = new FrontEntry(priority, expiresAt);
    }

    public static void ReinforceHolyWar(Kingdom kingdom, Kingdom enemy, Settlement sacredTarget, float priority, float durationDays)
    {
        if (kingdom == null || enemy == null || sacredTarget == null)
        {
            return;
        }

        string key = GetPairKey(kingdom, enemy);
        SetOrRaiseDirective(HolyWarPressureByPair, key, priority, durationDays);

        float expiresAt = GetCurrentDay() + Math.Max(0.25f, durationDays);
        if (SacredTargetByPair.TryGetValue(key, out SacredTargetEntry current))
        {
            SacredTargetByPair[key] = new SacredTargetEntry(
                current.Priority >= priority ? current.SettlementId : sacredTarget.StringId,
                Math.Max(current.Priority, priority),
                Math.Max(current.ExpiresAtDay, expiresAt));
            return;
        }

        SacredTargetByPair[key] = new SacredTargetEntry(sacredTarget.StringId, priority, expiresAt);
    }

    public static void ReinforceCollectiveDefense(Kingdom kingdom, Kingdom enemy, float priority, float durationDays)
    {
        if (kingdom == null || enemy == null)
        {
            return;
        }

        SetOrRaiseDirective(CollectiveDefensePressureByPair, GetPairKey(kingdom, enemy), priority, durationDays);
    }

    public static void ReinforceAlignmentWar(Kingdom kingdom, Kingdom enemy, float priority, float durationDays)
    {
        if (kingdom == null || enemy == null)
        {
            return;
        }

        SetOrRaiseDirective(AlignmentWarPressureByPair, GetPairKey(kingdom, enemy), priority, durationDays);
    }

    public static float GetTargetPriority(Kingdom kingdom, Settlement settlement)
    {
        if (kingdom == null || settlement == null)
        {
            return 0f;
        }

        string key = $"{GetKingdomKey(kingdom)}::{settlement.StringId}";
        if (!TargetPriorityByKey.TryGetValue(key, out FrontEntry entry))
        {
            return 0f;
        }

        if (entry.ExpiresAtDay < GetCurrentDay())
        {
            TargetPriorityByKey.Remove(key);
            return 0f;
        }

        return entry.Priority;
    }

    public static float GetEnemyPriority(Kingdom kingdom, Kingdom enemy)
    {
        if (kingdom == null || enemy == null)
        {
            return 0f;
        }

        string key = $"{GetKingdomKey(kingdom)}->{GetKingdomKey(enemy)}";
        if (!EnemyPriorityByKey.TryGetValue(key, out FrontEntry entry))
        {
            return 0f;
        }

        if (entry.ExpiresAtDay < GetCurrentDay())
        {
            EnemyPriorityByKey.Remove(key);
            return 0f;
        }

        return entry.Priority;
    }

    public static float GetHolyWarPressure(Kingdom kingdom, Kingdom enemy)
    {
        return GetDirectivePriority(HolyWarPressureByPair, kingdom, enemy);
    }

    public static float GetCollectiveDefensePressure(Kingdom kingdom, Kingdom enemy)
    {
        return GetDirectivePriority(CollectiveDefensePressureByPair, kingdom, enemy);
    }

    public static float GetAlignmentWarPressure(Kingdom kingdom, Kingdom enemy)
    {
        return GetDirectivePriority(AlignmentWarPressureByPair, kingdom, enemy);
    }

    public static float GetSacredTargetFactor(Kingdom kingdom, Kingdom enemy, Settlement targetSettlement)
    {
        if (kingdom == null || enemy == null || targetSettlement == null)
        {
            return 0f;
        }

        string key = GetPairKey(kingdom, enemy);
        if (!SacredTargetByPair.TryGetValue(key, out SacredTargetEntry entry))
        {
            return 0f;
        }

        if (entry.ExpiresAtDay < GetCurrentDay())
        {
            SacredTargetByPair.Remove(key);
            return 0f;
        }

        Settlement? sacredTarget = null;
        foreach (Settlement settlement in Settlement.All)
        {
            if (settlement.StringId == entry.SettlementId)
            {
                sacredTarget = settlement;
                break;
            }
        }

        if (sacredTarget == null)
        {
            SacredTargetByPair.Remove(key);
            return 0f;
        }

        if (sacredTarget == targetSettlement)
        {
            return entry.Priority;
        }

        float distanceSquared = sacredTarget.GatePosition.DistanceSquared(targetSettlement.GatePosition);
        if (distanceSquared <= FrontClusterDistanceSquared)
        {
            return entry.Priority * 0.6f;
        }

        return 0f;
    }

    private static void SetOrRaiseDirective(Dictionary<string, FrontEntry> store, string key, float priority, float durationDays)
    {
        float expiresAt = GetCurrentDay() + Math.Max(0.25f, durationDays);
        if (store.TryGetValue(key, out FrontEntry current))
        {
            store[key] = new FrontEntry(Math.Max(current.Priority, priority), Math.Max(current.ExpiresAtDay, expiresAt));
            return;
        }

        store[key] = new FrontEntry(priority, expiresAt);
    }

    private static float GetDirectivePriority(Dictionary<string, FrontEntry> store, Kingdom kingdom, Kingdom enemy)
    {
        if (kingdom == null || enemy == null)
        {
            return 0f;
        }

        string key = GetPairKey(kingdom, enemy);
        if (!store.TryGetValue(key, out FrontEntry entry))
        {
            return 0f;
        }

        if (entry.ExpiresAtDay < GetCurrentDay())
        {
            store.Remove(key);
            return 0f;
        }

        return entry.Priority;
    }

    private static string GetPairKey(Kingdom kingdom, Kingdom enemy)
    {
        return $"{GetKingdomKey(kingdom)}->{GetKingdomKey(enemy)}";
    }

    private static string GetKingdomKey(Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
    }

    private struct FrontEntry
    {
        public float Priority { get; }

        public float ExpiresAtDay { get; }

        public FrontEntry(float priority, float expiresAtDay)
        {
            Priority = priority;
            ExpiresAtDay = expiresAtDay;
        }
    }

    private struct SacredTargetEntry
    {
        public string SettlementId { get; }

        public float Priority { get; }

        public float ExpiresAtDay { get; }

        public SacredTargetEntry(string settlementId, float priority, float expiresAtDay)
        {
            SettlementId = settlementId;
            Priority = priority;
            ExpiresAtDay = expiresAtDay;
        }
    }
}
