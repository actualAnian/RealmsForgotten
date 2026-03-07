using BehaviorTrees;
using BehaviorTrees.Nodes;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks
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
