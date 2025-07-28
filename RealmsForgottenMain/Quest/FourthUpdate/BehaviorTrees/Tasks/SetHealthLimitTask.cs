using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class SetHealthLimitTask : BTTask, IBTBannerlordBase
    {
        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        float number;
        public SetHealthLimitTask(float number) : base() { this.number = number; }

        public override BTTaskStatus Execute()
        {
            Agent agent = Agent.GetValue();
            agent.HealthLimit = number;
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}
