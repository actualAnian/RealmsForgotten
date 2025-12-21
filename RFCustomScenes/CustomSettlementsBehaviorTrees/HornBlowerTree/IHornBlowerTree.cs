using BehaviorTrees;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    internal interface IHornBlowerTree : IBTBlackboard
    {
        BTBlackboardValue<AgentFlag> SavedFlags { get; set; }
    }
}