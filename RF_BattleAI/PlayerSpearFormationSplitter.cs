using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RF_BattleAI;

/// <summary>
/// Player-side spear split. Vanilla lumps spear and sword infantry into the same
/// Infantry formation, so the player can never select an anti-cavalry block on
/// its own. Pressing the hotkey (K) pulls every infantryman whose FIRST weapon
/// slot holds a polearm into the Heavy Infantry formation (formation "6"),
/// leaves the rest in Infantry ("1"), and hands the spear formation to the player
/// ALREADY SELECTED so a single move order (right-click) drops the wall wherever
/// they want it.
///
/// The key is idempotent, NOT a toggle: pressing it again never merges or shuffles
/// the troops around — it just re-grabs the spear formation for placement. That is
/// deliberate; the old toggle made the spears run across the field on a double-tap.
///
/// The slot-1 criterion is intentional: a troop XML opts a unit into the wall by
/// authoring its spear as the first weapon, and javelin throwers with a melee
/// polearm alternate mode stay out of it.
/// </summary>
internal static class PlayerSpearFormationSplitter
{
    // K is unbound in battle by default; P conflicts with the party screen.
    private const InputKey HotKey = InputKey.K;
    private const int MinimumSpearsForSplit = 5;

    // The spear formation is freshly populated the moment we split, so the order
    // UI may not have it registered as selectable on the same frame. Re-assert the
    // selection for a few ticks so it sticks without the player pressing "6".
    private const int ReselectTicks = 4;

    private static Mission? _lastMission;
    private static Formation? _spearFormation;
    private static int _reselectTicksRemaining;

    // MissionOrderVM lives on the Gauntlet order-UI handler as a protected field.
    private static readonly FieldInfo? OrderVmField =
        AccessTools.Field(typeof(GauntletOrderUIHandler), "_dataSource");

    public static void Tick()
    {
        Mission? mission = Mission.Current;
        if (!ReferenceEquals(_lastMission, mission))
        {
            _lastMission = mission;
            _spearFormation = null;
            _reselectTicksRemaining = 0;
        }

        if (mission == null
            || mission.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle
            || mission.Mode != MissionMode.Battle)
        {
            return;
        }

        Team? playerTeam = mission.PlayerTeam;
        if (playerTeam == null || !playerTeam.IsPlayerGeneral)
        {
            return;
        }

        // Retry the UI selection for a short window after a split, in case the
        // order UI had not registered the freshly populated formation on the same
        // frame. Stops as soon as the selection has actually stuck, so the order
        // menu is not re-opened over and over.
        if (_reselectTicksRemaining > 0)
        {
            if (_spearFormation != null
                && _spearFormation.CountOfUnits > 0
                && !IsAlreadySelected(playerTeam, _spearFormation))
            {
                SelectOnly(playerTeam, _spearFormation);
            }
            _reselectTicksRemaining--;
        }

        if (!Input.IsKeyPressed(HotKey))
        {
            return;
        }

        ApplySplit(playerTeam);
    }

    private static bool IsSpearCarrier(Agent agent)
    {
        MissionWeapon primarySlot = agent.Equipment[EquipmentIndex.WeaponItemBeginSlot];
        return !primarySlot.IsEmpty && primarySlot.Item?.PrimaryWeapon?.IsPolearm == true;
    }

    private static void ApplySplit(Team playerTeam)
    {
        List<Agent> spearCarriers = new();
        List<Agent> otherInfantry = new();

        // Re-scan every infantry-type formation (Infantry AND Heavy Infantry) so the
        // pass is idempotent: spears already sitting in the spear formation are
        // simply found in place, and reinforcements get pulled in on the next press.
        foreach (Formation formation in playerTeam.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits <= 0 || !formation.QuerySystem.IsInfantryFormation)
            {
                continue;
            }

            foreach (IFormationUnit unit in formation.Arrangement.GetAllUnits().Concat(formation.DetachedUnits).ToList())
            {
                if (unit is not Agent agent || !agent.IsActive() || agent.IsPlayerControlled)
                {
                    continue;
                }

                if (IsSpearCarrier(agent))
                {
                    spearCarriers.Add(agent);
                }
                else
                {
                    otherInfantry.Add(agent);
                }
            }
        }

        if (spearCarriers.Count < MinimumSpearsForSplit)
        {
            Announce($"Spear split: only {spearCarriers.Count} troops carry a spear in their first weapon slot — not enough to form a wall.");
            return;
        }

        if (otherInfantry.Count == 0)
        {
            Announce("Spear split: the entire infantry already carries spears.");
            return;
        }

        Formation swordFormation = playerTeam.GetFormation(FormationClass.Infantry);
        Formation spearFormation = playerTeam.GetFormation(FormationClass.HeavyInfantry);
        if (swordFormation == null || spearFormation == null)
        {
            return;
        }

        // Only move the troops that are not already in the right formation. If the
        // split is already in place this reassigns nobody, so a repeat press never
        // makes anyone run — it just falls through to re-selecting the wall.
        bool movedAny = false;

        foreach (Agent agent in spearCarriers)
        {
            if (agent.Formation != spearFormation)
            {
                agent.Formation = spearFormation;
                movedAny = true;
            }
        }

        foreach (Agent agent in otherInfantry)
        {
            if (agent.Formation != swordFormation)
            {
                agent.Formation = swordFormation;
                movedAny = true;
            }
        }

        // On the real split only, keep the freshly filled spear formation next to
        // the infantry line instead of letting formation VI snap to its default
        // deployment slot. We never position it ourselves on later presses — where
        // the wall goes is the player's call.
        if (movedAny && swordFormation.CountOfUnits > 0)
        {
            spearFormation.SetMovementOrder(MovementOrder.MovementOrderMove(swordFormation.CachedMedianPosition));
        }

        _spearFormation = spearFormation;
        SelectOnly(playerTeam, spearFormation);
        _reselectTicksRemaining = ReselectTicks;

        if (movedAny)
        {
            Announce($"Spear wall split: {spearFormation.CountOfUnits} spearmen pulled into their own formation and selected — give a move order (right-click) to place them. Press K again to re-select.");
        }
        else
        {
            Announce($"Spear wall selected: {spearFormation.CountOfUnits} spearmen ready — give a move order (right-click) to place them.");
        }
    }

    private static bool IsAlreadySelected(Team playerTeam, Formation formation)
    {
        MBReadOnlyList<Formation>? selected = playerTeam.PlayerOrderController?.SelectedFormations;
        return selected != null && selected.Count == 1 && selected.Contains(formation);
    }

    /// <summary>
    /// Selects the formation through the SAME entry point the formation number
    /// keys use — MissionOrderVM.OnTroopFormationSelected — so the full order UI
    /// reacts: selection markers appear over the troops, the order bar opens, and
    /// the next right-click places the movement flag. Selecting only on the
    /// backend OrderController does none of that (the UI never notices).
    /// </summary>
    private static void SelectOnly(Team playerTeam, Formation formation)
    {
        GauntletOrderUIHandler? handler = Mission.Current?.GetMissionBehavior<GauntletOrderUIHandler>();
        if (handler != null && OrderVmField?.GetValue(handler) is MissionOrderVM orderVm)
        {
            orderVm.OnTroopFormationSelected(formation.Index);
            return;
        }

        // No Gauntlet order UI found — backend-only selection as a last resort.
        OrderController orderController = playerTeam.PlayerOrderController;
        if (orderController != null)
        {
            orderController.ClearSelectedFormations();
            orderController.SelectFormation(formation);
        }
    }

    private static void Announce(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(0xFFB8D8F8u)));
    }
}
