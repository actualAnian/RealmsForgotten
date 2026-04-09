using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using BehaviorTreeWrapper.Tasks;
using RealmsForgotten.MusicSounds;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Decorators;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.CanyonWitchTree
{
    public class CanyonWitchTree : BehaviorTree
    {
        static readonly List<Vec3> bridgeSpawnTroopsLocations = new()
        {
             new(247.531f, 201.384f, 36.2237f),
             new(249.157f, 201.432f, 36.8093f),
             new(250.386f, 201.488f, 37.4231f),
             new(243.659f, 237.249f, 37.1675f),
             new(246.632f, 237.367f, 37.5593f),
             new(250.800f, 237.532f, 38.2252f),
        };

        static readonly List<Vec3> towerSpawnTroopsLocations = new()
        {
            new(197.1086f, 244.6261f, 39.98999f),
            new(197.1481f, 242.8306f, 39.98999f),
            new(197.1171f, 240.5379f, 39.59999f),
            new(197.1173f, 241.5379f, 39.79999f),
            new(197.1183f, 244.5379f, 39.89999f),
        };

        static readonly Vec3 lineToCrossForVoiceLineAPointA = new(289.0656f, 58.84806f, 30.44381f);
        static readonly Vec3 lineToCrossForVoiceLineAPointB = new(254.5997f, 63.73717f, 31.31858f);
        static readonly Vec3 lineToCrossBridgePointA = new(238.2366f, 195.9973f, 30.7439f);
        static readonly Vec3 lineToCrossBridgePointB = new(235.9295f, 221.0938f, 30.7565f);
        static readonly Vec3 lineToCrossTeleportToTowerPointA = new(242.00f, 241.99f, 37.96f);
        static readonly Vec3 lineToCrossTeleportToTowerPointB = new(254.92f, 243.45f, 38.82f);
        static readonly Vec3 lineToCrossForVoiceLineCavePointA = new(213.86f, 275.05f, 45.49f);
        static readonly Vec3 lineToCrossForVoiceLineCavePointB = new(219.01f, 271.49f, 44.53f);
        static readonly Vec3 witchFirstSpot = new(259.14f, 192.65f, 44.69f);
        static readonly Vec3 witchSecondSpot = new(190.92f, 252.82f, 60.01f);
        public const string SpawnEnemyStringId = "cs_devils_bandits_chief";
        private readonly BTBlackboardBannerlordBase bbBase;
        public CanyonWitchTree(BTBlackboardBannerlordBase baseBB)
        {
            bbBase = baseBB;
        }
        public static new BehaviorTree? BuildTree(object[] objects)
        {
            if (objects[0] is not Agent agent) return null;
            BTBlackboardBannerlordBase bbBase = new(agent);
            StageBlackBoard stageBB = new();
            var voicelineBLen = OggUtils.GetSoundLength("old_witch_canyon_line_b");
            var tree = StartBuildingTree(new CanyonWitchTree(bbBase))
                .AddSelector("main")
                    .AddSelector("entrance", new WitchStageDecorator(0, stageBB))
                        .AddSequence("say dialogue A", new AgentCrossedLineDecorator(lineToCrossForVoiceLineAPointA, lineToCrossForVoiceLineAPointB, false, Agent.Main))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("old_witch_canyon_line_a"))
                            .AddTask(new SetStageTask(1, stageBB))
                        .Up()
                    .Up()
                    .AddSelector("ambush", new WitchStageDecorator(1, stageBB))
                        .AddSequence("spawn enemies", new AgentCrossedLineDecorator(lineToCrossBridgePointA, lineToCrossBridgePointB, false, Agent.Main, true, true))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("old_witch_canyon_line_b"))
                            .AddTask(new SleepTask(TimeSpan.FromSeconds(voicelineBLen)))
                            .AddTask(new TeleportTask(witchFirstSpot, bbBase))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Alarmed, bbBase))
                            .AddTask(new SummonTroopsTask(bridgeSpawnTroopsLocations, SpawnEnemyStringId))
                            .AddTask(new SetHealthTask(1000, bbBase))
                        .Up()
                        .AddSequence("hit by player", new HitDecorator(BehaviorTreeWrapper.SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new SetStageTask(2, stageBB))
                        .Up()
                        .AddSequence("crossed to tower", new AgentCrossedLineDecorator(lineToCrossTeleportToTowerPointA, lineToCrossTeleportToTowerPointB, false, Agent.Main, true, false, 0.5))
                            .AddTask(new SetStageTask(2, stageBB))
                        .Up()
                    .Up()
                    .AddSelector("tower fight", new WitchStageDecorator(2, stageBB))
                        .AddSequence("prepare", new SingleTimeDecorator())
                            .AddTask(new TeleportTask(witchSecondSpot, bbBase))
                            .AddTask(new SummonTroopsTask(towerSpawnTroopsLocations, SpawnEnemyStringId))
                            .AddTask(new SetHealthTask(1000, bbBase))
                        .Up()
                       .AddSequence("hit by player", new HitDecorator(BehaviorTreeWrapper.SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new TeleportTask(WitchCanyonMissionLogic.WitchSpawnPosition, bbBase))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Cautious, bbBase))
                        .Up()
                        .AddSequence("player next to cave", new AgentCrossedLineDecorator(lineToCrossForVoiceLineCavePointA, lineToCrossForVoiceLineCavePointB, false, Agent.Main))
                            .AddTask(new TeleportTask(WitchCanyonMissionLogic.WitchSpawnPosition, bbBase))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Cautious, bbBase))
                        .Up()
                    .Up() 
                .Up()
                .Finish();
            return tree;
        }
    }
}