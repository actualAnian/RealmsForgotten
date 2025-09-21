//@TODO what is that
//using TaleWorlds.Library;
//using TaleWorlds.MountAndBlade;

//namespace RealmsForgotten.AiMade
//{
//    public class AttachWallSegmentDebugBehavior : MissionBehavior
//    {
//        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

//        public override void OnMissionTick(float dt)
//        {
//            var entities = Mission.Current.Scene.FindEntitiesWithTag("wall_debug");

//            foreach (var entity in entities)
//            {
//                if (!entity.HasScriptOfType<WallSegmentDebug>())
//                {
//                    entity.CreateAndAddScriptComponent("RealmsForgotten.WallSegmentDebug");
//                    InformationManager.DisplayMessage(new InformationMessage("✅ WallSegmentDebug attached to: " + entity.Name));
//                }
//            }

//            // Only run once
//            Mission.Current.RemoveMissionBehavior(this);
//        }
//    }
//}