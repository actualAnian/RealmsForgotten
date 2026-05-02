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
using TaleWorlds.ObjectSystem;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    public class HornBlowerBehaviorTree : BehaviorTree
    {
        BTBlackboardBannerlordBase _bbBase;
        HornBlowerBlackBoard _blackBoardHornBlover;
        public HornBlowerBehaviorTree(Agent agent, BTBlackboardBannerlordBase bbBase, HornBlowerBlackBoard blackBoardHornBlover) : base(1000)
        {
            _bbBase = bbBase;
            _blackBoardHornBlover = blackBoardHornBlover;
        }
        public static new BehaviorTree? BuildTree(object[] objects)
        {
            if (objects[0] is not Agent agent) return null;
            if (objects[1] is not float alertDistance) return null;
            if (objects[2] is not string hornItemId) return null;

            BTBlackboardBannerlordBase bbBase = new(agent);
            HornBlowerBlackBoard bbHornBlover = new(agent.GetAgentFlags());
            HornBlowerBehaviorTree? tree = StartBuildingTree(new HornBlowerBehaviorTree(agent, bbBase, bbHornBlover))
                .AddSelector("main")
                    .AddSequence("alerted", new AlarmedDecorator(SubscriptionPossibilities.OnSelfAlarmedStateChanged, bbBase))
                        .AddTask(new FlipAiTask(true, bbBase, bbHornBlover))
                        //.AddTask(new SleepTask(TimeSpan.FromSeconds(0.5)))
                         //.AddTask(new EquipHornTask(hornItemId))
                        .AddTask(new BaseTasks.PlayAnimationTask("act_human_blow_horn", bbBase))
                        .AddTask(new SleepTask(TimeSpan.FromSeconds(1)))
                        .AddTask(new PlaySoundTask("medieval_alarm_horn", bbBase))
                        .AddTask(new AlertNearbyFoesTask(alertDistance, bbBase))
                        .AddTask(new SleepTask(TimeSpan.FromSeconds(0.5)))
                        .AddTask(new FlipAiTask(false, bbBase, bbHornBlover))
                    .Up()
                .Up()
                .Finish();
            //EquipHornItem(agent, hornItemId);
            return tree;
        }
        private static void EquipHornItem(Agent agent, string hornItemId)
        {
            var horn = MBObjectManager.Instance.GetObject<ItemObject>(hornItemId);
            MissionWeapon weapon = new(horn, null, null);
            agent.EquipWeaponWithNewEntity(EquipmentIndex.ExtraWeaponSlot, ref weapon);
            agent.TryToWieldWeaponInSlot(EquipmentIndex.ExtraWeaponSlot, TaleWorlds.MountAndBlade.Agent.WeaponWieldActionType.Instant, false);
        }
    }
}