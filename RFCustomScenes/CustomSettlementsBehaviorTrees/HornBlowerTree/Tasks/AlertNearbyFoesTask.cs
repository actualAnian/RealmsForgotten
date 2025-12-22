using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using HarmonyLib;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree.Tasks
{
    public class AlertNearbyFoesTask : BTTask, IBTBannerlordBase
    {
        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

        readonly float _alertDistance;
        public AlertNearbyFoesTask(float alertDistance)
        {
            _alertDistance = alertDistance;
        }
        public override BTTaskStatus Execute()
        {
            if (TaleWorlds.MountAndBlade.Agent.Main == null)
                return BTTaskStatus.FinishedWithFalse;
            Mission.Current.Agents
                .Where(agent => agent.IsEnemyOf(TaleWorlds.MountAndBlade.Agent.Main)
                && agent.GetDistanceTo(Agent.GetValue()) < _alertDistance 
                && agent != Agent.GetValue()
                && (agent.AIStateFlags & TaleWorlds.MountAndBlade.Agent.AIStateFlag.Alarmed) != TaleWorlds.MountAndBlade.Agent.AIStateFlag.Alarmed)
                .ToList()
                .ForEach(agent =>
                {
                    agent.SetAlarmState(TaleWorlds.MountAndBlade.Agent.AIStateFlag.Cautious);
                    WorldPosition lastSuspiciousPosition = Agent.GetValue().GetWorldPosition();
                    agent.SetAILastSuspiciousPosition(lastSuspiciousPosition, checkNavMeshForCorrection: false);
                    AlarmedBehaviorGroup? alarmedBehavior = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
                    if (alarmedBehavior != null)
                    {
                        MethodInfo setterMethod = AccessTools.PropertySetter(typeof(AlarmedBehaviorGroup), "AlarmFactor");
                        setterMethod.Invoke(alarmedBehavior, new object[] { 1f });
                    }
                });
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}