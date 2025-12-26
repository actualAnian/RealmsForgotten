using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.BaseTasks
{
    internal class PlayAnimationTask : BTTask, IBTBannerlordBase
    {
        readonly string animationName;

        public PlayAnimationTask(string animationName)
        {
            this.animationName = animationName;
        }

        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        public override BTTaskStatus Execute()
        {
            Agent.GetValue().SetActionChannel(0, ActionIndexCache.Create(animationName), true);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}