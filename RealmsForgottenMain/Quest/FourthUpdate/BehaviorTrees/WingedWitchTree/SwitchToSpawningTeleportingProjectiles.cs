using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree
{
    public class SwitchToSpawningTeleportingProjectiles : BTTask
    {
        public override BTTaskStatus Execute()
        {
            if (Mission.Current.MissionLogics.FirstOrDefault(l => l is WingedWitchFinalMissionLogic) is not WingedWitchFinalMissionLogic logic)
            {
                InformationManager.DisplayMessage(new("Error, WingedWitchFinalMissionLogic not loaded"));
                return BTTaskStatus.FinishedWithFalse;
            }
            logic.SpawnTeleportingProjectiles = true;
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}