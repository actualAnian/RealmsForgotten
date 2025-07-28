using BehaviorTrees;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Decorators
{
    public class WitchStageDecorator : BTReturnFalseDecorator, IWitchTree
    {
        private int stageLookingFor;
        public BTBlackboardValue<int> _stage;
        public BTBlackboardValue<int> Stage { get => _stage; set => _stage = value; }

        public WitchStageDecorator(int stage)
        {
            stageLookingFor = stage;
        }

        public override bool Evaluate()
        {
            return stageLookingFor == Stage.GetValue();
        }
    }
}
