using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

/// <summary>
/// Player-side spear split. Vanilla lumps spear and sword infantry into the same
/// Infantry formation, so the player can never select an anti-cavalry block with
/// the formation number keys. Pressing the hotkey (P) splits the player team's
/// infantry: troops whose FIRST weapon slot holds a polearm move to the Heavy
/// Infantry formation (selected with the "6" key), everyone else stays in
/// Infantry ("1"). Pressing it again merges them back.
///
/// The slot-1 criterion is deliberate: troop XMLs can opt a unit into the spear
/// wall simply by authoring its spear as the first weapon, and javelin throwers
/// with a melee-polearm alternate mode stay out of it.
/// </summary>
internal static class PlayerSpearFormationSplitter
{
    // K is unbound in battle by default; P conflicts with the party screen.
    private const InputKey ToggleKey = InputKey.K;
    private const int MinimumSpearsForSplit = 5;

    private static Mission? _lastMission;
    private static bool _splitActive;

    public static void Tick()
    {
        Mission? mission = Mission.Current;
        if (!ReferenceEquals(_lastMission, mission))
        {
            _lastMission = mission;
            _splitActive = false;
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

        if (!Input.IsKeyPressed(ToggleKey))
        {
            return;
        }

        if (_splitActive)
        {
            MergeBack(playerTeam);
        }
        else
        {
            ApplySplit(playerTeam);
        }
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

        foreach (Agent agent in spearCarriers)
        {
            if (agent.Formation != spearFormation)
            {
                agent.Formation = spearFormation;
            }
        }

        foreach (Agent agent in otherInfantry)
        {
            if (agent.Formation != swordFormation)
            {
                agent.Formation = swordFormation;
            }
        }

        _splitActive = true;
        Announce($"Spear wall formed: {spearCarriers.Count} spears in formation VI (key 6), {otherInfantry.Count} infantry in formation I. Press K to merge back.");
    }

    private static void MergeBack(Team playerTeam)
    {
        Formation swordFormation = playerTeam.GetFormation(FormationClass.Infantry);
        Formation spearFormation = playerTeam.GetFormation(FormationClass.HeavyInfantry);
        _splitActive = false;

        if (swordFormation == null || spearFormation == null || spearFormation.CountOfUnits <= 0)
        {
            return;
        }

        foreach (IFormationUnit unit in spearFormation.Arrangement.GetAllUnits().Concat(spearFormation.DetachedUnits).ToList())
        {
            if (unit is Agent agent && agent.IsActive() && !agent.IsPlayerControlled)
            {
                agent.Formation = swordFormation;
            }
        }

        Announce("Spear wall merged back into the infantry.");
    }

    private static void Announce(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(0xFFB8D8F8u)));
    }
}
