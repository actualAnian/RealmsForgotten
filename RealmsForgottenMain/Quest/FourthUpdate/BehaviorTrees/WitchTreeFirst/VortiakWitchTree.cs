using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using BehaviorTreeWrapper.Tasks;
using RealmsForgotten.MusicSounds;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Decorators;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst
{
    public class VortiakWitchTree : BehaviorTree
    {
        public static int witchFinalFightHealth = 500; // at 50% health the witch will teleport away and the fight is over
        public static Vec3 entrance = new(787.9024f, 680.45f, 155.4046f);
        public static Vec3 platformA = new(789.72f, 646.49f, 179.97f);
        public static Vec3 platformB = new(691.44f, 736.63f, 246.34f);
        public static Vec3 platformC = new(534.06f, 566.85f, 253.57f);

        public static Vec3 playerPositionToTeleportToPlatformB = new(804.6071f, 722.5151f, 171.8341f);
        public static Vec3 playerPositionToTeleportToPlatformC = new(640.3368f, 725.8465f, 238.0678f);
        public static Vec3 playerPositionToStartStage3 = new(585.8661f, 615.3213f, 275.3501f);

        public static List<Vec3> possibleTeleportLocations = new()
        {        
             new(547.7893f, 588.1529f, 275.8912f),
             new(544.7042f, 611.6611f, 275.1653f),
             new(573.1707f, 624.2562f, 275.1483f),
             new(595.8301f, 593.8116f, 275.2361f),
             new(572.5754f, 576.1677f, 275.307f)
        };
        public static string demonSummonStringId = "balrog";


        readonly BTBlackboardBannerlordBase _bbBase;
        readonly WitchTreeBlackBoard _witchBb;
        public VortiakWitchTree(Agent agent, BTBlackboardBannerlordBase bbBase, WitchTreeBlackBoard witchBb) : base(2000)
        {
            _bbBase = bbBase;
            _witchBb = witchBb;
        }
        public static new BehaviorTree? BuildTree(object[] objects)
        {
            if (objects[0] is not Agent agent) return null;
            BTBlackboardBannerlordBase bbBase = new(agent);
            WitchTreeBlackBoard witchBB = new();
            RFSound demonCommand = RFSound.All.Where(s => s.Id == "witch_voice_demon_command").FirstOrDefault();
            if (demonCommand == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error creating the VortiakWitchTree: Sound 'witch_voice_demon_command' not found in RFSound.All. Please ensure it is registered correctly.", new Color(1, 0, 0)));
                return null;
            }
            RFSound demonDefeated = RFSound.All.Where(s => s.Id == "witch_voice_demon_defeated").FirstOrDefault();
            if (demonDefeated == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error creating the VortiakWitchTree: Sound 'witch_voice_demon_defeated' not found in RFSound.All. Please ensure it is registered correctly.", new Color(1, 0, 0)));
                return null;
            }
            VortiakWitchTree? tree = StartBuildingTree(new VortiakWitchTree(agent, bbBase, witchBB))
                .AddSelector("main")
                    .AddSelector("entrance", new WitchStageDecorator(0, witchBB)) 
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(entrance, 1))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_entrance"))
                            .AddTask(new SetStageTask(1, witchBB))
                            .Up()
                        .Up()
                    .AddSelector("first platform", new WitchStageDecorator(1, witchBB))
                        .AddSequence("hit", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new TeleportTask(platformB, bbBase))
                            .AddTask(new SetHealthTask(300, bbBase))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_laugh"))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Cautious, bbBase))
                            .AddTask(new SetStageTask(2, witchBB))
                            .Up()
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(playerPositionToTeleportToPlatformB, 1))
                            .AddTask(new TeleportTask(platformB, bbBase))
                            .AddTask(new SetHealthTask(300, bbBase))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Cautious, bbBase))
                            .AddTask(new SetStageTask(2, witchBB))
                            .Up()
                        .Up()
                     .AddSelector("second platform", new WitchStageDecorator(2, witchBB))
                        .AddSequence("hit", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new TeleportTask(platformC, bbBase))
                            .AddTask(new SetHealthTask(100, bbBase))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_laugh"))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Cautious, bbBase))
                            .AddTask(new SetStageTask(3, witchBB))
                            .Up()
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(playerPositionToTeleportToPlatformC), 1)
                            .AddTask(new TeleportTask(platformC, bbBase))
                            .AddTask(new SetAiStateFlag(Agent.AIStateFlag.Cautious, bbBase))
                            .AddTask(new SetHealthTask(100, bbBase))
                            .AddTask(new SetStageTask(3, witchBB))
                            .Up()
                        .Up()
                    .AddSelector("demon summon", new WitchStageDecorator(3, witchBB))
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(playerPositionToStartStage3), 1)
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_demon_command"))
                            .AddTask(new SleepTask(TimeSpan.FromSeconds(demonCommand.Length)))
                            .AddTask(new PrepareAndTeleportNPCTask(demonSummonStringId, possibleTeleportLocations[0]))
                            .AddTask(new SetStageTask(4, witchBB))
                            .Up()
                        .Up()
                    .AddSelector("demon defeated", new WitchStageDecorator(4, witchBB))
                        .AddSequence("demon killed", new NPCKilledDecorator(demonSummonStringId, SubscriptionPossibilities.OnAgentRemoved), 1)
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_demon_defeated"))
                            .AddTask(new SleepTask(TimeSpan.FromSeconds(demonDefeated.Length)))
                            .AddTask(new TeleportTask(possibleTeleportLocations[0], bbBase))
                            .AddTask(new SetHealthTask(witchFinalFightHealth, bbBase))
                            .AddTask(new SetHealthLimitTask(witchFinalFightHealth, bbBase))
                            .AddTask(new SetStageTask(5, witchBB))
                            .Up()
                        .Up()
                    .AddSelector("witch fight", new WitchStageDecorator(5, witchBB))
                        .AddSequence("witch defeated", new BelowPercentageAfterHitDecorator(0.5f, SubscriptionPossibilities.OnSelfIsHit, bbBase), 1)
                            .AddTask(new TeleportTask(platformC, bbBase))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_defeated"))
                            .AddTask(new CompleteWitchQuestTask())
                            .Up()
                        .AddSequence("witch hit", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new TeleportToFurthestLocationFromPlayerTask(possibleTeleportLocations, bbBase))
                            .Up()
                        .Up()
                .Finish();
            return tree;
        }
    }
}
