using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class SetHealthTask : BTTask, IBTBannerlordBase
    {
        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        float number;
        public SetHealthTask(float number) : base() { this.number = number; }

        public override BTTaskStatus Execute()
        {
            Agent agent = Agent.GetValue();
            agent.Health = number;
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}
