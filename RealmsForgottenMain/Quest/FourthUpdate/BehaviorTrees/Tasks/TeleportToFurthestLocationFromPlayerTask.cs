using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using RealmsForgotten.Utility.Magic;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class TeleportToFurthestLocationFromPlayerTask : BTTask, IBTBannerlordBase
    {
        private List<Vec3> possiblePosition;

        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        public TeleportToFurthestLocationFromPlayerTask(List<Vec3> possiblePosition)
        {
            this.possiblePosition = possiblePosition;
        }
        public override BTTaskStatus Execute()
        {
            Agent agent = Agent.GetValue();
            Vec3 furthestPosition = possiblePosition[0];
            if (TaleWorlds.MountAndBlade.Agent.Main == null)
            {
                Teleport.TeleportToPosition(agent, furthestPosition);
                return BTTaskStatus.FinishedWithTrue;
            }
            float bestPosition = 0;
            foreach (Vec3 position in possiblePosition)
            {
                float nextPosition = TaleWorlds.MountAndBlade.Agent.Main!.Position.DistanceSquared(position);
                if (nextPosition > bestPosition)
                {
                    furthestPosition = position;
                    bestPosition = nextPosition;
                }
            }
            Teleport.TeleportToPosition(agent, furthestPosition);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}