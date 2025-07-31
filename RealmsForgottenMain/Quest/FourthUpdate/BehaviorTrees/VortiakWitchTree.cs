using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using BehaviorTreeWrapper.Tasks;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Decorators;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks;
using RealmsForgotten.RFMissionLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees
{
    public class VortiakWitchTree : BehaviorTree, IBTBannerlordBase, IWitchTree
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

        public VortiakWitchTree(Agent agent) : base(2000)
        {
            Agent = new BTBlackboardValue<Agent>(agent);
            Stage = new BTBlackboardValue<int>(0);
        }

        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
        public BTBlackboardValue<int> _stage;
        public BTBlackboardValue<int> Stage { get => _stage; set => _stage = value; }

        public static new BehaviorTree? BuildTree(object[] objects)
        {
            if (objects[0] is not Agent agent) return null;

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
            VortiakWitchTree? tree = StartBuildingTree(new VortiakWitchTree(agent))
                .AddSelector("main")
                    .AddSelector("entrance", new WitchStageDecorator(0)) 
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(entrance, 1))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_entrance"))
                            .AddTask(new SetStageTask(1))
                            .Up()
                        .Up()
                    .AddSelector("first platform", new WitchStageDecorator(1))
                        .AddSequence("hit", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new TeleportTask(platformB))
                            .AddTask(new SetHealthTask(300))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_laugh"))
                            .AddTask(new SetAiStateFlag(TaleWorlds.MountAndBlade.Agent.AIStateFlag.Guard))
                            .AddTask(new SetStageTask(2))
                            .Up()
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(playerPositionToTeleportToPlatformB, 1))
                            .AddTask(new TeleportTask(platformB))
                            .AddTask(new SetHealthTask(300))
                            .AddTask(new SetAiStateFlag(TaleWorlds.MountAndBlade.Agent.AIStateFlag.Guard))
                            .AddTask(new SetStageTask(2))
                            .Up()
                        .Up()
                     .AddSelector("second platform", new WitchStageDecorator(2))
                        .AddSequence("hit", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new TeleportTask(platformC))
                            .AddTask(new SetHealthTask(100))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_laugh"))
                            .AddTask(new SetAiStateFlag(TaleWorlds.MountAndBlade.Agent.AIStateFlag.Guard))
                            .AddTask(new SetStageTask(3))
                            .Up()
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(playerPositionToTeleportToPlatformC), 1)
                            .AddTask(new TeleportTask(platformC))
                            .AddTask(new SetAiStateFlag(TaleWorlds.MountAndBlade.Agent.AIStateFlag.Guard))
                            .AddTask(new SetHealthTask(100))
                            .AddTask(new SetStageTask(3))
                            .Up()
                        .Up()
                    .AddSelector("demon summon", new WitchStageDecorator(3))
                        .AddSequence("playerLeavesPosition", new PlayerNearPointDecorator(playerPositionToStartStage3), 1)
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_demon_command"))
                            .AddTask(new SleepTask(TimeSpan.FromSeconds(demonCommand.Length)))
                            .AddTask(new PrepareAndTeleportNPCTask(demonSummonStringId, possibleTeleportLocations[0]))
                            .AddTask(new SetStageTask(4))
                            .Up()
                        .Up()
                    .AddSelector("demon defeated", new WitchStageDecorator(4))
                        .AddSequence("demon killed", new NPCKilledDecorator(demonSummonStringId, SubscriptionPossibilities.OnAgentRemoved), 1)
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_demon_defeated"))
                            .AddTask(new SleepTask(TimeSpan.FromSeconds(demonDefeated.Length)))
                            .AddTask(new TeleportTask(possibleTeleportLocations[0]))
                            .AddTask(new SetHealthTask(witchFinalFightHealth))
                            .AddTask(new SetHealthLimitTask(witchFinalFightHealth))
                            .AddTask(new SetStageTask(5))
                            .Up()
                        .Up()
                    .AddSelector("witch fight", new WitchStageDecorator(5))
                        .AddSequence("witch defeated", new BelowPercentageAfterHitDecorator(0.5f, SubscriptionPossibilities.OnSelfIsHit), 1)
                            .AddTask(new TeleportTask(platformC))
                            .AddTask(new PlaySoundEffectFollowingPlayerTask("witch_voice_defeated"))
                            .AddTask(new CompleteWitchQuestTask())
                            .Up()
                        .AddSequence("witch hit", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
                            .AddTask(new TeleportToFurthestLocationFromPlayerTask(possibleTeleportLocations))
                            .Up()
                        .Up()
                .Finish();
            return tree;
        }
    }
}
