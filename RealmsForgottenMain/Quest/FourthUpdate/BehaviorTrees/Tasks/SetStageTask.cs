using BehaviorTrees;
using BehaviorTrees.Nodes;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class SetStageTask : BTTask, IWitchTree
    {
        private int stageToSet;

        public SetStageTask(int stage)
        {
            stageToSet = stage;
        }

        BTBlackboardValue<int> _stage;
        public BTBlackboardValue<int> Stage { get => _stage; set => _stage = value; }

        public override BTTaskStatus Execute()
        {
            Stage.SetValue(stageToSet);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}
