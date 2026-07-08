using BehaviorTrees;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Decorators
{
    public class WitchStageDecorator : BTReturnFalseDecorator
    {
        private int stageLookingFor;
        StageBlackBoard _witchBB;
        public WitchStageDecorator(int stage, StageBlackBoard witchBB)
        {
            stageLookingFor = stage;
            _witchBB = witchBB;
        }

        public override bool Evaluate()
        {
            return stageLookingFor == _witchBB.Stage;
        }
    }
}
