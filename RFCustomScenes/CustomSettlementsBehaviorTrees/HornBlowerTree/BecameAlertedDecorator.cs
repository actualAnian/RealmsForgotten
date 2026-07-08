using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    public class BecameAlertedDecorator : BannerlordEventDecorator
    {
        BTBlackboardBannerlordBase _bbBase;
        public BecameAlertedDecorator(BTBlackboardBannerlordBase bbBase) : base(SubscriptionPossibilities.OnSelfAlarmedStateChanged) { _bbBase = bbBase; }
        public override bool Evaluate()
        {
            var test = _bbBase.Agent.AIStateFlags.HasFlag(Agent.AIStateFlag.Alarmed);
            return test;
        }
        public override void Notify(object[] data) { }
    }
}