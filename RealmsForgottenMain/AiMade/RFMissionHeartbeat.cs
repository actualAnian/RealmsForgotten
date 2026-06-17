using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade
{
    public class RFMissionHeartbeat : MissionBehavior
    {
        private float _t;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            RFLogger.Log($"[Mission] AfterStart. Scene={Mission.Current?.SceneName ?? "null"}");
        }

        public override void OnMissionTick(float dt)
        {
            _t += dt;
            if (_t >= 1f)
            {
                _t = 0f;
                int agents = Mission.Current?.Agents?.Count ?? -1;
                int teams = Mission.Current?.Teams?.Count ?? -1;
                RFLogger.Log($"[Mission] Heartbeat. agents={agents} teams={teams}");
            }
        }
    }
}
