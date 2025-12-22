using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree.Tasks
{
    internal class EquipHornTask : BTTask, IBTBannerlordBase
    {
        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        private string hornItemId;

        public EquipHornTask(string hornItemId)
        {
            this.hornItemId = hornItemId;
        }
        public override BTTaskStatus Execute()
        {
            if (Agent.GetValue().WieldedOffhandWeapon.Item != null)
                Agent.GetValue().TryToSheathWeaponInHand(TaleWorlds.MountAndBlade.Agent.HandIndex.OffHand, TaleWorlds.MountAndBlade.Agent.WeaponWieldActionType.Instant);
            var horn = MBObjectManager.Instance.GetObject<ItemObject>(hornItemId);
            MissionWeapon weapon = new(horn, null, null);
            Agent.GetValue().EquipWeaponToExtraSlotAndWield(ref weapon);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}