using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAIDebug
{
    private static readonly Color InfoColor = Colors.Cyan;
    private static readonly Color StateColor = Colors.Yellow;

    public static bool Enabled { get; set; } = true;

    public static void ModuleLoaded()
    {
        Show("RF Battle AI loaded", InfoColor);
    }

    public static void TacticRegistered(Team team, string tacticName)
    {
        string message = $"RF Battle AI: team {DescribeTeam(team)} registered {tacticName}";
        BattleAIRuntimeTracer.NoteEvent(message);
        Show(message, InfoColor);
    }

    public static void DoctrineSelected(Team team, string doctrineName, int tacticsSkill, float score, string? details = null)
    {
        string message = $"RF Battle AI: team {DescribeTeam(team)} picked {doctrineName} | tactics={tacticsSkill} | score={score:F2}";
        BattleAIAdaptiveMemory.NoteDoctrineApplied(team, doctrineName);
        BattleAIRuntimeTracer.NoteDoctrineApplied(team, doctrineName);
        BattleAIRuntimeTracer.NoteEvent(message);
        if (!string.IsNullOrWhiteSpace(details))
        {
            BattleAIRuntimeTracer.NoteEvent("  doctrine_details " + details);
        }
        Show(message, InfoColor);
    }

    public static void DoctrineFallbackToVanilla(Team team, int tacticsSkill, string? details = null)
    {
        string message = $"RF Battle AI: team {DescribeTeam(team)} stays vanilla | tactics={tacticsSkill}";
        BattleAIRuntimeTracer.NoteDoctrineFallback(team);
        BattleAIRuntimeTracer.NoteEvent(message);
        if (!string.IsNullOrWhiteSpace(details))
        {
            BattleAIRuntimeTracer.NoteEvent("  doctrine_details " + details);
        }
        Show(message, InfoColor);
    }

    public static void BehaviorRegistered(Formation formation, string behaviorName)
    {
        string message = $"RF Battle AI: F{formation?.Index ?? -1} added {behaviorName}";
        BattleAIRuntimeTracer.NoteEvent(message);
        Show(message, InfoColor);
    }

    public static void TacticStateChanged(Team team, string tacticName, string stateName)
    {
        TacticStateChanged(team, tacticName, stateName, null);
    }

    public static void TacticStateChanged(Team team, string tacticName, string stateName, string? details)
    {
        string message = $"RF Battle AI: team {DescribeTeam(team)} -> {tacticName} / {stateName}";
        BattleAIRuntimeTracer.NoteTacticState(team, tacticName, stateName, details);
        BattleAIRuntimeTracer.NoteEvent(message);
        Show(message, StateColor);
    }

    public static void PlayerDoctrineHotkeysReady()
    {
        Show("RF Battle AI: Numpad1 Shieldwall | 2 Hammer | 3 Oblique | 4 Feigned | 5 Refused | 6 Cannae | 7 Elastic | 8 Reserve | 9 Bandit | 0 Auto", InfoColor);
    }

    public static void PlayerDoctrineChanged(Team team, string doctrineName, bool manual)
    {
        string source = manual ? "manual doctrine accepted" : "manual doctrine request";
        string message = $"RF Battle AI: team {DescribeTeam(team)} {source} -> {doctrineName}";
        BattleAIRuntimeTracer.NoteEvent(message);
        Show(message, InfoColor);
    }

    public static void PlayerAiControlEnabled(Team team)
    {
        string message = $"RF Battle AI: player command delegated to {DescribeTeam(team)} formations";
        BattleAIRuntimeTracer.NoteEvent(message);
        Show(message, InfoColor);
    }

    public static void PlayerDoctrineUnavailable(Team team, string doctrineName, string? reason = null)
    {
        string message = $"RF Battle AI: team {DescribeTeam(team)} cannot use {doctrineName}, staying on auto choice";
        BattleAIRuntimeTracer.NoteEvent(message);
        if (!string.IsNullOrWhiteSpace(reason))
        {
            BattleAIRuntimeTracer.NoteEvent("  doctrine_unavailable " + reason);
        }
        Show(message, StateColor);
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

        if (team.IsPlayerTeam)
        {
            return $"Player-{side}";
        }

        return $"Enemy-{side}";
    }

    private static void Show(string message, Color color)
    {
        if (!Enabled)
        {
            return;
        }

        InformationManager.DisplayMessage(new InformationMessage(message, color));
    }
}
