using BehaviorTrees;
using BehaviorTrees.Nodes;
using RealmsForgotten.RFMissionLogic;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree
{
    internal class SummonTroopsTask : BTTask
    {
        public readonly List<Vec3> spawnTroopsLocations = new();
        readonly string troopStringId;
        public SummonTroopsTask(List<Vec3> possibleSpawnLocations, string ttroopStringId)
        {
            spawnTroopsLocations = possibleSpawnLocations;
            troopStringId = ttroopStringId;
        }

        public override BTTaskStatus Execute()
        {
            foreach (var location in spawnTroopsLocations)
                SpawnAgentMissionLogic.AddAgentToSpawn(new(troopStringId, Mission.Current.PlayerEnemyTeam, location, true));
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}