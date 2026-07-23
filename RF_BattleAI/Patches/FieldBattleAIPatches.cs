using System;
using System.Reflection;
using HarmonyLib;
using RF_BattleAI.FieldBattle.Behaviors;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.Patches;

/// <summary>
/// Single authority for ONE invariant:
///
///   A formation that has units always carries (a) the vanilla behavior set —
///   when its team has an AI — and (b) the RF custom behaviors, BEFORE
///   anything calls FormationAI.SetBehaviorWeight.
///
/// Why the invariant breaks (each learned from a real, reproduced crash):
///   1. Vanilla only installs its behavior set when a formation goes from 0 to
///      1 units AND Team.TeamAI is already assigned. Quest missions spawn
///      agents first and wire the team AI afterwards → populated formations
///      with an empty behavior list.
///   2. Formation.ResetAux() — deployment machinery — replaces Formation.AI
///      with a brand-new FormationAI, silently wiping every installed behavior
///      while the units stay put.
///   3. RF tactics use custom behaviors vanilla never installs.
///
/// In all three cases the next SetBehaviorWeight&lt;T&gt; — vanilla's, RBM's or
/// ours — throws MBException "Behavior weight could not be set".
///
/// The repair runs at the three choke points listed at the bottom of this file
/// and is cheap: two sentinel lookups per formation; the full install only
/// runs when a sentinel is missing. CountOfUnits &gt; 0 is a hard gate — the
/// vanilla installer's ForceCalculateCaches reaches native code and
/// access-violates on empty formations during mission setup.
/// </summary>
internal static class FormationBehaviorGuardian
{
    public static void EnsureAllTeams(Mission? mission)
    {
        if (mission?.Teams == null)
        {
            return;
        }
        foreach (Team team in mission.Teams)
        {
            EnsureTeam(team);
        }
    }

    public static void EnsureTeam(Team? team)
    {
        if (team == null)
        {
            return;
        }
        foreach (Formation formation in team.FormationsIncludingSpecialAndEmpty)
        {
            EnsureFormation(formation);
        }
    }

    public static void EnsureFormation(Formation? formation)
    {
        if (formation?.AI == null || formation.CountOfUnits <= 0)
        {
            return;
        }

        // Sentinel 1: BehaviorCharge is always the first entry of the vanilla
        // set. Missing on a populated formation = the whole set is gone;
        // re-run the game's own installer so the list matches the running
        // game version exactly.
        if (formation.Team?.TeamAI != null && formation.AI.GetBehavior<BehaviorCharge>() == null)
        {
            formation.Team.TeamAI.OnUnitAddedToFormationForTheFirstTime(formation);
            BattleAIDebug.BehaviorRegistered(formation, "VanillaDefaultSet(reinstalled)");
        }

        // Sentinel 2: BehaviorMaintainReserve is always the first RF custom
        // behavior installed, so its absence means all customs are missing.
        // Customs only exist where RF tactics can run — TeamAIGeneral teams
        // (field battles, custom battle). Siege/sally-out teams keep a pure
        // vanilla behavior list.
        if (formation.Team?.TeamAI is TeamAIGeneral
            && formation.AI.GetBehavior<BehaviorMaintainReserve>() == null)
        {
            InstallCustomBehaviors(formation);
        }
    }

    private static void InstallCustomBehaviors(Formation formation)
    {
        EnsureBehavior(formation, f => new BehaviorMaintainReserve(f)); // sentinel — keep first
        EnsureBehavior(formation, f => new BehaviorFallbackLine(f));
        EnsureBehavior(formation, f => new BehaviorScreenArchers(f));
        EnsureBehavior(formation, f => new BehaviorCavalryReposition(f));
        EnsureBehavior(formation, f => new BehaviorCavalryBreakthrough(f));
        EnsureBehavior(formation, f => new BehaviorRearPincer(f));
        EnsureBehavior(formation, f => new BehaviorBaitLure(f));
        EnsureBehavior(formation, f => new BehaviorDirectedCharge(f));
        EnsureBehavior(formation, f => new BehaviorSwatPursuers(f));
        EnsureBehavior(formation, f => new BehaviorMountedFiringArc(f));
        EnsureBehavior(formation, f => new BehaviorCavalryWaveCharge(f));
        // Vanilla type, but required by the RF bandit rout even on teams whose
        // installer never ran (TeamAI-less missions).
        EnsureBehavior(formation, f => new BehaviorRetreat(f));
    }

    private static void EnsureBehavior<T>(Formation formation, Func<Formation, T> create) where T : BehaviorComponent
    {
        if (formation.AI.GetBehavior<T>() == null)
        {
            formation.AI.AddAiBehavior(create(formation));
            BattleAIDebug.BehaviorRegistered(formation, typeof(T).Name);
        }
    }
}

