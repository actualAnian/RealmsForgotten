using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RF_warsystem.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;

namespace RF_warsystem.Behaviors;

internal enum RFWarSpecialRequestType
{
    HolyWar,
    CollectiveDefense,
    AlignmentWar,
    MercenaryContract,
    StrategicIntrigue,
    EnduringRivalry,
    /// <summary>A kingdom's grand design (KingdomObjectives) pressing for the
    /// war its court keeps demanding. The most patient request type: it yields
    /// to every other type and gives the planner the longest window to reach
    /// the same conclusion on its own.</summary>
    GrandDesign
}

public sealed class RFWarSpecialAuthorityBehavior : CampaignBehaviorBase
{
    private readonly Dictionary<string, PendingSpecialWarRequest> _pendingWarsByPair = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingSpecialPeaceRequest> _pendingPeacesByPair = new(StringComparer.Ordinal);

    internal static RFWarSpecialAuthorityBehavior? Instance { get; private set; }

    public RFWarSpecialAuthorityBehavior()
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
        // Pending requests carry multi-day grace/fallback windows — losing them
        // on reload silently cancelled every special war (grand designs, holy
        // wars, collective defense) the moment the player saved mid-campaign.
        string warState = string.Empty;
        string peaceState = string.Empty;

        if (!dataStore.IsLoading)
        {
            warState = string.Join(";", _pendingWarsByPair.Values.Select(request => string.Join("|",
                request.AttackerKingdomId,
                request.DefenderKingdomId,
                ((int)request.Type).ToString(CultureInfo.InvariantCulture),
                request.Intensity.ToString("R", CultureInfo.InvariantCulture),
                request.SupportCount.ToString(CultureInfo.InvariantCulture),
                request.FirstRequestDay.ToString("R", CultureInfo.InvariantCulture),
                request.LastRequestDay.ToString("R", CultureInfo.InvariantCulture))));
            peaceState = string.Join(";", _pendingPeacesByPair.Values.Select(request => string.Join("|",
                request.LeftKingdomId,
                request.RightKingdomId,
                request.FirstRequestDay.ToString("R", CultureInfo.InvariantCulture),
                request.LastRequestDay.ToString("R", CultureInfo.InvariantCulture))));
        }

        dataStore.SyncData("RFWarSystem_PendingSpecialWars", ref warState);
        dataStore.SyncData("RFWarSystem_PendingSpecialPeaces", ref peaceState);

        if (!dataStore.IsLoading)
        {
            return;
        }

