using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RF_warsystem.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Logic;

internal static class RFWarStrategicAssessment
{
    internal static float GetWarExhaustion(Kingdom viewer, Kingdom enemy)
    {
        RFWarLedgerRecord? record = RFWarLedgerBehavior.GetActiveWar(viewer, enemy);
        if (record == null)
        {
            return 0f;
        }

        string viewerId = GetKingdomId(viewer);
        float score = record.GetScoreFor(viewerId);
        float duration = Math.Max(0f, GetCurrentDay() - record.StartedAtDay);
        float losingPressure = Math.Max(0f, -score) / 100f;
        float fiefLossPressure = GetInitialFiefLossRatio(record, viewerId);
        float raidPressure = Math.Min(1f, CountEventsAgainst(record, viewerId, RFWarLedgerEventType.VillageRaided) / 8f);
        float captivityPressure = Math.Min(1f,
            (CountEventsAgainst(record, viewerId, RFWarLedgerEventType.NobleCaptured)
             + (3f * CountEventsAgainst(record, viewerId, RFWarLedgerEventType.RulerCaptured))) / 8f);
        float multiFrontPressure = Math.Min(1f, Math.Max(0, viewer.FactionsAtWarWith.OfType<Kingdom>().Count() - 1) / 3f);
        float treasuryPressure = GetTreasuryDistress(viewer);
        float durationPressure = Math.Min(1f, duration / 180f);
        float inactivityPressure = GetInactivityPressure(record);
        float victoryRelief = Math.Max(0f, score) / 100f;

        return Clamp01(
            (durationPressure * 0.16f)
            + (losingPressure * 0.25f)
            + (fiefLossPressure * 0.2f)
            + (raidPressure * 0.08f)
            + (captivityPressure * 0.08f)
            + (multiFrontPressure * 0.12f)
            + (treasuryPressure * 0.12f)
            + (inactivityPressure * 0.08f)
            - (victoryRelief * 0.12f));
    }

    internal static float GetWarWill(Kingdom viewer, Kingdom enemy)
    {
        RFWarLedgerRecord? record = RFWarLedgerBehavior.GetActiveWar(viewer, enemy);
        if (record == null)
        {
            return 0f;
        }

        string viewerId = GetKingdomId(viewer);
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(viewer);
        float score = record.GetScoreFor(viewerId);
        float motiveCommitment = GetMotiveCommitment(record.Motive, record.InitiatorKingdomId == viewerId, profile);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(viewer, enemy);
        float focus = Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(viewer, enemy));
        float grievance = GetHistoricalGrievance(viewer, enemy);
        float exhaustion = GetWarExhaustion(viewer, enemy);

