using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Decorators
{
    public class BelowPercentageAfterHitDecorator : BannerlordEventDecorator, IBTBannerlordBase
    {
        private bool hasBeenHit = false;
        float percentage;
        public BelowPercentageAfterHitDecorator(float percentage, SubscriptionPossibilities SubscribesTo) : base(SubscribesTo) { this.percentage = percentage; }

        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

        public override bool Evaluate()
        {
            if (!hasBeenHit) return false;
            hasBeenHit = false;
            return true;
        }
        public override void Notify(object[] data)
        {
            if (hasBeenHit) return;
            Agent agent = Agent.GetValue();
            if (agent.Health / agent.HealthLimit < percentage)
                hasBeenHit = true;
        }
    }
}