/// <summary>
/// TeamAIComponent.Team is a protected readonly field in the shipped
/// assemblies. Resolved lazily; accepts field or property; never throws — a
/// failed resolution disables the repair instead of crashing the patch.
/// </summary>
internal static class TeamAIComponentTeamAccess
{
    private static bool _resolved;
    private static FieldInfo? _field;
    private static MethodInfo? _propertyGetter;

    public static Team? Get(TeamAIComponent component)
    {
        if (component == null)
        {
            return null;
        }
        if (!_resolved)
        {
            _resolved = true;
            _field = AccessTools.Field(typeof(TeamAIComponent), "Team");
            if (_field == null)
            {
                _propertyGetter = AccessTools.PropertyGetter(typeof(TeamAIComponent), "Team");
            }
        }
        try
        {
            if (_field != null)
            {
                return _field.GetValue(component) as Team;
            }
            if (_propertyGetter != null)
            {
                return _propertyGetter.Invoke(component, null) as Team;
            }
        }
        catch
        {
            // shape changed in a future game version — repair disabled
        }
        return null;
    }
}

// ---------------------------------------------------------------------------
// Choke points. Every code path that sets behavior weights goes through
// exactly one of these three, so repairing here covers vanilla, RBM and RF:
//
//   1. TeamAIComponent.Tick — the per-frame funnel that runs MakeDecision /
//      TickOccasionally on its timers and itself sets the bodyguard's
//      BehaviorCharge weight unguarded. TeamAISiegeComponent.Tick ends with
//      base.Tick(dt), so siege teams are covered by the same patch.
//   2. TeamAIComponent.ResetTactic — called directly by deployment-finish
//      code, runs MakeDecision/TickOccasionally immediately, outside Tick.
//   3. MissionState.OnTick — where the RF battle systems themselves run;
//      the sweep precedes them so they always see complete behavior lists.
//   4. TeamAIGeneral.OnUnitAddedToFormationForTheFirstTime — the 0→1 install
//      point itself. Tactics (ours and RBM's) repopulate empty formations in
//      the MIDDLE of their own tick and set weights immediately; sweeps 1-3
//      already ran, so the customs must piggyback on the vanilla installer
//      the moment it fires. Deleting this hook as "redundant" caused a real
//      MBException in TacticHammerAndAnvil.ApplyCavalryAssembly.
//
// Doctrine bootstrap (5) is the one hook that is not about the invariant: it
// assigns RF tactic doctrines to field-battle teams at mission start.
// ---------------------------------------------------------------------------

[HarmonyPatch(typeof(TeamAIComponent), "Tick")]
internal static class TeamAIComponent_Tick_EnsureBehaviorsPatch
{
    private static void Prefix(TeamAIComponent __instance)
    {
        FormationBehaviorGuardian.EnsureTeam(TeamAIComponentTeamAccess.Get(__instance));
    }
}

[HarmonyPatch(typeof(TeamAIComponent), "ResetTactic")]
internal static class TeamAIComponent_ResetTactic_EnsureBehaviorsPatch
{
    private static void Prefix(TeamAIComponent __instance)
    {
        FormationBehaviorGuardian.EnsureTeam(TeamAIComponentTeamAccess.Get(__instance));
    }
}

[HarmonyPatch(typeof(TeamAIGeneral), "OnUnitAddedToFormationForTheFirstTime")]
internal static class TeamAIGeneral_OnUnitAddedToFormationForTheFirstTime_EnsureBehaviorsPatch
{
    private static void Postfix(Formation formation)
    {
        // Runs after the vanilla body, so BehaviorCharge is already present
        // and EnsureFormation only tops up the RF customs — re-entry through
        // the sentinel-1 reinstall path therefore terminates immediately.
        FormationBehaviorGuardian.EnsureFormation(formation);
    }
}

[HarmonyPatch(typeof(MissionState), "OnTick")]
internal static class MissionState_OnTick_BattleAISystemsPatch
{
    private static void Postfix()
    {
        FormationBehaviorGuardian.EnsureAllTeams(Mission.Current);
        BattleAITacticController.TickPostSpawnDoctrineReapply();
        BattleAIPlayerHotkeyController.Tick();
        PlayerSpearFormationSplitter.Tick();
        BattleAICavalryStabilizer.Tick();
        BattleAIAdaptiveMemory.Tick();
        BattleAIFormationCaptainTuner.Tick();
        BattleAIRuntimeTracer.Tick();
        BattleAITacticTelemetry.Tick();
    }
}

[HarmonyPatch(typeof(MissionCombatantsLogic), "EarlyStart")]
internal static class MissionCombatantsLogic_EarlyStart_DoctrineBootstrapPatch
{
    private static void Postfix()
    {
        if (Mission.Current == null || Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
        {
            return;
        }
        foreach (Team team in Mission.Current.Teams)
        {
            BattleAITacticController.ApplyDoctrine(team);
        }
    }
}
