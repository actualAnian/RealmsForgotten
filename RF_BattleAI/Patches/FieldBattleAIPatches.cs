using HarmonyLib;
using RF_BattleAI.FieldBattle.Behaviors;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.Patches;

internal static class FieldBattleBehaviorRegistrar
{
    public static void RegisterCustomBehaviors(Formation? formation)
    {
        if (formation?.AI == null)
        {
            return;
        }

        if (formation.AI.GetBehavior<BehaviorMaintainReserve>() == null)
        {
            formation.AI.AddAiBehavior(new BehaviorMaintainReserve(formation));
            BattleAIDebug.BehaviorRegistered(formation, nameof(BehaviorMaintainReserve));
        }

        if (formation.AI.GetBehavior<BehaviorFallbackLine>() == null)
        {
            formation.AI.AddAiBehavior(new BehaviorFallbackLine(formation));
            BattleAIDebug.BehaviorRegistered(formation, nameof(BehaviorFallbackLine));
        }

        if (formation.AI.GetBehavior<BehaviorScreenArchers>() == null)
        {
            formation.AI.AddAiBehavior(new BehaviorScreenArchers(formation));
            BattleAIDebug.BehaviorRegistered(formation, nameof(BehaviorScreenArchers));
        }

        if (formation.AI.GetBehavior<BehaviorCavalryReposition>() == null)
        {
            formation.AI.AddAiBehavior(new BehaviorCavalryReposition(formation));
            BattleAIDebug.BehaviorRegistered(formation, nameof(BehaviorCavalryReposition));
        }

        if (formation.AI.GetBehavior<BehaviorCavalryBreakthrough>() == null)
        {
            formation.AI.AddAiBehavior(new BehaviorCavalryBreakthrough(formation));
            BattleAIDebug.BehaviorRegistered(formation, nameof(BehaviorCavalryBreakthrough));
        }
    }
}

[HarmonyPatch(typeof(MissionCombatantsLogic), "EarlyStart")]
internal static class MissionCombatantsLogic_EarlyStart_FieldBattlePatch
{
    private static void Postfix()
    {
        if (Mission.Current == null || Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
        {
            return;
        }

        foreach (Team team in Mission.Current.Teams)
        {
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                FieldBattleBehaviorRegistrar.RegisterCustomBehaviors(formation);
            }

            BattleAITacticController.ApplyDoctrine(team);
        }
    }
}

[HarmonyPatch(typeof(TeamAIGeneral), "OnUnitAddedToFormationForTheFirstTime")]
internal static class TeamAIGeneral_OnUnitAddedToFormationForTheFirstTime_FieldBattlePatch
{
    private static void Postfix(Formation formation)
    {
        FieldBattleBehaviorRegistrar.RegisterCustomBehaviors(formation);
    }
}

[HarmonyPatch(typeof(MissionState), "OnTick")]
internal static class MissionState_OnTick_BattleAIPlayerHotkeysPatch
{
    private static void Postfix()
    {
        BattleAIPlayerHotkeyController.Tick();
        BattleAICavalryStabilizer.Tick();
        BattleAIAdaptiveMemory.Tick();
        BattleAIFormationCaptainTuner.Tick();
        BattleAIRuntimeTracer.Tick();
    }
}
