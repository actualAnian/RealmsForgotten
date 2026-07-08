using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.BaseTasks
{
    internal class PlayAnimationTask : BTTask
    {
        readonly string _animationName;
        BTBlackboardBannerlordBase _bbBase;

        public PlayAnimationTask(string animationName, BTBlackboardBannerlordBase bbBase)
        {
            _animationName = animationName;
            _bbBase = bbBase;
        }
        public override BTTaskStatus Execute()
        {
            _bbBase.Agent.SetActionChannel(0, ActionIndexCache.Create(_animationName), true);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}