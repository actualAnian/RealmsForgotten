using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAIPlayerHotkeyController
{
    private static Mission? _lastMission;
    private static bool _controlsShown;

    public static void Tick()
    {
        Mission? mission = Mission.Current;
        if (!ReferenceEquals(_lastMission, mission))
        {
            _lastMission = mission;
            _controlsShown = false;
            BattleAITacticController.ResetPlayerOverride();
        }

        if (mission == null || mission.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
        {
            return;
        }

        Team? playerTeam = mission.PlayerTeam;
        if (playerTeam == null || !playerTeam.HasTeamAi || !playerTeam.IsPlayerGeneral)
        {
            return;
        }

        if (!_controlsShown)
        {
            _controlsShown = true;
            BattleAIDebug.PlayerDoctrineHotkeysReady();
        }

        if (TryHandleSelection(InputKey.Numpad0, null, playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad1, nameof(FieldBattle.Tactics.TacticShieldwallAdvance), playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad2, nameof(FieldBattle.Tactics.TacticHammerAndAnvil), playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad3, nameof(FieldBattle.Tactics.TacticObliqueOrder), playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad4, nameof(FieldBattle.Tactics.TacticFeignedRetreat), playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad5, nameof(FieldBattle.Tactics.TacticRefusedFlank), playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad6, nameof(FieldBattle.Tactics.TacticCannaeEnvelopment), playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad7, nameof(FieldBattle.Tactics.TacticElasticDefense), playerTeam))
        {
            return;
        }

        if (TryHandleSelection(InputKey.Numpad8, nameof(FieldBattle.Tactics.TacticReserveCounterattack), playerTeam))
        {
            return;
        }

        TryHandleSelection(InputKey.Numpad9, nameof(FieldBattle.Tactics.TacticBanditAdaptiveSkirmish), playerTeam);
    }

    private static bool TryHandleSelection(InputKey key, string? doctrineId, Team playerTeam)
    {
        if (!Input.IsKeyPressed(key))
        {
            return false;
        }

        EnablePlayerArmyAiControl(playerTeam);
        BattleAITacticController.SetPlayerOverride(doctrineId);
        if (doctrineId == null)
        {
            BattleAIDebug.PlayerDoctrineChanged(playerTeam, "Auto", manual: false);
        }

        BattleAITacticController.ApplyDoctrine(playerTeam);
        BattleAIRuntimeTracer.BeginPlayerTrace(playerTeam, doctrineId ?? "Auto");
        return true;
    }

    private static void EnablePlayerArmyAiControl(Team playerTeam)
    {
        playerTeam.DelegateCommandToAI();
        playerTeam.PlayerOrderController?.ClearSelectedFormations();
        BattleAIDebug.PlayerAiControlEnabled(playerTeam);
    }
}
