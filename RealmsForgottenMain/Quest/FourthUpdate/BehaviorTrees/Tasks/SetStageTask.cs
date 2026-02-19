using BehaviorTrees;
using BehaviorTrees.Nodes;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class SetStageTask : BTTask
    {
        private int _stageToSet;
        WitchTreeBlackBoard _witchBB;

        public SetStageTask(int stage, WitchTreeBlackBoard witchBB)
        {
            _stageToSet = stage;
            _witchBB = witchBB;
        }
        public override BTTaskStatus Execute()
        {
            _witchBB.Stage = _stageToSet;
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}
