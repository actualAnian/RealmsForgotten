using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using BehaviorTreeWrapper.Tasks;
using RealmsForgotten.MusicSounds;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.CanyonWitchTree;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks;
using System;
using System.CodeDom;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree
{
    public class FinalWitchFightTree : BehaviorTree
    {
        const int WitchHealth = 500;
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
        public const string SpawnEnemyStrindId = "cs_devils_bandits_chief";
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
            var voicelineALen = OggUtils.GetSoundLength("old_witch_cave_line_a");
            FinalWitchFightTree? tree = StartBuildingTree(new FinalWitchFightTree(agent, bbBase, bbWitch))
                .AddSelector("main")
                    .AddSelector("witch fight")
                        .AddSequence("start fight", new SingleTimeDecorator())
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("old_witch_cave_line_a"))
                            .AddTask(new SleepTask(TimeSpan.FromSeconds(voicelineALen)))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Alarmed, bbBase))
                            .AddTask(new SummonTroopsTask(spawnTroopsLocations, SpawnEnemyStrindId))
                            .AddTask(new SetHealthTask(WitchHealth, bbBase))
                        .Up()
                        .AddSequence("previous spawned enemies killed", new EnemyKilledDecorator(SpawnEnemyStrindId, SubscriptionPossibilities.OnAgentRemoved, bbBase, bbWitch), 1)
                            .AddTask(new SummonTroopsTask(spawnTroopsLocations, SpawnEnemyStrindId))
                        .Up()
                        .AddSequence("start spawning projectiles", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
                            .AddSequence("only start spawning once", new SingleTimeDecorator())
                                .AddTask(new SwitchToSpawningTeleportingProjectiles())
                                .AddTask(new PlaySoundEffectFollowingPlayerTask("old_witch_cave_final"))
                            .Up()
                        .Up()
                        .AddSequence("fallen down the platform", new AgentCrossedLineDecorator(positionsToTeleport[0], positionsToTeleport[1], true, bbBase.Agent, false))
                            .AddTask(new TeleportTask(_teleportToPlace, bbBase))
                        .Up()
                    .Up()
                .Finish();
            return tree;
        }
    }
}