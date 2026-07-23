using System;
using System.IO;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

/// <summary>
/// Decision telemetry for the bandit trap. Every ~2s of battle it records what
/// the tactic decided, what behavior the ENGINE actually activated on each
/// bandit formation (the tactic setting a weight does not guarantee the
/// behavior wins the selection), the power-gate numbers and the enemy army
/// layout — so failures are diagnosed from data instead of guesswork.
/// Log: Documents\Mount and Blade II Bannerlord\Configs\ModLogs\RF_BattleAI_trap.log
/// </summary>
internal static class BanditTrapTelemetry
{
    /// <summary>Diagnostic-only. Ships OFF: this writes to disk every 2 s of
    /// battle while the bandit trap runs — same convention as
    /// BattleAITacticTelemetry.Enabled. Flip on locally when investigating.</summary>
    private const bool Enabled = false;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_BattleAI_trap.log");

    private static Mission? _mission;
    private static float _nextLogTime;

    public static void LogDecision(
        Team team,
        string state,
        Formation? bait,
        Formation? support,
        Formation? chaser,
        string supportDecision,
        float chaserToBait,
        bool powerFavorable,
        string pursuerSensor = "")
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            Mission? mission = Mission.Current;
            if (mission == null)
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

            _nextLogTime = mission.CurrentTime + 2f;

            StringBuilder sb = new();
            sb.AppendLine($"[t={mission.CurrentTime,6:F1}] state={state} | supportDecision={supportDecision} | chaserToBait={(chaserToBait >= float.MaxValue ? -1f : chaserToBait):F0} | powerGate={powerFavorable}");
            sb.AppendLine($"  bait   : {Describe(bait)}");
            sb.AppendLine($"  support: {Describe(support)}");
            sb.AppendLine($"  chaser : {Describe(chaser)}");
            if (!string.IsNullOrEmpty(pursuerSensor))
            {
                // Agent-level pursuer sensor: enemies physically near each
                // bandit group regardless of which formation they belong to.
                sb.AppendLine($"  pursue : {pursuerSensor}");
            }

            // EVERY bandit formation with its class — cavalry and horse
            // archers were invisible when only bait/support were logged
            // (all-mounted bandit parties produced empty battles in the log).
            foreach (Formation ownFormation in team.FormationsIncludingEmpty)
            {
                if (ownFormation.CountOfUnits <= 0)
                {
                    continue;
                }

                string troopClass = ownFormation.QuerySystem.IsRangedCavalryFormation ? "HA "
                    : ownFormation.QuerySystem.IsCavalryFormation ? "CAV"
                    : ownFormation.QuerySystem.IsRangedFormation ? "ARC"
                    : "INF";
                sb.AppendLine($"  own    : [{troopClass}] {Describe(ownFormation)}");
            }

            // The enemy (player) side: what each formation is DOING and which
            // bandit group it is bearing down on — the decisions the bandits
            // reacted to are read off these lines.
            Vec2 baitPosition = bait?.CachedMedianPosition.AsVec2 ?? Vec2.Invalid;
            Vec2 supportPosition = support?.CachedMedianPosition.AsVec2 ?? Vec2.Invalid;
            foreach (Team otherTeam in mission.Teams)
            {
                if (otherTeam == null || !otherTeam.IsEnemyOf(team))
                {
                    continue;
                }

                foreach (Formation enemyFormation in otherTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0)
                    {
                        continue;
                    }

                    Vec2 enemyPosition = enemyFormation.CachedMedianPosition.AsVec2;
                    float toBait = baitPosition.IsValid ? enemyPosition.Distance(baitPosition) : -1f;
                    float toSupport = supportPosition.IsValid ? enemyPosition.Distance(supportPosition) : -1f;
                    string order = DescribeOrder(enemyFormation);
                    sb.AppendLine($"  enemy  : {Describe(enemyFormation)} | order={order} | dBait={toBait:F0} dSupport={toSupport:F0}");
                }
            }

            Append(sb.ToString());
        }
        catch
        {
            // Telemetry must never take a battle down with it.
        }
    }

    private static string DescribeOrder(Formation formation)
    {
        try
        {
            string controller = formation.IsAIControlled ? "AI" : "PLAYER";
            string order = formation.GetReadonlyMovementOrderReference().OrderEnum.ToString();
            string target = formation.TargetFormation != null
                ? $"->F{(int)formation.TargetFormation.FormationIndex}"
                : "";
            return $"{controller}:{order}{target}";
        }
        catch
        {
            return "?";
        }
    }

    private static string Describe(Formation? formation)
    {
        if (formation == null)
        {
            return "<null>";
        }

        if (formation.CountOfUnits <= 0)
        {
            return $"F{(int)formation.FormationIndex} <empty>";
        }

        Vec2 position = formation.CachedMedianPosition.AsVec2;
        string activeBehavior = formation.AI?.ActiveBehavior?.GetType().Name ?? "<none>";
        return $"F{(int)formation.FormationIndex} n={formation.CountOfUnits} pow={formation.QuerySystem.FormationPower:F0} "
            + $"pos=({position.x:F0},{position.y:F0}) ACTIVE={activeBehavior}";
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
