using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAIRuntimeTracer
{
    private static readonly string[] LogPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_BattleAI_RuntimeTrace.log"),
        Path.Combine(BasePath.Name, "Configs", "ModLogs", "RF_BattleAI_RuntimeTrace.log"),
        Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", "RF_BattleAI_RuntimeTrace.log")
    };

    private static Mission? _trackedMission;
    private static Mission? _stateMission;
    private static Team? _trackedTeam;
    private static Team? _trackedEnemyTeam;
    private static float _traceUntilTime;
    private static float _nextSnapshotTime;
    private static string _traceLabel = "Auto";
    private static readonly Dictionary<Team, string> DoctrineRequests = new();
    private static readonly Dictionary<Team, string> AppliedDoctrines = new();
    private static readonly Dictionary<Team, string> LastStates = new();
    private static readonly Dictionary<Team, string> LastStateDetails = new();

    public static void Tick()
    {
        Mission? mission = Mission.Current;
        if (!ReferenceEquals(_trackedMission, mission))
        {
            Reset();
        }

        if (mission == null || _trackedTeam == null || mission.CurrentTime > _traceUntilTime)
        {
            return;
        }

        if (mission.CurrentTime < _nextSnapshotTime)
        {
            return;
        }

        _nextSnapshotTime = mission.CurrentTime + 2f;
        LogSnapshot(_trackedTeam, _traceLabel, mission.CurrentTime);
        if (_trackedEnemyTeam != null)
        {
            LogSnapshot(_trackedEnemyTeam, "EnemyAuto", mission.CurrentTime);
        }
    }

    public static void BeginPlayerTrace(Team playerTeam, string traceLabel)
    {
        Mission? mission = Mission.Current;
        if (mission == null)
        {
            return;
        }

        EnsureMissionState(mission);
        _trackedMission = mission;
        _trackedTeam = playerTeam;
        _trackedEnemyTeam = mission.Teams.FirstOrDefault(team => team.Side != playerTeam.Side && team.HasTeamAi);
        _traceLabel = string.IsNullOrWhiteSpace(traceLabel) ? "Auto" : traceLabel;
        _traceUntilTime = mission.CurrentTime + 20f;
        _nextSnapshotTime = mission.CurrentTime;

        DoctrineRequests[playerTeam] = _traceLabel;
        Log($"=== begin trace | team={DescribeTeam(playerTeam)} | enemy={DescribeTeam(_trackedEnemyTeam)} | doctrine={_traceLabel} | t={mission.CurrentTime:F1} ===");
    }

    public static void NoteEvent(string message)
    {
        if (_trackedMission == null)
        {
            return;
        }

        Log(message);
    }

    public static void NoteDoctrineApplied(Team team, string doctrineName)
    {
        EnsureMissionState(Mission.Current);
        AppliedDoctrines[team] = doctrineName;
        LastStates[team] = $"{doctrineName}:Pending";
        LastStateDetails[team] = string.Empty;
    }

    public static void NoteDoctrineFallback(Team team)
    {
        EnsureMissionState(Mission.Current);
        AppliedDoctrines[team] = "Vanilla";
        LastStates[team] = "Vanilla:Pending";
        LastStateDetails[team] = string.Empty;
    }

    public static void NoteDoctrineRequest(Team team, string requestLabel)
    {
        EnsureMissionState(Mission.Current);
        DoctrineRequests[team] = requestLabel;
    }

    public static void NoteDoctrineSkipped(Team team, string reason)
    {
        EnsureMissionState(Mission.Current);
        AppliedDoctrines[team] = $"Skipped({reason})";
        LastStates[team] = $"Skipped:{reason}";
        LastStateDetails[team] = string.Empty;
    }

    public static void NoteTacticState(Team team, string tacticName, string stateName, string? details)
    {
        EnsureMissionState(Mission.Current);
        LastStates[team] = $"{tacticName}:{stateName}";
        LastStateDetails[team] = details ?? string.Empty;
    }

    private static void LogSnapshot(Team team, string traceLabel, float missionTime)
    {
        EnsureMissionState(Mission.Current);
        BattleAITerrainAssessment terrain = BattleAITerrainAnalyzer.Analyze(team);
        StringBuilder sb = new();
        sb.Append("snapshot");
        sb.Append(" | t=").Append(missionTime.ToString("F1"));
        sb.Append(" | team=").Append(DescribeTeam(team));
        sb.Append(" | doctrine_request=").Append(GetDoctrineRequest(team, traceLabel));
        sb.Append(" | doctrine_applied=").Append(GetAppliedDoctrine(team));
        sb.Append(" | state=").Append(GetLastState(team));
        int effectiveTactics = BattleAISergeantDoctrineAdvisor.AnalyzeCommander(team)
            .GetEffectiveTacticsSkill(BattleAICombatantHelper.GetCommanderTacticsSkill(team));
        sb.Append(" | tactics=").Append(effectiveTactics);
        sb.Append(" | power=").Append(team.QuerySystem.RemainingPowerRatio.ToString("F2"));
        sb.Append(" | inf=").Append(team.QuerySystem.InfantryRatio.ToString("F2"));
        sb.Append(" | arch=").Append(team.QuerySystem.RangedRatio.ToString("F2"));
        sb.Append(" | cav=").Append(team.QuerySystem.CavalryRatio.ToString("F2"));
        sb.Append(" | horseArch=").Append(team.QuerySystem.RangedCavalryRatio.ToString("F2"));
        sb.Append(" | enemyCav=").Append((team.QuerySystem.EnemyCavalryRatio + team.QuerySystem.EnemyRangedCavalryRatio).ToString("F2"));
        sb.Append(" | terrain=").Append(terrain.Describe());
        sb.Append(" | distance=").Append(GetEngagementDistance(team).ToString("F1"));
        sb.Append(" | memory=").Append(BattleAIAdaptiveMemory.DescribeLiveState(team));
        Log(sb.ToString());

        string lastStateDetails = GetLastStateDetails(team);
        if (lastStateDetails.Length > 0)
        {
            Log("  state_details " + lastStateDetails);
        }

        foreach (Formation formation in team.FormationsIncludingEmpty.Where(f => f.CountOfUnits > 0).OrderBy(f => (int)f.FormationIndex))
        {
            string troopClass = DescribeFormationClass(formation);
            string activeBehavior = formation.AI?.ActiveBehavior?.GetType().Name ?? "None";
            string movement = formation.GetReadonlyMovementOrderReference().OrderType.ToString();
            string arrangement = formation.ArrangementOrder.OrderType.ToString();
            string form = formation.FormOrder.OrderType.ToString();
            string firing = formation.FiringOrder.OrderType.ToString();
            string side = formation.AI?.Side.ToString() ?? "None";
            SergeantProfile? captainProfile = BattleAISergeantDoctrineAdvisor.AnalyzeAnyFormationCaptain(formation);
            string captainText = captainProfile == null
                ? "none"
                : $"{captainProfile.Name}:{captainProfile.Role}:t{captainProfile.TacticsSkill}";

            Log(
                $"  F{(int)formation.FormationIndex} {troopClass} units={formation.CountOfUnits} ai={formation.IsAIControlled} side={side} behavior={activeBehavior} move={movement} arrange={arrangement} form={form} fire={firing} captain={captainText}");
        }
    }

    private static string DescribeFormationClass(Formation formation)
    {
        if (formation.QuerySystem.IsInfantryFormation)
        {
            return "Inf";
        }

        if (formation.QuerySystem.IsRangedFormation)
        {
            return "Arch";
        }

        if (formation.QuerySystem.IsCavalryFormation)
        {
            return "Cav";
        }

        if (formation.QuerySystem.IsRangedCavalryFormation)
        {
            return "HorseArch";
        }

        return "Other";
    }

    private static string DescribeTeam(Team? team)
    {
        if (team == null)
        {
            return "?";
        }

        string side = team.Side switch
        {
            BattleSideEnum.Attacker => "Attacker",
            BattleSideEnum.Defender => "Defender",
            _ => $"Side{(int)team.Side}"
        };

        return team.IsPlayerTeam ? $"Player-{side}" : $"Enemy-{side}";
    }

    private static void Reset()
    {
        _trackedMission = null;
        _trackedTeam = null;
        _trackedEnemyTeam = null;
        _traceUntilTime = 0f;
        _nextSnapshotTime = 0f;
        _traceLabel = "Auto";
    }

    private static void EnsureMissionState(Mission? mission)
    {
        if (ReferenceEquals(_stateMission, mission))
        {
            return;
        }

        _stateMission = mission;
        DoctrineRequests.Clear();
        AppliedDoctrines.Clear();
        LastStates.Clear();
        LastStateDetails.Clear();
    }

    private static string GetDoctrineRequest(Team team, string fallback)
    {
        return DoctrineRequests.TryGetValue(team, out string? value) ? value : fallback;
    }

    private static string GetAppliedDoctrine(Team team)
    {
        return AppliedDoctrines.TryGetValue(team, out string? value) ? value : "Unknown";
    }

    private static string GetLastState(Team team)
    {
        return LastStates.TryGetValue(team, out string? value) ? value : "Unknown";
    }

    private static string GetLastStateDetails(Team team)
    {
        return LastStateDetails.TryGetValue(team, out string? value) ? value : string.Empty;
    }

    private static float GetEngagementDistance(Team team)
    {
        Formation? formation = team.FormationsIncludingEmpty
            .FirstOrDefault(f => f.CountOfUnits > 0 && f.CachedClosestEnemyFormation != null);
        if (formation?.CachedClosestEnemyFormation == null)
        {
            return -1f;
        }

        return formation.CachedMedianPosition.AsVec2.Distance(formation.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
    }

    private static void Log(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";

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
                // Keep tracing best-effort only.
            }
        }
    }
}
