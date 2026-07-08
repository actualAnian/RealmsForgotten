using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using RealmsForgotten.Utility.Magic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks
{
    public class TeleportTask : BTTask
    {
        private Vec3 _position;
        BTBlackboardBannerlordBase _bbBase;
        public TeleportTask(Vec3 position, BTBlackboardBannerlordBase bbBase)
        {
            _position = position;
            _bbBase = bbBase;
        }
        public override BTTaskStatus Execute()
        {
            Teleport.TeleportToPosition(_bbBase.Agent, _position);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}