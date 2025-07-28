using BehaviorTrees;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees
{
    public interface IWitchTree : IBTBlackboard
    {
        public BTBlackboardValue<int> Stage { get; set; }
    }
}
