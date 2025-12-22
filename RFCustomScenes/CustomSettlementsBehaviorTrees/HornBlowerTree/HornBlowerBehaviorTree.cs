using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using BehaviorTreeWrapper.Tasks;
using RFCustomSettlements.CustomSettlementsBehaviorTrees.BaseTasks;
using RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree.Tasks;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    public class HornBlowerBehaviorTree : BehaviorTree, IBTBannerlordBase, IHornBlowerTree
    {
        public HornBlowerBehaviorTree(Agent agent) : base(1000)
        {
            Agent = new BTBlackboardValue<Agent>(agent);
            SavedFlags = new BTBlackboardValue<AgentFlag>(Agent.GetValue().GetAgentFlags());
        }
        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        BTBlackboardValue<AgentFlag> _flags;
        public BTBlackboardValue<AgentFlag> SavedFlags { get => _flags; set => _flags = value; }
        public static new BehaviorTree? BuildTree(object[] objects)
        {
            if (objects[0] is not Agent agent) return null;
            if (objects[1] is not float alertDistance) return null;
            if (objects[2] is not string hornItemId) return null;
            HornBlowerBehaviorTree? tree = StartBuildingTree(new HornBlowerBehaviorTree(agent))
                .AddSelector("main")
                    .AddSequence("alerted", new AlarmedDecorator(SubscriptionPossibilities.OnSelfAlarmedStateChanged))
                        .AddTask(new FlipAiTask(true))
                        .AddTask(new EquipHornTask(hornItemId))
                        .AddTask(new BaseTasks.PlayAnimationTask("act_human_blow_horn"))
                        .AddTask(new SleepTask(TimeSpan.FromSeconds(1)))
                        .AddTask(new PlaySoundTask("medieval_alarm_horn"))
                        .AddTask(new AlertNearbyFoesTask(alertDistance))
                        .AddTask(new SleepTask(TimeSpan.FromSeconds(0.5)))
                        .AddTask(new FlipAiTask(false))
                    .Up()
                .Up()
                .Finish();
            return tree;
        }
    }
}