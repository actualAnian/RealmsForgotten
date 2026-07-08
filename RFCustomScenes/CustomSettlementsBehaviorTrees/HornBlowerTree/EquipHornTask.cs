using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    internal class EquipHornTask : BTTask
    {
        BTBlackboardBannerlordBase _bbBase;
        private string _hornItemId;

        public EquipHornTask(string hornItemId, BTBlackboardBannerlordBase bbBase)
        {
            _hornItemId = hornItemId;
            _bbBase = bbBase;
        }
        public override BTTaskStatus Execute()
        {
            if (_bbBase.Agent.WieldedOffhandWeapon.Item != null)
                _bbBase.Agent.TryToSheathWeaponInHand(TaleWorlds.MountAndBlade.Agent.HandIndex.OffHand, TaleWorlds.MountAndBlade.Agent.WeaponWieldActionType.Instant);
            _bbBase.Agent.TryToWieldWeaponInSlot(EquipmentIndex.ExtraWeaponSlot, Agent.WeaponWieldActionType.Instant, false);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}