using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    public class AgentSpeedLimitMissionBehavior : MissionBehavior
    {
        private const float SpeedLimitForSpecificRace = 1.5f; // Example speed limit for the specific race
        private readonly int specificRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("half_giant"); // Replace with the actual race ID

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            base.AfterStart();
            Mission.Current.AddMissionBehavior(new AgentSpeedLimitMissionLogic(specificRaceId, SpeedLimitForSpecificRace));
        }

        private class AgentSpeedLimitMissionLogic : MissionBehavior
        {
            private readonly int _specificRaceId;
            private readonly float _speedLimitForSpecificRace;

            public AgentSpeedLimitMissionLogic(int specificRaceId, float speedLimitForSpecificRace)
            {
                _specificRaceId = specificRaceId;
                _speedLimitForSpecificRace = speedLimitForSpecificRace;
            }

            public override void OnAgentBuild(Agent agent, Banner banner)
            {
                if (agent.Character != null && agent.Character.Race == _specificRaceId)
                {
                    agent.SetMaximumSpeedLimit(_speedLimitForSpecificRace, isMultiplier: false);
                }
            }

            public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        }
    }
}