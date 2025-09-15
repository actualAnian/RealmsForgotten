using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.CustomOrderofBattle
{
    public class InfantrySpearSorterOnSpawn : MissionLogic
    {
        private readonly HashSet<int> _done = new HashSet<int>();
        private float _t;
        private bool _summaryShown;
        private int _movedSpear, _movedNonSpear;

        public override void OnAgentCreated(Agent agent) => TryReassign(agent);

        public override void OnMissionTick(float dt)
        {
            _t += dt;
            if (!_summaryShown && _t > 2.0f && (_movedSpear + _movedNonSpear) > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[RF] Split INF -> Spear:{_movedSpear}  NonSpear:{_movedNonSpear}"));
                _summaryShown = true;
            }
        }

        private void TryReassign(Agent agent)
        {
            if (agent == null || !agent.IsHuman || !agent.IsAIControlled) return;
            if (agent.HasMount || agent.Team == null || agent.Formation == null) return;
            if (_done.Contains(agent.Index)) return;

            // Target formations by CLASS (robust; no OOB index dependency)
            var nonSpear = agent.Team.GetFormation(FormationClass.Infantry);
            var spear = agent.Team.GetFormation(FormationClass.HeavyInfantry);
            if (nonSpear == null || spear == null) { _done.Add(agent.Index); return; }

            bool isSpear = HasMeleePolearm(agent); // uses SpawnEquipment (authoritative)
            var target = isSpear ? spear : nonSpear;

            if (agent.Formation != target)
            {
                agent.Formation = target; // supported runtime reassignment
                if (isSpear) _movedSpear++; else _movedNonSpear++;
            }

            _done.Add(agent.Index);
        }

        // True if spawned gear includes a melee polearm (not javelin / not ranged)
        private static bool HasMeleePolearm(Agent agent)
        {
            var eq = agent.SpawnEquipment; // actual loadout chosen at spawn
            if (eq == null) return false;

            for (var i = EquipmentIndex.WeaponItemBeginSlot; i <= EquipmentIndex.Weapon3; i++)
            {
                var item = eq[i].Item;
                var comp = item?.WeaponComponent;
                if (comp == null) continue;

                // Any usage that is melee, not ranged, not consumable, and uses Polearm skill (not javelin)
                if (comp.Weapons.Any(u =>
                        u.IsMeleeWeapon &&
                        !u.IsRangedWeapon &&
                        !u.IsConsumable &&
                        u.RelevantSkill == DefaultSkills.Polearm &&
                        u.WeaponClass != WeaponClass.Javelin))
                {
                    return true;
                }
            }
            return false;
        }
    }
}