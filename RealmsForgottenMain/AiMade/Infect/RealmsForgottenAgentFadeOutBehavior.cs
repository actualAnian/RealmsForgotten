using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Infect
{
    public class RealmsForgottenAgentFadeOutBehavior : MissionBehavior
    {
        private readonly Agent _agent;
        private float _timer;
        private readonly float _fadeDelay;

        public RealmsForgottenAgentFadeOutBehavior(Agent agent, float fadeDelaySeconds)
        {
            _agent = agent;
            _fadeDelay = fadeDelaySeconds;
            _timer = 0f;
        }

        public override void OnMissionTick(float dt)
        {
            if (_agent == null || !_agent.IsActive() || _agent.State == AgentState.Killed)
            {
                Mission.Current?.RemoveMissionBehavior(this);
                return;
            }

            _timer += dt;
            if (_timer >= _fadeDelay)
            {
                _agent.FadeOut(false, true);
                Mission.Current?.RemoveMissionBehavior(this);
            }
        }


        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }
}
