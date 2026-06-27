using System;
using RF_BattleAI.FieldBattle.Tactics;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAITacticController
{
    private const float ManualDoctrineLockSeconds = 45f;
    private const float FlankLockSeconds = 45f;
    private const float ManualDoctrineWeightBonus = 100f;

    private static readonly Type[] ManagedTacticTypes =
    {
        typeof(TacticBanditAdaptiveSkirmish),
        typeof(TacticAntiCavalryBrace),
        typeof(TacticBaitAndPounce),
        typeof(TacticCannaeEnvelopment),
        typeof(TacticElasticDefense),
        typeof(TacticFeignedRetreat),
        typeof(TacticHammerAndAnvil),
        typeof(TacticInfantryWaves),
        typeof(TacticMissileScreen),
        typeof(TacticObliqueOrder),
        typeof(TacticRefusedFlank),
        typeof(TacticReserveCounterattack),
        typeof(TacticShieldwallAdvance)
    };

    private static string? _playerDoctrineOverrideId;
    private static float _playerDoctrineOverrideUntilTime;
    private static Mission? _trackedMission;
    private static readonly System.Collections.Generic.Dictionary<Team, FlankLockState> FlankLocks = new();

    public static void ResetPlayerOverride()
    {
        ResetTransientStateIfMissionChanged();
        _playerDoctrineOverrideId = null;
        _playerDoctrineOverrideUntilTime = 0f;
    }

    public static void SetPlayerOverride(string? doctrineId)
    {
        ResetTransientStateIfMissionChanged();
        _playerDoctrineOverrideId = doctrineId;
        _playerDoctrineOverrideUntilTime = doctrineId == null ? 0f : GetMissionTime() + ManualDoctrineLockSeconds;
    }

    public static bool TryGetLockedFlankPreference(Team team, out bool favorLeft)
    {
        ResetTransientStateIfMissionChanged();

        if (FlankLocks.TryGetValue(team, out FlankLockState? state) && GetMissionTime() <= state.UntilTime)
        {
            favorLeft = state.FavorLeft;
            return true;
        }

        FlankLocks.Remove(team);
        favorLeft = false;
        return false;
    }

    public static void LockFlankPreference(Team team, bool favorLeft)
    {
        ResetTransientStateIfMissionChanged();
        FlankLocks[team] = new FlankLockState(favorLeft, GetMissionTime() + FlankLockSeconds);
    }

    public static void ApplyDoctrine(Team team)
    {
        BattleAIRuntimeTracer.NoteDoctrineRequest(team, GetDoctrineRequestLabel(team));

        if (!team.HasTeamAi || team.TeamAI is not TeamAIGeneral)
        {
            BattleAIRuntimeTracer.NoteDoctrineSkipped(team, team.HasTeamAi ? "NonGeneralTeamAI" : "NoTeamAi");
            return;
        }

        RemoveManagedTactics(team);

        if (team.IsPlayerTeam && !IsPlayerDoctrineOverrideActive())
        {
            BattleAIRuntimeTracer.NoteDoctrineFallback(team);
            team.ResetTactic();
            return;
        }

        BattleAIDoctrineSelection? doctrineSelection = GetDoctrineSelection(team, out string diagnostics);
        if (doctrineSelection != null)
        {
            team.AddTacticOption(doctrineSelection.Tactic);
            BattleAIDebug.DoctrineSelected(team, doctrineSelection.DoctrineId, doctrineSelection.CommanderTacticsSkill, doctrineSelection.Score, diagnostics);
            BattleAIDebug.TacticRegistered(team, doctrineSelection.DoctrineId);
        }
        else
        {
            int effectiveTactics = BattleAISergeantDoctrineAdvisor.AnalyzeCommander(team)
                .GetEffectiveTacticsSkill(BattleAICombatantHelper.GetCommanderTacticsSkill(team));
            BattleAIDebug.DoctrineFallbackToVanilla(team, effectiveTactics, diagnostics);
        }

        team.ResetTactic();
    }

    public static float ApplyManualWeightBonus(Team team, string doctrineId, float weight)
    {
        if (!HasManualDoctrinePriority(team, doctrineId))
        {
            return weight;
        }

        return weight + ManualDoctrineWeightBonus;
    }

    public static bool HasManualDoctrineOverride(Team team, string doctrineId)
    {
        return HasManualDoctrinePriority(team, doctrineId);
    }

    private static BattleAIDoctrineSelection? GetDoctrineSelection(Team team, out string diagnostics)
    {
        ResetTransientStateIfMissionChanged();

        if (team.IsPlayerTeam)
        {
            if (!IsPlayerDoctrineOverrideActive())
            {
                _playerDoctrineOverrideId = null;
            }

            string? playerDoctrineOverrideId = _playerDoctrineOverrideId;
            if (playerDoctrineOverrideId is { Length: > 0 } doctrineId)
            {
                BattleAIDoctrineSelection? manualSelection = BattleAIDoctrineSelector.SelectDoctrineById(team, doctrineId, manualRequest: true, out diagnostics);
                if (manualSelection != null)
                {
                    BattleAIDebug.PlayerDoctrineChanged(team, manualSelection.DoctrineId, manual: true);
                    return manualSelection;
                }

                BattleAIDebug.PlayerDoctrineUnavailable(team, doctrineId, diagnostics);
            }
        }

        return BattleAIDoctrineSelector.SelectDoctrine(team, out diagnostics);
    }

    private static bool HasManualDoctrinePriority(Team team, string doctrineId)
    {
        return team.IsPlayerTeam
            && IsPlayerDoctrineOverrideActive()
            && string.Equals(_playerDoctrineOverrideId, doctrineId, StringComparison.Ordinal);
    }

    private static bool IsPlayerDoctrineOverrideActive()
    {
        return _playerDoctrineOverrideId is { Length: > 0 } && GetMissionTime() <= _playerDoctrineOverrideUntilTime;
    }

    private static string GetDoctrineRequestLabel(Team team)
    {
        if (!team.IsPlayerTeam)
        {
            return "EnemyAuto";
        }

        return IsPlayerDoctrineOverrideActive() && _playerDoctrineOverrideId is { Length: > 0 } doctrineId
            ? doctrineId
            : "PlayerAuto";
    }

    private static float GetMissionTime()
    {
        return Mission.Current?.CurrentTime ?? 0f;
    }

    private static void ResetTransientStateIfMissionChanged()
    {
        Mission? mission = Mission.Current;
        if (ReferenceEquals(_trackedMission, mission))
        {
            return;
        }

        _trackedMission = mission;
        FlankLocks.Clear();
        _playerDoctrineOverrideId = null;
        _playerDoctrineOverrideUntilTime = 0f;
    }

    private static void RemoveManagedTactics(Team team)
    {
        foreach (Type tacticType in ManagedTacticTypes)
        {
            team.RemoveTacticOption(tacticType);
        }
    }

    private sealed class FlankLockState
    {
        public FlankLockState(bool favorLeft, float untilTime)
        {
            FavorLeft = favorLeft;
            UntilTime = untilTime;
        }

        public bool FavorLeft { get; }

        public float UntilTime { get; }
    }
}
