using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade
{
    internal sealed class RFSettlementTorchMissionBehavior : MissionLogic
    {
        private const float RefreshIntervalSeconds = 3f;
        private readonly HashSet<Agent> _torchBearers = new();
        private ItemObject _torchItem;
        private float _nextRefreshTime;

        public override void AfterStart()
        {
            base.AfterStart();
            RefreshTorches();
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            TryGiveTorch(agent);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Mission.CurrentTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Mission.CurrentTime + RefreshIntervalSeconds;
            RefreshTorches();
        }

        private void RefreshTorches()
        {
            if (!IsOutdoorSettlementAtNight())
            {
                RemoveTorches();
                return;
            }

            foreach (Agent agent in Mission.Agents)
            {
                TryGiveTorch(agent);
            }

            _torchBearers.RemoveWhere(agent => agent == null || !agent.IsActive());
        }

        private void TryGiveTorch(Agent agent)
        {
            if (!IsOutdoorSettlementAtNight()
                || agent == null
                || agent == Agent.Main
                || !agent.IsHuman
                || !agent.IsActive())
            {
                return;
            }

            _torchItem ??= MBObjectManager.Instance.GetObject<ItemObject>("torch");
            if (_torchItem == null)
            {
                return;
            }

            MissionWeapon currentExtraWeapon = agent.Equipment[EquipmentIndex.ExtraWeaponSlot];
            if (!currentExtraWeapon.IsEmpty)
            {
                if (currentExtraWeapon.Item == _torchItem)
                {
                    _torchBearers.Add(agent);
                }
                return;
            }

            MissionWeapon torch = new(_torchItem, null, null);
            agent.EquipWeaponWithNewEntity(EquipmentIndex.ExtraWeaponSlot, ref torch);
            agent.TryToWieldWeaponInSlot(
                EquipmentIndex.ExtraWeaponSlot,
                Agent.WeaponWieldActionType.InstantAfterPickUp,
                true);
            _torchBearers.Add(agent);
        }

        private void RemoveTorches()
        {
            if (_torchItem == null || _torchBearers.Count == 0)
            {
                return;
            }

            foreach (Agent agent in _torchBearers)
            {
                if (agent != null
                    && agent.IsActive()
                    && agent.Equipment[EquipmentIndex.ExtraWeaponSlot].Item == _torchItem)
                {
                    agent.RemoveEquippedWeapon(EquipmentIndex.ExtraWeaponSlot);
                }
            }
            _torchBearers.Clear();
        }

        private bool IsOutdoorSettlementAtNight()
        {
            Settlement settlement = Settlement.CurrentSettlement;
            return Campaign.Current != null
                && settlement != null
                && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage)
                && CampaignMission.Current?.Location?.IsIndoor == false
                && Mission.CombatType == Mission.MissionCombatType.NoCombat
                && !Campaign.Current.IsDay;
        }

        protected override void OnEndMission()
        {
            _torchBearers.Clear();
            base.OnEndMission();
        }
    }
}
