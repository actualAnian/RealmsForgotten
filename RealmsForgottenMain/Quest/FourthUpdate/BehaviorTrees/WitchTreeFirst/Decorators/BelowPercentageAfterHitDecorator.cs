using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Decorators
{
    public class BelowPercentageAfterHitDecorator : BannerlordEventDecorator
    {
        private bool hasBeenHit = false;
        float percentage;
        BTBlackboardBannerlordBase _bbBase;
        public BelowPercentageAfterHitDecorator(float percentage, SubscriptionPossibilities SubscribesTo, BTBlackboardBannerlordBase bbBase) : base(SubscribesTo)
        {
            this.percentage = percentage;
            _bbBase = bbBase;
        }
        public override bool Evaluate()
        {
            if (!hasBeenHit) return false;
            hasBeenHit = false;
            return true;
        }
        public override void Notify(object[] data)
        {
            if (hasBeenHit) return;
            Agent agent = _bbBase.Agent;
            if (agent.Health / agent.HealthLimit < percentage)
                hasBeenHit = true;
        }
    }
}
