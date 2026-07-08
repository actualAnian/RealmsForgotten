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
    public class AlertNearbyFoesTask : BTTask
    {
        BTBlackboardBannerlordBase _bbBase;

        readonly float _alertDistance;
        public AlertNearbyFoesTask(float alertDistance, BTBlackboardBannerlordBase bbBase)
        {
            _alertDistance = alertDistance;
            _bbBase = bbBase;
        }
        public override BTTaskStatus Execute()
        {
            if (Agent.Main == null)
                return BTTaskStatus.FinishedWithFalse;
            Mission.Current.Agents
                .Where(agent => agent.IsEnemyOf(Agent.Main)
                && agent.GetDistanceTo(_bbBase.Agent) < _alertDistance 
                && agent != _bbBase.Agent
                && (agent.AIStateFlags & Agent.AIStateFlag.Alarmed) != Agent.AIStateFlag.Alarmed)
                .ToList()
                .ForEach(agent =>
                {
                    agent.SetAlarmState(Agent.AIStateFlag.Cautious);
                    WorldPosition lastSuspiciousPosition = _bbBase.Agent.GetWorldPosition();
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