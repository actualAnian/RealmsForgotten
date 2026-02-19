using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    internal class FlipAiTask : BTTask
    {
        BTBlackboardBannerlordBase _bbBase;
        HornBlowerBlackBoard _bbHornBlower;
        private bool _disable;

        public FlipAiTask(bool disable, BTBlackboardBannerlordBase bbBase, HornBlowerBlackBoard bbHornBlover)
        {
            _disable = disable;
            _bbBase = bbBase;
            _bbHornBlower = bbHornBlover;
        }

        public override BTTaskStatus Execute()
        {
            var agent = _bbBase.Agent;
            if (_disable)
            {
                agent.SetTargetPosition(agent.Position.AsVec2);
                _bbHornBlower.SavedFlags = agent.GetAgentFlags();
                agent.DisableScriptedMovement();
                agent.SetAgentFlags(AgentFlag.IsHumanoid);
                agent.SetIsAIPaused(true);
            }
            else
            {
                agent.ClearTargetFrame();
                agent.SetAgentFlags(_bbHornBlower.SavedFlags);
                agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
                agent.SetIsAIPaused(false);
            }
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}