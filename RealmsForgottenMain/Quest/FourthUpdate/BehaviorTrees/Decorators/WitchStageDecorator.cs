using BehaviorTrees;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Decorators
{
    public class WitchStageDecorator : BTReturnFalseDecorator
    {
        private int stageLookingFor;
        WitchTreeBlackBoard _witchBB;
        public WitchStageDecorator(int stage, WitchTreeBlackBoard witchBB)
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
