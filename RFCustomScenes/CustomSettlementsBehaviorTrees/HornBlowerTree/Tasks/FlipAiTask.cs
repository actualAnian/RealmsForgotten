using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree.Tasks
{
    internal class FlipAiTask : BTTask, IBTBannerlordBase, IHornBlowerTree
    {
        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        BTBlackboardValue<AgentFlag> _flags;
        public BTBlackboardValue<AgentFlag> SavedFlags { get => _flags; set => _flags = value; }
        private bool _disable;

        public FlipAiTask(bool disable)
        {
            _disable = disable;
        }

        public override BTTaskStatus Execute()
        {
            var agent = Agent.GetValue();
            if (_disable)
            {
                agent.SetTargetPosition(agent.Position.AsVec2);
                SavedFlags.SetValue(agent.GetAgentFlags());
                agent.DisableScriptedMovement();
                agent.SetAgentFlags(AgentFlag.IsHumanoid);
                agent.SetIsAIPaused(true);
            }
            else
            {
                agent.ClearTargetFrame();
                agent.SetAgentFlags(SavedFlags.GetValue());
                agent.AIStateFlags |= TaleWorlds.MountAndBlade.Agent.AIStateFlag.Alarmed;
                agent.SetIsAIPaused(false);
            }
            return BTTaskStatus.FinishedWithTrue;
            //closest.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);w
        }
    }
}