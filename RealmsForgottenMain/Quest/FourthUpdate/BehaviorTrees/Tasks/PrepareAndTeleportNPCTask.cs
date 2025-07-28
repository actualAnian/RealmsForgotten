using BehaviorTrees;
using BehaviorTrees.Nodes;
using RealmsForgotten.Utility.Magic;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.Tasks
{
    public class PrepareAndTeleportNPCTask : BTTask
    {
        private Vec3 _position;
        private string _npcId;

        public PrepareAndTeleportNPCTask(string npcId, Vec3 position)
        {
            _position = position;
            _npcId = npcId;
        }
        public override BTTaskStatus Execute()
        {
            Agent agent = Mission.Current.Agents.Find(agent => agent.Character?.StringId == _npcId);
            if (agent == null)
                return BTTaskStatus.FinishedWithFalse;

            agent.StopUsingGameObject();
            agent.SetWatchState(Agent.WatchState.Alarmed);
            //agent.AIStateFlags |= Agent.AIStateFlag.Guard;
            //agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
            //agent.AIStateFlags &= ~Agent.AIStateFlag.UseObjectUsing;

            Teleport.TeleportToPosition(agent, _position);
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}