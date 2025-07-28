using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using RealmsForgotten.Utility.Magic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class TeleportTask : BTTask, IBTBannerlordBase
    {
        private Vec3 position;

        public TeleportTask(Vec3 position)
        {
            this.position = position;
        }

        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }



        public override BTTaskStatus Execute()
        {
            // add an effect 
            //

            // change equipment
            //ItemObject newItem = MBObjectManager.Instance.GetObject<ItemObject>("sturgia_noble_sword_1_t5");
            //Equipment eq = new();
            //EquipmentElement el = new(newItem);
            //eq[0] = el;
            //MissionEquipment Meq = new(eq, null);
            //Agent.GetValue().InitializeMissionEquipment(Meq, null);
            //Agent.GetValue().EquipItemsFromSpawnEquipment(false);
            //
            Teleport.TeleportToPosition(Agent.GetValue(), position);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}