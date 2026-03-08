using BehaviorTreeWrapper.AbstractDecoratorsListeners;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.CanyonWitchTree
{
    internal class SingleTimeDecorator : BannerlordNoWaitDecorator
    {
        bool hasBeenExecuted = false;
        public SingleTimeDecorator() { }
        public override bool Evaluate()
        {
            if (hasBeenExecuted) return false;
            hasBeenExecuted = true;
            return true;
        }
    }
}