        _pendingWarsByPair.Clear();
        foreach (string entry in (warState ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split('|');
            if (parts.Length != 7
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)
                || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float intensity)
                || !int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int support)
                || !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float firstDay)
                || !float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float lastDay))
            {
                continue;
            }

            _pendingWarsByPair[$"{parts[0]}->{parts[1]}"] = new PendingSpecialWarRequest(
                parts[0], parts[1], (RFWarSpecialRequestType)type, intensity, support, firstDay, lastDay);
        }

        _pendingPeacesByPair.Clear();
        foreach (string entry in (peaceState ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split('|');
            if (parts.Length != 4
                || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float firstDay)
                || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float lastDay))
            {
                continue;
            }

            _pendingPeacesByPair[$"{parts[0]}->{parts[1]}"] = new PendingSpecialPeaceRequest(
                parts[0], parts[1], firstDay, lastDay);
        }
    }

    internal static void RequestWar(Kingdom attacker, Kingdom defender, RFWarSpecialRequestType type, float intensity = 1f)
    {
        Instance?.RegisterPendingWar(attacker, defender, type, intensity);
    }

    internal static void RequestPeace(Kingdom left, Kingdom right)
    {
        Instance?.RegisterPendingPeace(left, right);
    }

    internal static float GetPendingWarPriority(Kingdom attacker, Kingdom defender)
    {
        return Instance?.GetPendingWarPriorityInternal(attacker, defender) ?? 0f;
    }

    internal static RFWarSpecialRequestType? GetPendingWarType(Kingdom attacker, Kingdom defender)
    {
        return Instance?.GetPendingWarTypeInternal(attacker, defender);
    }

    internal static float GetPendingPeacePriority(Kingdom left, Kingdom right)
    {
        return Instance?.GetPendingPeacePriorityInternal(left, right) ?? 0f;
    }

    private RFWarSpecialRequestType? GetPendingWarTypeInternal(Kingdom attacker, Kingdom defender)
    {
        if (attacker == null || defender == null)
        {
            return null;
        }

        return _pendingWarsByPair.TryGetValue(GetPairKey(attacker, defender), out PendingSpecialWarRequest request)
            ? request.Type
            : null;
    }

    private void OnDailyTick()
    {
            using var _rfPerf = RF_warsystem.Diagnostics.RFPerfProbe.Measure("WarSpecialAuth");
        if (_pendingWarsByPair.Count == 0)
        {
            ProcessPendingPeaces();
            return;
        }

        ProcessPendingWars();
        ProcessPendingPeaces();
    }

    private void ProcessPendingWars()
    {
        if (_pendingWarsByPair.Count == 0)
        {
            return;
        }

        float now = GetCurrentDay();
        List<string> expired = new();

        // Snapshot: DeclareWarAction / TryPromoteSpecialWarProposal fire the
        // WarDeclared event synchronously, whose OnWarDeclared handler removes
        // from _pendingWarsByPair — iterating the live dict would throw
        // InvalidOperationException (only reachable now that GetCurrentDay works).
        foreach (KeyValuePair<string, PendingSpecialWarRequest> pair in _pendingWarsByPair.ToList())
        {
            string key = pair.Key;
            PendingSpecialWarRequest request = pair.Value;
            Kingdom? attacker = ResolveKingdom(request.AttackerKingdomId);
            Kingdom? defender = ResolveKingdom(request.DefenderKingdomId);
            if (attacker == null || defender == null || !IsValid(attacker) || !IsValid(defender) || attacker == defender)
            {
                expired.Add(key);
                continue;
            }

            if (attacker.IsAtWarWith(defender))
            {
                expired.Add(key);
                continue;
            }

            if (now - request.LastRequestDay > 3f)
            {
                expired.Add(key);
                continue;
            }

            float graceDays = GetEffectiveGraceDays(request);
            float fallbackDays = GetEffectiveFallbackDays(request);

            if (now - request.FirstRequestDay < graceDays)
            {
                continue;
            }

            if (RFWarDecisionPlannerBehavior.TryPromoteSpecialWarProposal(attacker, defender))
            {
                continue;
            }

            if (now - request.FirstRequestDay < fallbackDays)
            {
                continue;
            }

            RFWarSystemTraceLog.Write($"special_force type=war request={request.Type} intensity={request.Intensity:0.00} support={request.SupportCount} attacker={RFWarSystemTraceLog.FormatKingdom(attacker)} defender={RFWarSystemTraceLog.FormatKingdom(defender)} age={(now - request.FirstRequestDay):0.00}");
            DeclareWarAction.ApplyByKingdomDecision(attacker, defender);
            expired.Add(key);
        }

        foreach (string key in expired)
        {
            _pendingWarsByPair.Remove(key);
        }
    }

    private void ProcessPendingPeaces()
    {
        if (_pendingPeacesByPair.Count == 0)
        {
            return;
        }

        float now = GetCurrentDay();
        List<string> expired = new();

        // Snapshot: MakePeaceAction fires OnMakePeace synchronously, which
        // removes from _pendingPeacesByPair during this enumeration.
        foreach (KeyValuePair<string, PendingSpecialPeaceRequest> pair in _pendingPeacesByPair.ToList())
        {
            string key = pair.Key;
            PendingSpecialPeaceRequest request = pair.Value;
            Kingdom? left = ResolveKingdom(request.LeftKingdomId);
            Kingdom? right = ResolveKingdom(request.RightKingdomId);
            if (left == null || right == null || !IsValid(left) || !IsValid(right) || left == right)
            {
                expired.Add(key);
                continue;
            }

            if (!left.IsAtWarWith(right))
            {
                expired.Add(key);
                continue;
            }

            if (now - request.LastRequestDay > 3f)
            {
                expired.Add(key);
                continue;
            }

            if (now - request.FirstRequestDay < 0.1f)
            {
                continue;
            }

            if (RFWarDecisionPlannerBehavior.TryPromoteSpecialPeaceProposal(left, right))
            {
                continue;
            }

            if (now - request.FirstRequestDay < 1.35f)
            {
                continue;
            }

            RFWarSystemTraceLog.Write($"special_force type=peace left={RFWarSystemTraceLog.FormatKingdom(left)} right={RFWarSystemTraceLog.FormatKingdom(right)} age={(now - request.FirstRequestDay):0.00}");
            MakePeaceAction.Apply(left, right);
            expired.Add(key);
        }

        foreach (string key in expired)
        {
            _pendingPeacesByPair.Remove(key);
        }
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
    {
        if (faction1 is not Kingdom left || faction2 is not Kingdom right)
        {
            return;
        }

        _pendingWarsByPair.Remove(GetPairKey(left, right));
        _pendingWarsByPair.Remove(GetPairKey(right, left));
        _pendingPeacesByPair.Remove(GetPairKey(left, right));
        _pendingPeacesByPair.Remove(GetPairKey(right, left));
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
    {
        if (faction1 is not Kingdom left || faction2 is not Kingdom right)
        {
            return;
        }

        _pendingWarsByPair.Remove(GetPairKey(left, right));
        _pendingWarsByPair.Remove(GetPairKey(right, left));
    }

    private void RegisterPendingWar(Kingdom attacker, Kingdom defender, RFWarSpecialRequestType type, float intensity)
    {
        if (!IsValid(attacker) || !IsValid(defender) || attacker == defender || attacker.IsAtWarWith(defender))
        {
            return;
        }

        string key = GetPairKey(attacker, defender);
        float now = GetCurrentDay();
        float clampedIntensity = Math.Max(0.35f, Math.Min(1f, intensity));
        if (_pendingWarsByPair.TryGetValue(key, out PendingSpecialWarRequest current))
        {
            _pendingWarsByPair[key] = new PendingSpecialWarRequest(
                current.AttackerKingdomId,
                current.DefenderKingdomId,
                GetDominantType(current.Type, type),
                Math.Min(1f, Math.Max(current.Intensity, clampedIntensity) + 0.08f),
                Math.Min(6, current.SupportCount + 1),
                current.FirstRequestDay,
                now);
            return;
        }

        _pendingWarsByPair[key] = new PendingSpecialWarRequest(
            GetKingdomKey(attacker),
            GetKingdomKey(defender),
            type,
            clampedIntensity,
            1,
            now,
            now);
    }

    private void RegisterPendingPeace(Kingdom left, Kingdom right)
    {
        if (!IsValid(left) || !IsValid(right) || left == right || !left.IsAtWarWith(right))
        {
            return;
        }

        string key = GetPairKey(left, right);
        float now = GetCurrentDay();
        if (_pendingPeacesByPair.TryGetValue(key, out PendingSpecialPeaceRequest current))
        {
            _pendingPeacesByPair[key] = new PendingSpecialPeaceRequest(
                current.LeftKingdomId,
                current.RightKingdomId,
                current.FirstRequestDay,
                now);
            return;
        }

        _pendingPeacesByPair[key] = new PendingSpecialPeaceRequest(
            GetKingdomKey(left),
            GetKingdomKey(right),
            now,
            now);
    }

    private float GetPendingWarPriorityInternal(Kingdom attacker, Kingdom defender)
    {
        string key = GetPairKey(attacker, defender);
        if (!_pendingWarsByPair.TryGetValue(key, out PendingSpecialWarRequest request))
        {
            return 0f;
        }

        float age = GetCurrentDay() - request.FirstRequestDay;
        float grace = GetEffectiveGraceDays(request);
        float fallback = GetEffectiveFallbackDays(request);
        if (fallback <= grace)
        {
            return 1f;
        }

        float supportFactor = GetSupportFactor(request);
        float progress = Math.Max(0f, Math.Min(1f, age / fallback));
        return Math.Max(0f, Math.Min(1f, progress * (0.68f + request.Intensity * 0.2f + supportFactor * 0.12f)));
    }

    private float GetPendingPeacePriorityInternal(Kingdom left, Kingdom right)
    {
        string key = GetPairKey(left, right);
        if (!_pendingPeacesByPair.TryGetValue(key, out PendingSpecialPeaceRequest request))
        {
            return 0f;
        }

        float age = GetCurrentDay() - request.FirstRequestDay;
        return Math.Max(0f, Math.Min(1f, age / 1.35f));
    }

    private static RFWarSpecialRequestType GetDominantType(RFWarSpecialRequestType left, RFWarSpecialRequestType right)
    {
        if (left == RFWarSpecialRequestType.CollectiveDefense || right == RFWarSpecialRequestType.CollectiveDefense)
        {
            return RFWarSpecialRequestType.CollectiveDefense;
        }

        if (left == RFWarSpecialRequestType.EnduringRivalry || right == RFWarSpecialRequestType.EnduringRivalry)
        {
            return RFWarSpecialRequestType.EnduringRivalry;
        }

        if (left == RFWarSpecialRequestType.StrategicIntrigue || right == RFWarSpecialRequestType.StrategicIntrigue)
        {
            return RFWarSpecialRequestType.StrategicIntrigue;
        }

        if (left == RFWarSpecialRequestType.MercenaryContract || right == RFWarSpecialRequestType.MercenaryContract)
        {
            return RFWarSpecialRequestType.MercenaryContract;
        }

        if (left == RFWarSpecialRequestType.HolyWar || right == RFWarSpecialRequestType.HolyWar)
        {
            return RFWarSpecialRequestType.HolyWar;
        }

        if (left == RFWarSpecialRequestType.GrandDesign)
        {
            return right;
        }

        if (right == RFWarSpecialRequestType.GrandDesign)
        {
            return left;
        }

        return right;
    }

    private static float GetGraceDays(RFWarSpecialRequestType type)
    {
        return type switch
        {
            RFWarSpecialRequestType.CollectiveDefense => 0.35f,
            RFWarSpecialRequestType.EnduringRivalry => 0.3f,
            RFWarSpecialRequestType.StrategicIntrigue => 0.4f,
            RFWarSpecialRequestType.MercenaryContract => 0.45f,
            RFWarSpecialRequestType.HolyWar => 0.75f,
            RFWarSpecialRequestType.AlignmentWar => 1.2f,
            RFWarSpecialRequestType.GrandDesign => 1.6f,
            _ => 1f
        };
    }

    private static float GetFallbackForceDays(RFWarSpecialRequestType type)
    {
        return type switch
        {
            RFWarSpecialRequestType.CollectiveDefense => 1.6f,
            RFWarSpecialRequestType.EnduringRivalry => 1.4f,
            RFWarSpecialRequestType.StrategicIntrigue => 1.75f,
            RFWarSpecialRequestType.MercenaryContract => 1.9f,
            RFWarSpecialRequestType.HolyWar => 2.6f,
            RFWarSpecialRequestType.AlignmentWar => 3.2f,
            // Longest of all: a grand design should almost always reach war
            // through the planner's own proposal, not through the force path.
            RFWarSpecialRequestType.GrandDesign => 6f,
            _ => 2.2f
        };
    }

    private static float GetEffectiveGraceDays(PendingSpecialWarRequest request)
    {
        float baseGrace = GetGraceDays(request.Type);
        return Math.Max(0.15f, baseGrace - request.Intensity * 0.2f - GetSupportFactor(request) * 0.12f);
    }

    private static float GetEffectiveFallbackDays(PendingSpecialWarRequest request)
    {
        float baseFallback = GetFallbackForceDays(request.Type);
        return Math.Max(GetEffectiveGraceDays(request) + 0.2f, baseFallback - request.Intensity * 0.55f - GetSupportFactor(request) * 0.35f);
    }

    private static float GetSupportFactor(PendingSpecialWarRequest request)
    {
        return Math.Max(0f, Math.Min(1f, (request.SupportCount - 1) / 4f));
    }

    private static Kingdom? ResolveKingdom(string kingdomId)
    {
        return Kingdom.All.FirstOrDefault(kingdom => GetKingdomKey(kingdom) == kingdomId);
    }

    private static bool IsValid(Kingdom? kingdom)
    {
        return kingdom != null && !kingdom.IsEliminated && kingdom.Leader != null;
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
    }

    private static string GetPairKey(Kingdom attacker, Kingdom defender)
    {
        return $"{GetKingdomKey(attacker)}->{GetKingdomKey(defender)}";
    }

    private static string GetKingdomKey(Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private readonly struct PendingSpecialWarRequest
    {
        public PendingSpecialWarRequest(
            string attackerKingdomId,
            string defenderKingdomId,
            RFWarSpecialRequestType type,
            float intensity,
            int supportCount,
            float firstRequestDay,
            float lastRequestDay)
        {
            AttackerKingdomId = attackerKingdomId;
            DefenderKingdomId = defenderKingdomId;
            Type = type;
            Intensity = intensity;
            SupportCount = supportCount;
            FirstRequestDay = firstRequestDay;
            LastRequestDay = lastRequestDay;
        }

        public string AttackerKingdomId { get; }

        public string DefenderKingdomId { get; }

        public RFWarSpecialRequestType Type { get; }

        public float Intensity { get; }

        public int SupportCount { get; }

        public float FirstRequestDay { get; }

        public float LastRequestDay { get; }
    }

    private readonly struct PendingSpecialPeaceRequest
    {
        public PendingSpecialPeaceRequest(
            string leftKingdomId,
            string rightKingdomId,
            float firstRequestDay,
            float lastRequestDay)
        {
            LeftKingdomId = leftKingdomId;
            RightKingdomId = rightKingdomId;
            FirstRequestDay = firstRequestDay;
            LastRequestDay = lastRequestDay;
        }

        public string LeftKingdomId { get; }

        public string RightKingdomId { get; }

        public float FirstRequestDay { get; }

        public float LastRequestDay { get; }
    }
}