        return Clamp01(
            0.24f
            + (motiveCommitment * 0.3f)
            + (Math.Max(0f, score) / 100f * 0.16f)
            + (Math.Max(0f, objectiveCommitment) * 0.1f)
            + (Math.Min(1f, focus) * 0.08f)
            + (grievance * 0.12f)
            + (Math.Max(0f, profile.Persistence) * 0.08f)
            - (exhaustion * 0.38f));
    }

    internal static float GetPeacePressure(Kingdom viewer, Kingdom enemy)
    {
        RFWarLedgerRecord? record = RFWarLedgerBehavior.GetActiveWar(viewer, enemy);
        if (record == null)
        {
            return 0f;
        }

        float score = record.GetScoreFor(GetKingdomId(viewer));
        return Clamp01(
            (GetWarExhaustion(viewer, enemy) * 0.62f)
            + (Math.Max(0f, -score) / 100f * 0.25f)
            + (GetStalematePressure(viewer, enemy) * 0.22f)
            - (GetWarWill(viewer, enemy) * 0.28f));
    }

    internal static float GetFinishPressure(Kingdom viewer, Kingdom enemy)
    {
        RFWarLedgerRecord? record = RFWarLedgerBehavior.GetActiveWar(viewer, enemy);
        if (record == null)
        {
            return 0f;
        }

        float score = record.GetScoreFor(GetKingdomId(viewer));
        int enemyFiefs = enemy.Fiefs.Count();
        float collapse = enemyFiefs <= 1 ? 1f : enemyFiefs <= 3 ? 0.65f : enemyFiefs <= 5 ? 0.3f : 0f;
        float recentProgress = HasRecentPositiveProgress(record, GetKingdomId(viewer), 18f) ? 1f : 0f;
        float objectiveCommitment = Math.Max(0f, RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(viewer, enemy));
        return Clamp01(
            (Math.Max(0f, score) / 100f * 0.42f)
            + (collapse * 0.3f)
            + (recentProgress * 0.16f)
            + (objectiveCommitment * 0.12f));
    }

    internal static float GetStalematePressure(Kingdom viewer, Kingdom enemy)
    {
        RFWarLedgerRecord? record = RFWarLedgerBehavior.GetActiveWar(viewer, enemy);
        if (record == null)
        {
            return 0f;
        }

        float duration = Math.Max(0f, GetCurrentDay() - record.StartedAtDay);
        float score = Math.Abs(record.GetScoreFor(GetKingdomId(viewer)));
        if (duration < 35f || score > 35f)
        {
            return 0f;
        }

        float inactivity = GetInactivityPressure(record);
        float durationFactor = Math.Min(1f, (duration - 35f) / 120f);
        float balanceFactor = 1f - Math.Min(1f, score / 35f);
        return Clamp01((inactivity * 0.5f) + (durationFactor * 0.3f) + (balanceFactor * 0.2f));
    }

    internal static float GetHistoricalGrievance(Kingdom viewer, Kingdom enemy)
    {
        string viewerId = GetKingdomId(viewer);
        float now = GetCurrentDay();
        float grievance = 0f;

        foreach (RFWarLedgerRecord record in RFWarLedgerBehavior.GetTrackedWars()
                     .Where(candidate => !candidate.IsActive && IsSamePair(candidate, viewer, enemy))
                     .OrderByDescending(candidate => candidate.EndedAtDay)
                     .Take(4))
        {
            float age = Math.Max(0f, now - record.EndedAtDay);
            float recency = Math.Max(0f, 1f - (age / 720f));
            float defeat = Math.Max(0f, -record.GetScoreFor(viewerId)) / 100f;
            float lostFiefs = Math.Min(1f,
                CountEventsAgainst(record, viewerId,
                    RFWarLedgerEventType.TownCaptured,
                    RFWarLedgerEventType.CastleCaptured) / 4f);
            float captivity = Math.Min(1f,
                (CountEventsAgainst(record, viewerId, RFWarLedgerEventType.NobleCaptured)
                 + (2f * CountEventsAgainst(record, viewerId, RFWarLedgerEventType.RulerCaptured))) / 6f);
            grievance += recency * ((defeat * 0.5f) + (lostFiefs * 0.35f) + (captivity * 0.15f));
        }

        return Clamp01(grievance);
    }

    internal static string BuildKingdomSituationReport(Kingdom kingdom)
    {
        if (kingdom == null)
        {
            return "No kingdom is available for this report.";
        }

        List<Kingdom> enemies = kingdom.FactionsAtWarWith.OfType<Kingdom>()
            .Where(enemy => enemy != null && !enemy.IsEliminated)
            .OrderByDescending(enemy => GetWarExhaustion(kingdom, enemy))
            .ToList();
        if (enemies.Count == 0)
        {
            return "Current wars: none.";
        }

        StringBuilder builder = new("Current wars:");
        foreach (Kingdom enemy in enemies)
        {
            RFWarLedgerRecord? record = RFWarLedgerBehavior.GetActiveWar(kingdom, enemy);
            float score = record?.GetScoreFor(GetKingdomId(kingdom)) ?? 0f;
            builder.AppendLine();
            builder.Append("- ").Append(enemy.Name)
                .Append(": score ").Append(score.ToString("+0;-0;0", CultureInfo.InvariantCulture))
                .Append(", motive ").Append(FormatMotive(record?.Motive ?? RFWarMotive.Unknown))
                .Append(", exhaustion ").Append((GetWarExhaustion(kingdom, enemy) * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%')
                .Append(", war will ").Append((GetWarWill(kingdom, enemy) * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%')
                .Append(", peace pressure ").Append((GetPeacePressure(kingdom, enemy) * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%');
        }

        return builder.ToString();
    }

    private static float GetMotiveCommitment(RFWarMotive motive, bool isInitiator, RFWarStrategicProfile profile)
    {
        float baseValue = motive switch
        {
            RFWarMotive.Reconquest => 0.9f,
            RFWarMotive.HolyWar => 0.88f + Math.Max(0f, profile.SacredZeal) * 0.12f,
            RFWarMotive.CollectiveDefense => isInitiator ? 0.78f : 0.9f,
            RFWarMotive.DefensiveWar => isInitiator ? 0.5f : 0.85f,
            RFWarMotive.EnduringRivalry => 0.86f + Math.Max(0f, profile.RevengeBias) * 0.12f,
            RFWarMotive.GrandDesign => 0.82f,
            RFWarMotive.StrategicIntrigue => 0.76f,
            RFWarMotive.AlignmentWar => 0.82f,
            RFWarMotive.MercenaryContract => 0.68f,
            RFWarMotive.Containment => 0.72f,
            RFWarMotive.PunitiveWar => 0.62f,
            RFWarMotive.Expansion => 0.66f,
            RFWarMotive.RecoveredConflict => 0.52f,
            _ => 0.5f
        };
        return Clamp01(baseValue);
    }

    private static float GetInitialFiefLossRatio(RFWarLedgerRecord record, string viewerId)
    {
        List<string> initialFiefs = record.InitialOwnerBySettlementId
            .Where(pair => pair.Value == viewerId)
            .Select(pair => pair.Key)
            .ToList();
        if (initialFiefs.Count == 0)
        {
            return 0f;
        }

        int lost = initialFiefs.Count(settlementId =>
        {
            Settlement? settlement = Settlement.Find(settlementId);
            return settlement?.OwnerClan?.Kingdom == null || GetKingdomId(settlement.OwnerClan.Kingdom) != viewerId;
        });
        return Math.Min(1f, lost / (float)initialFiefs.Count);
    }

    private static float CountEventsAgainst(
        RFWarLedgerRecord record,
        string viewerId,
        params RFWarLedgerEventType[] eventTypes)
    {
        return record.Events.Count(ledgerEvent =>
            ledgerEvent.TargetKingdomId == viewerId && eventTypes.Contains(ledgerEvent.Type));
    }

    private static float GetInactivityPressure(RFWarLedgerRecord record)
    {
        RFWarLedgerEvent? lastDecisiveEvent = record.Events.LastOrDefault(ledgerEvent =>
            ledgerEvent.Type == RFWarLedgerEventType.BattleWon
            || ledgerEvent.Type == RFWarLedgerEventType.MajorBattleWon
            || ledgerEvent.Type == RFWarLedgerEventType.TownCaptured
            || ledgerEvent.Type == RFWarLedgerEventType.CastleCaptured
            || ledgerEvent.Type == RFWarLedgerEventType.TownRetaken
            || ledgerEvent.Type == RFWarLedgerEventType.CastleRetaken);
        float lastDay = lastDecisiveEvent?.Day ?? record.StartedAtDay;
        return Clamp01((GetCurrentDay() - lastDay - 18f) / 70f);
    }

    private static bool HasRecentPositiveProgress(RFWarLedgerRecord record, string viewerId, float days)
    {
        float cutoff = GetCurrentDay() - days;
        return record.Events.Any(ledgerEvent =>
            ledgerEvent.Day >= cutoff
            && ledgerEvent.ActorKingdomId == viewerId
            && ledgerEvent.Delta != 0f
            && (viewerId == record.SideAKingdomId ? ledgerEvent.Delta : -ledgerEvent.Delta) > 0f);
    }

    private static float GetTreasuryDistress(Kingdom kingdom)
    {
        float gold = kingdom.RulingClan?.Gold ?? 0f;
        if (gold <= 0f)
        {
            return 1f;
        }

        return Clamp01((120000f - gold) / 120000f);
    }

    private static bool IsSamePair(RFWarLedgerRecord record, Kingdom first, Kingdom second)
    {
        string firstId = GetKingdomId(first);
        string secondId = GetKingdomId(second);
        return (record.SideAKingdomId == firstId && record.SideBKingdomId == secondId)
               || (record.SideAKingdomId == secondId && record.SideBKingdomId == firstId);
    }

    private static string FormatMotive(RFWarMotive motive)
    {
        return motive switch
        {
            RFWarMotive.RecoveredConflict => "recovered conflict",
            RFWarMotive.DefensiveWar => "defensive war",
            RFWarMotive.Reconquest => "reconquest",
            RFWarMotive.HolyWar => "holy war",
            RFWarMotive.PunitiveWar => "punitive campaign",
            RFWarMotive.Expansion => "expansion",
            RFWarMotive.Containment => "containment",
            RFWarMotive.GrandDesign => "grand design",
            RFWarMotive.StrategicIntrigue => "strategic intrigue",
            RFWarMotive.MercenaryContract => "mercenary contract",
            RFWarMotive.AlignmentWar => "alignment war",
            RFWarMotive.CollectiveDefense => "collective defense",
            RFWarMotive.EnduringRivalry => "enduring rivalry",
            _ => "undetermined"
        };
    }

    private static string GetKingdomId(Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }
}
