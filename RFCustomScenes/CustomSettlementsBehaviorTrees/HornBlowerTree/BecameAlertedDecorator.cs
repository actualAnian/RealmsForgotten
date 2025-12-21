using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    public class BecameAlertedDecorator : BannerlordEventDecorator, IBTBannerlordBase
    {
        public BecameAlertedDecorator() : base(SubscriptionPossibilities.OnSelfAlarmedStateChanged) { }

        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

        public override bool Evaluate()
        {
            var test = Agent.GetValue().AIStateFlags.HasFlag(TaleWorlds.MountAndBlade.Agent.AIStateFlag.Alarmed);
            if ( test)
            {
                int a = 5;
            }
            return test;
        }

        public override void Notify(object[] data) { }
    }
}