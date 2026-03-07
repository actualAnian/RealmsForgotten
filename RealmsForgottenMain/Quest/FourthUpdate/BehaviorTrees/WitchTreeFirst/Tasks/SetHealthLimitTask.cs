using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks
{
    public class SetHealthLimitTask : BTTask
    {

        BTBlackboardBannerlordBase _bbBase;
        float _number;
        public SetHealthLimitTask(float number, BTBlackboardBannerlordBase bbBase) : base() 
        {
            _number = number; 
            _bbBase = bbBase;
        }

        public override BTTaskStatus Execute()
        {
            Agent agent = _bbBase.Agent;
            agent.HealthLimit = _number;
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}
