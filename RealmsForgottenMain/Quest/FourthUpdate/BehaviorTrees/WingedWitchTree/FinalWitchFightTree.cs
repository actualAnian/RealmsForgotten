using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree
{
    public class FinalWitchFightTree : BehaviorTree
    {
        public static readonly List<Vec3> spawnTroopsLocations = new()
        {
            new(179.7798f, 219.5161f, 23f),
            new(184.281f, 219.354f, 23f),
            new(189.0654f, 219.2905f, 23f),
            new(201.0189f, 219.2926f, 23f),
            new(204.4614f, 219.4682f, 23f),
            new(207.8909f, 219.195f, 23f)
        };
        static readonly Vec3 _teleportToPlace = new(196.3073f, 256.1617f, 24.5f);
        public const string SpawnEnemyStrindId = "looter";
        private Agent agent;
        private BTBlackboardBannerlordBase bbBase;
        private WitchFinalFightBlackBoard bbWitch;

        public FinalWitchFightTree(Agent aagent, BTBlackboardBannerlordBase baseBB, WitchFinalFightBlackBoard witchBB)
        {
            agent = aagent;
            bbBase = baseBB;
            bbWitch = witchBB;
        }

        public static new BehaviorTree? BuildTree(object[] objects)
        {
            var positionsToTeleport = new List<Vec3>() { new(211.821f, 235.5218f, 20.05393f), new(180.8923f, 232.8567f, 20.19595f) };
            if (objects[0] is not Agent agent) return null;
            BTBlackboardBannerlordBase bbBase = new(agent);
            WitchFinalFightBlackBoard bbWitch = new();
            FinalWitchFightTree? tree = StartBuildingTree(new FinalWitchFightTree(agent, bbBase, bbWitch))
                .AddSelector("main")
                    .AddSelector("witch fight")
                        .AddSequence("seen player", new AlarmedDecorator(SubscriptionPossibilities.OnSelfAlarmedStateChanged, bbBase))
                            .AddTask(new SummonTroopsTask(spawnTroopsLocations, SpawnEnemyStrindId))
                        .Up()
                        .AddSequence("previous spawned enemies killed", new EnemyKilledDecorator(SpawnEnemyStrindId, SubscriptionPossibilities.OnAgentRemoved, bbBase, bbWitch), 1)
                            .AddTask(new SummonTroopsTask(spawnTroopsLocations, SpawnEnemyStrindId))
                        .Up()
                        .AddSequence("start spawning projectiles", new HealthBelowPercentageDecorator(50, bbBase))
                            .AddTask(new SwitchToSpawningTeleportingProjectiles())
                        .Up()
                        .AddSequence("fallen down the platform", new CrossedTheLineDecorator(positionsToTeleport[0], positionsToTeleport[1], true, bbBase))
                            .AddTask(new TeleportTask(_teleportToPlace, bbBase))
                        .Up()
                    .Up()
                .Finish();
            return tree;
        }
    }
}