using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Decorators;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchFirstEncounter
{
    public class WingedWitchFirstEncounterTree : BehaviorTree
    {
        public static readonly List<Vec3> spawnTroopsLocations = new();
        public const string SpawnEnemyStrindId = "looter";
        private readonly BTBlackboardBannerlordBase bbBase;
        private static readonly Vec3 ambushLocation = new(1, 1, 1);
        public WingedWitchFirstEncounterTree(BTBlackboardBannerlordBase baseBB)
        {
            bbBase = baseBB;
        }
        public static new BehaviorTree? BuildTree(object[] objects)
        {
            if (objects[0] is not Agent agent) return null;
            BTBlackboardBannerlordBase bbBase = new(agent);
            var tree = StartBuildingTree(new WingedWitchFirstEncounterTree(bbBase))
                .AddSelector("main")
                    .AddSelector("ambush")
                        .AddSequence("spawn enemies", new PlayerNearPointDecorator(ambushLocation))
                            .AddTask(new SummonTroopsTask(spawnTroopsLocations, SpawnEnemyStrindId))
                            .Up()
                        .Up()
                        .AddSequence("start spawning projectiles", new HealthBelowPercentageDecorator(50, bbBase))
                            .AddTask(new SwitchToSpawningTeleportingProjectiles())
                        .Up()
                .Finish();
            return tree;
        }
    }
}