using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

/// <summary>
/// Decision telemetry for ALL tactics — the generalization of the bandit trap
/// log. Every ~2s it records, per team: the ACTIVE tactic (RF doctrine or
/// vanilla) and, per formation, the ACTIVE behavior next to the top weighted
/// candidates (effective weight = GetAiWeight × WeightFactor). The bandit logs
/// proved vanilla behaviors can OUTBID what a tactic assigned and silently
/// hijack formations; this file makes that visible for the other 12 doctrines
/// before any of them gets tuned.
/// Log: Documents\Mount and Blade II Bannerlord\Configs\ModLogs\RF_BattleAI_tactics.log
/// </summary>
internal static class BattleAITacticTelemetry
{
    /// <summary>OFF by default: this telemetry invokes GetAiWeight() by
    /// reflection to rank behavior candidates, and some vanilla implementations
    /// (e.g. BehaviorScreenedSkirmish.GetAiWeight) have SIDE EFFECTS — they call
    /// CalculateCurrentOrder and mutate inactive behaviors' state. Only enable
    /// it for a deliberate hijack-audit session, never in normal play.</summary>
    public static bool Enabled => BattleAILogSwitches.Tactics;

    private const float LogInterval = 2f;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_BattleAI_tactics.log");

    private static readonly FieldInfo CurrentTacticField = AccessTools.Field(typeof(TeamAIComponent), "_currentTactic");
    private static readonly FieldInfo BehaviorsField = AccessTools.Field(typeof(FormationAI), "_behaviors");
    private static readonly MethodInfo GetAiWeightMethod = AccessTools.Method(typeof(BehaviorComponent), "GetAiWeight");

    private static Mission? _mission;
    private static float _nextLogTime;

    public static void Tick()
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            Mission? mission = Mission.Current;
            if (mission == null || mission.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
            {
                return;
            }

            if (!ReferenceEquals(_mission, mission))
            {
                _mission = mission;
                _nextLogTime = 0f;
                Append($"\n===== NEW BATTLE {DateTime.Now:HH:mm:ss} =====");
            }

            if (mission.CurrentTime < _nextLogTime)
            {
                return;
            }

            _nextLogTime = mission.CurrentTime + LogInterval;

            StringBuilder sb = new();
            foreach (Team team in mission.Teams)
            {
                if (team?.HasTeamAi != true)
                {
                    continue;
                }

                TacticComponent? tactic = CurrentTacticField?.GetValue(team.TeamAI) as TacticComponent;
                string tacticName = tactic?.GetType().Name ?? "<none>";
                bool isManaged = tactic != null && tactic.GetType().Namespace?.StartsWith("RF_BattleAI", StringComparison.Ordinal) == true;
                sb.AppendLine($"[t={mission.CurrentTime,6:F1}] team={team.Side}{(team.IsPlayerTeam ? "/player" : "")} "
                    + $"tactic={tacticName}{(isManaged ? " (RF)" : "")} pw={team.QuerySystem.RemainingPowerRatio:F2}");

                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0 || formation.AI == null)
                    {
                        continue;
                    }

                    string troopClass = formation.QuerySystem.IsRangedCavalryFormation ? "HA "
                        : formation.QuerySystem.IsCavalryFormation ? "CAV"
                        : formation.QuerySystem.IsRangedFormation ? "ARC"
                        : "INF";
                    string controller = formation.IsAIControlled ? "" : " PLAYER";
                    string active = StripBehaviorPrefix(formation.AI.ActiveBehavior?.GetType().Name ?? "<none>");
                    sb.AppendLine($"  F{(int)formation.FormationIndex} [{troopClass}]{controller} n={formation.CountOfUnits} "
                        + $"pow={formation.QuerySystem.FormationPower:F0} ACTIVE={active} | {DescribeTopBehaviors(formation)}");
                }
            }

            if (sb.Length > 0)
            {
                Append(sb.ToString());
            }
        }
        catch
        {
            // Telemetry must never take a battle down with it.
        }
    }

    /// <summary>Top-3 candidates by EFFECTIVE weight (aiWeight × factor). A
    /// formation whose ACTIVE behavior is not the one its tactic boosted
    /// (factor > 1) is being hijacked by the weight competition.</summary>
    private static string DescribeTopBehaviors(Formation formation)
    {
        if (BehaviorsField?.GetValue(formation.AI) is not List<BehaviorComponent> behaviors)
        {
            return "?";
        }

        List<(string Name, float Effective, float Factor)> scored = new();
        foreach (BehaviorComponent behavior in behaviors)
        {
            float factor = behavior.WeightFactor;
            float aiWeight;
            try
            {
                aiWeight = (float)GetAiWeightMethod.Invoke(behavior, null);
            }
            catch
            {
                continue;
            }

            float effective = aiWeight * factor;
            if (effective > 0.01f)
            {
                scored.Add((StripBehaviorPrefix(behavior.GetType().Name), effective, factor));
            }
        }

        scored.Sort((a, b) => b.Effective.CompareTo(a.Effective));
        StringBuilder sb = new("top:");
        for (int i = 0; i < scored.Count && i < 3; i++)
        {
            sb.Append($" {scored[i].Name}={scored[i].Effective:F2}(f{scored[i].Factor:F1})");
        }

        return scored.Count > 0 ? sb.ToString() : "top: <all zero>";
    }

    private static string StripBehaviorPrefix(string name)
    {
        return name.StartsWith("Behavior", StringComparison.Ordinal) ? name.Substring(8) : name;
    }

    private static void Append(string text)
    {
        try
        {
            string? directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(LogPath, text + "\n");
        }
        catch
        {
        }
    }
}
