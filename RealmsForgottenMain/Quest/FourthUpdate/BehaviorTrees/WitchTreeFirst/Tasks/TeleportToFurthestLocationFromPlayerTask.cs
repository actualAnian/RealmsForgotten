using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using RealmsForgotten.Utility.Magic;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks
{
    public class TeleportToFurthestLocationFromPlayerTask : BTTask
    {
        private List<Vec3> _possiblePosition;
        BTBlackboardBannerlordBase _bbBase;
        public TeleportToFurthestLocationFromPlayerTask(List<Vec3> possiblePosition, BTBlackboardBannerlordBase bbBase)
        {
            _possiblePosition = possiblePosition;
            _bbBase = bbBase;
        }
        public override BTTaskStatus Execute()
        {
            Agent agent = _bbBase.Agent;
            Vec3 furthestPosition = _possiblePosition[0];
            if (Agent.Main == null)
            {
                Teleport.TeleportToPosition(agent, furthestPosition);
                return BTTaskStatus.FinishedWithTrue;
            }
            float bestPosition = 0;
            foreach (Vec3 position in _possiblePosition)
            {
                float nextPosition = Agent.Main!.Position.DistanceSquared(position);
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