using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class SetHealthTask : BTTask
    {

        BTBlackboardBannerlordBase _bbBase;
        float _number;
        public SetHealthTask(float number, BTBlackboardBannerlordBase bbBase) : base() 
        {
            _number = number;
            _bbBase = bbBase;
        }

        public override BTTaskStatus Execute()
        {
            Agent agent = _bbBase.Agent;
            agent.Health = _number;
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}
