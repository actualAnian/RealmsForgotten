using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAIAdaptiveMemory
{
    private const int MaxPersistedRecords = 240;
    private const int MinRelevantSamples = 2;
    private const float SnapshotInterval = 2f;

    private static readonly string[] LogPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_BattleAI_AdaptiveMemory.log")
    };

    private static readonly List<CompletedRecord> History = new();
    private static readonly Dictionary<Team, ActiveRecord> ActiveRecords = new();

    private static Mission? _trackedMission;
    private static bool _historyLoaded;
    private static float _nextSnapshotTime;

    public static void Tick()
    {
        EnsureHistoryLoaded();

        Mission? mission = Mission.Current;
        if (!ReferenceEquals(_trackedMission, mission))
        {
            FinalizeTrackedMission();
            _trackedMission = mission;
            _nextSnapshotTime = 0f;
        }

        if (mission == null)
        {
            return;
        }

        if (mission.IsMissionEnding || mission.MissionEnded || mission.CurrentState != Mission.State.Continuing)
        {
            return;
        }

        if (mission.CurrentTime < _nextSnapshotTime)
        {
            return;
        }

        _nextSnapshotTime = mission.CurrentTime + SnapshotInterval;
        foreach (KeyValuePair<Team, ActiveRecord> pair in ActiveRecords.ToList())
        {
            Team team = pair.Key;
            ActiveRecord active = pair.Value;
            if (team == null)
            {
                continue;
            }

            UpdateActiveRecord(team, active);
        }
    }

    public static void NoteDoctrineApplied(Team team, string doctrineId)
    {
        EnsureHistoryLoaded();
        ResetMissionIfChanged();

        string cultureId = BattleAICombatantHelper.GetTeamCultureId(team);
        string enemyProfile = BuildEnemyProfile(team);
        float powerRatio = GetSafeRemainingPowerRatio(team, fallback: 1f);
        float missionTime = Mission.Current?.CurrentTime ?? 0f;

        ActiveRecords[team] = new ActiveRecord(doctrineId, cultureId, enemyProfile, powerRatio, missionTime);
    }

    public static AdaptiveMemoryBias EvaluateDoctrineBias(Team team, string doctrineId)
    {
        EnsureHistoryLoaded();

        string cultureId = BattleAICombatantHelper.GetTeamCultureId(team);
        string enemyProfile = BuildEnemyProfile(team);

        List<CompletedRecord> matchingRecords = History
            .Where(record =>
                string.Equals(record.DoctrineId, doctrineId, StringComparison.Ordinal)
                && string.Equals(record.CultureId, cultureId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(record.EnemyProfile, enemyProfile, StringComparison.Ordinal))
            .ToList();

        if (matchingRecords.Count == 0)
        {
            return new AdaptiveMemoryBias(0f, $"profile={enemyProfile} samples=0");
        }

        float averageDelta = matchingRecords.Average(record => record.EndDelta);
        float averagePeak = matchingRecords.Average(record => record.PeakDelta);
        float bonus = TaleWorlds.Library.MathF.Clamp(averageDelta * 0.45f + averagePeak * 0.15f, -0.16f, 0.16f);

        if (matchingRecords.Count >= MinRelevantSamples)
        {
            CompletedRecord lastRecord = matchingRecords[matchingRecords.Count - 1];
            if (lastRecord.EndDelta < -0.12f)
            {
                bonus -= 0.03f;
            }
        }

        bonus = TaleWorlds.Library.MathF.Clamp(bonus, -0.18f, 0.18f);
        return new AdaptiveMemoryBias(
            bonus,
            $"profile={enemyProfile} samples={matchingRecords.Count} avgDelta={averageDelta:F2} avgPeak={averagePeak:F2} bonus={bonus:F2}");
    }

    public static string DescribeLiveState(Team team)
    {
        EnsureHistoryLoaded();
        ResetMissionIfChanged();

        string enemyProfile = BuildEnemyProfile(team);
        if (!ActiveRecords.TryGetValue(team, out ActiveRecord? active))
        {
            return $"profile={enemyProfile} live=none";
        }

        return $"profile={enemyProfile} doctrine={active.DoctrineId} delta={active.LastPowerRatio - active.StartPowerRatio:F2} peak={active.BestPowerRatio - active.StartPowerRatio:F2}";
    }

    private static void ResetMissionIfChanged()
    {
        Mission? mission = Mission.Current;
        if (ReferenceEquals(_trackedMission, mission))
        {
            return;
        }

        FinalizeTrackedMission();
        _trackedMission = mission;
        _nextSnapshotTime = 0f;
    }

    private static void FinalizeTrackedMission()
    {
        if (_trackedMission == null)
        {
            ActiveRecords.Clear();
            return;
        }

        foreach (KeyValuePair<Team, ActiveRecord> pair in ActiveRecords.ToList())
        {
            CompletedRecord completed = pair.Value.ToCompletedRecord();
            History.Add(completed);
            PersistRecord(completed);
        }

        TrimHistory();
        ActiveRecords.Clear();
    }

    private static void UpdateActiveRecord(Team team, ActiveRecord active)
    {
        float powerRatio = GetSafeRemainingPowerRatio(team, active.LastPowerRatio);
        active.LastPowerRatio = powerRatio;
        active.BestPowerRatio = Math.Max(active.BestPowerRatio, powerRatio);
        active.WorstPowerRatio = Math.Min(active.WorstPowerRatio, powerRatio);
        active.LastMissionTime = Mission.Current?.CurrentTime ?? active.LastMissionTime;
    }

    private static float GetSafeRemainingPowerRatio(Team? team, float fallback)
    {
        Mission? mission = Mission.Current;
        if (team == null || mission == null)
        {
            return fallback;
        }

        if (mission.IsMissionEnding || mission.MissionEnded || mission.CurrentState != Mission.State.Continuing)
        {
            return fallback;
        }

        try
        {
            TeamQuerySystem? querySystem = team.QuerySystem;
            if (querySystem == null)
            {
                return fallback;
            }

            // Clamp: once one side is nearly annihilated the raw ratio explodes
            // (own power / a sliver of enemy power → 20, 57, 147…). Those spikes
            // are meaningless for "how did this doctrine do" and, unclamped, they
            // poisoned the adaptive-memory averages (avgDelta/avgPeak). Cap at a
            // decisive-but-sane 3.0 (and floor at 0).
            float ratio = querySystem.RemainingPowerRatio;
            if (float.IsNaN(ratio) || float.IsInfinity(ratio))
            {
                return fallback;
            }
            return Math.Max(0f, Math.Min(3f, ratio));
        }
        catch
        {
            return fallback;
        }
    }

    private static string BuildEnemyProfile(Team team)
    {
        Mission? mission = Mission.Current;
        if (mission == null || mission.IsMissionEnding || mission.MissionEnded || mission.CurrentState != Mission.State.Continuing)
        {
            return "unknown";
        }

        Team? enemyTeam = mission.Teams.FirstOrDefault(candidate => candidate.Side != team.Side && candidate.HasTeamAi);
        if (enemyTeam == null)
        {
            return "unknown";
        }

        BattleAIFormationComposition composition;
        try
        {
            composition = BattleAIFormationCompositionHelper.FromTeam(enemyTeam);
        }
        catch
        {
            return "unknown";
        }

        if (composition.RangedCavalryRatio > 0.12f)
        {
            return "horse_archer";
        }

        if (composition.MountedRatio > 0.3f)
        {
            return "mounted_heavy";
        }

        if (composition.RangedRatio > 0.3f)
        {
            return "ranged_heavy";
        }

        if (composition.InfantryRatio > 0.58f)
        {
            return "infantry_heavy";
        }

        if (composition.MountedRatio > 0.18f && composition.RangedRatio > 0.18f)
        {
            return "combined_arms";
        }

        return "balanced";
    }

    private static void EnsureHistoryLoaded()
    {
        if (_historyLoaded)
        {
            return;
        }

        _historyLoaded = true;
        foreach (string logPath in LogPaths.Distinct())
        {
            try
            {
                if (!File.Exists(logPath))
                {
                    continue;
                }

                foreach (string line in File.ReadLines(logPath))
                {
                    CompletedRecord record;
                    if (TryParseRecord(line, out record))
                    {
                        History.Add(record);
                    }
                }
            }
            catch
            {
                // Keep adaptive memory best-effort only.
            }
        }

        TrimHistory();
    }

    private static void PersistRecord(CompletedRecord record)
    {
        string line = string.Join("|", new[]
        {
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            record.CultureId,
            record.EnemyProfile,
            record.DoctrineId,
            record.StartPowerRatio.ToString("F3", CultureInfo.InvariantCulture),
            record.EndPowerRatio.ToString("F3", CultureInfo.InvariantCulture),
            record.BestPowerRatio.ToString("F3", CultureInfo.InvariantCulture),
            record.WorstPowerRatio.ToString("F3", CultureInfo.InvariantCulture),
            record.Duration.ToString("F1", CultureInfo.InvariantCulture)
        }) + Environment.NewLine;

        foreach (string logPath in LogPaths.Distinct())
        {
            try
            {
                string? directory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(logPath, line);
            }
            catch
            {
                // Keep adaptive memory best-effort only.
            }
        }
    }

    private static bool TryParseRecord(string line, out CompletedRecord record)
    {
        record = null!;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string[] parts = line.Split('|');
        if (parts.Length < 9)
        {
            return false;
        }

        if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float startPower))
        {
            return false;
        }

        if (!float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float endPower))
        {
            return false;
        }

        if (!float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float bestPower))
        {
            return false;
        }

        if (!float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float worstPower))
        {
            return false;
        }

        if (!float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
        {
            return false;
        }

        record = new CompletedRecord(parts[1], parts[2], parts[3], startPower, endPower, bestPower, worstPower, duration);
        return true;
    }

    private static void TrimHistory()
    {
        if (History.Count <= MaxPersistedRecords)
        {
            return;
        }

        int removeCount = History.Count - MaxPersistedRecords;
        History.RemoveRange(0, removeCount);
    }

    private sealed class ActiveRecord
    {
        public ActiveRecord(string doctrineId, string cultureId, string enemyProfile, float startPowerRatio, float startMissionTime)
        {
            DoctrineId = doctrineId;
            CultureId = cultureId;
            EnemyProfile = enemyProfile;
            StartPowerRatio = startPowerRatio;
            BestPowerRatio = startPowerRatio;
            WorstPowerRatio = startPowerRatio;
            LastPowerRatio = startPowerRatio;
            StartMissionTime = startMissionTime;
            LastMissionTime = startMissionTime;
        }

        public string DoctrineId { get; }

        public string CultureId { get; }

        public string EnemyProfile { get; }

        public float StartPowerRatio { get; }

        public float BestPowerRatio { get; set; }

        public float WorstPowerRatio { get; set; }

        public float LastPowerRatio { get; set; }

        public float StartMissionTime { get; }

        public float LastMissionTime { get; set; }

        public CompletedRecord ToCompletedRecord()
        {
            return new CompletedRecord(
                CultureId,
                EnemyProfile,
                DoctrineId,
                StartPowerRatio,
                LastPowerRatio,
                BestPowerRatio,
                WorstPowerRatio,
                Math.Max(0f, LastMissionTime - StartMissionTime));
        }
    }

    private sealed class CompletedRecord
    {
        public CompletedRecord(
            string cultureId,
            string enemyProfile,
            string doctrineId,
            float startPowerRatio,
            float endPowerRatio,
            float bestPowerRatio,
            float worstPowerRatio,
            float duration)
        {
            CultureId = cultureId ?? string.Empty;
            EnemyProfile = enemyProfile ?? string.Empty;
            DoctrineId = doctrineId ?? string.Empty;
            StartPowerRatio = startPowerRatio;
            EndPowerRatio = endPowerRatio;
            BestPowerRatio = bestPowerRatio;
            WorstPowerRatio = worstPowerRatio;
            Duration = duration;
        }

        public string CultureId { get; }

        public string EnemyProfile { get; }

        public string DoctrineId { get; }

        public float StartPowerRatio { get; }

        public float EndPowerRatio { get; }

        public float BestPowerRatio { get; }

        public float WorstPowerRatio { get; }

        public float Duration { get; }

        public float EndDelta => EndPowerRatio - StartPowerRatio;

        public float PeakDelta => BestPowerRatio - StartPowerRatio;
    }
}

internal readonly struct AdaptiveMemoryBias
{
    public AdaptiveMemoryBias(float scoreBonus, string description)
    {
        ScoreBonus = scoreBonus;
        Description = description ?? string.Empty;
    }

    public float ScoreBonus { get; }

    public string Description { get; }
}
