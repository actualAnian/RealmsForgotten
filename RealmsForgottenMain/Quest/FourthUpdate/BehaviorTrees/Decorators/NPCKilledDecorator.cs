using BehaviorTreeWrapper;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Decorators
{
    public class NPCKilledDecorator : BannerlordEventDecorator
    {
        private readonly string demonSummonStringId;
        bool hasBeenKilled = false;
        public NPCKilledDecorator(string demonSummonStringId, SubscriptionPossibilities SubscribesTo) : base(SubscribesTo)
        {
            this.demonSummonStringId = demonSummonStringId;
        }

        public override bool Evaluate()
        {
            return hasBeenKilled;
        }

        // Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow
        public override void Notify(object[] data)
        {
            Agent affectedAgent = (Agent)data[0];
            hasBeenKilled = affectedAgent.Character?.StringId == demonSummonStringId;
        }
    }
}
