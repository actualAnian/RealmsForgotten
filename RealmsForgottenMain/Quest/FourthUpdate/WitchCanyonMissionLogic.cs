using BehaviorTrees;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.CanyonWitchTree;
using RealmsForgotten.RFMissionLogic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class WitchCanyonMissionLogic : MissionLogic
    {
        public static Vec3 WitchSpawnPosition = new(219.55f, 332.22f, 47.09f);
        float _timer = 0f;
        bool _isInitialized = false;
        
        public override void OnMissionTick(float dt)
        {
            _timer += dt;
            if (_timer < 1) return;
            if (!_isInitialized)
            {
                BTRegister.RegisterClass("CanyonWitchTree", objects => CanyonWitchTree.BuildTree(objects));
                SpawnAgentMissionLogic.AddAgentToSpawn(new("winged_witch_boss", false, WitchSpawnPosition, false, false, false, default, "CanyonWitchTree"));
                _isInitialized = true;
            }
        }
    }
}
