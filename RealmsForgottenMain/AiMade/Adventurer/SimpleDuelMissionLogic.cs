using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Adventurer
{
    public class SimpleDuelMissionLogic : MissionLogic
    {
        private readonly Hero _opponent;
        private Agent _playerAgent;
        private Agent _opponentAgent;

        public SimpleDuelMissionLogic(Hero opponent)
        {
            _opponent = opponent;
        }

        public override void AfterStart()
        {
            base.AfterStart();

            Vec3 playerPos = new Vec3(0f, -2f, 0f);
            Vec3 enemyPos = new Vec3(0f, 2f, 0f);

            _playerAgent = Mission.SpawnAgent(new AgentBuildData(Hero.MainHero.CharacterObject)
                .Team(Mission.Teams.Defender)
                .TroopOrigin(new SimpleAgentOrigin(Hero.MainHero.CharacterObject))
                .InitialPosition(playerPos)
                .InitialDirection(Vec2.Forward));

            _opponentAgent = Mission.SpawnAgent(new AgentBuildData(_opponent.CharacterObject)
                .Team(Mission.Teams.Attacker)
                .TroopOrigin(new SimpleAgentOrigin(_opponent.CharacterObject))
                .InitialPosition(enemyPos)
                .InitialDirection(-Vec2.Forward));

            _playerAgent.WieldInitialWeapons();
            _opponentAgent.WieldInitialWeapons();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_opponentAgent != null && _opponentAgent.State == AgentState.Killed)
            {
                InformationManager.DisplayMessage(new InformationMessage($"{_opponent.Name} was defeated!"));
                Mission.EndMission();
            }

            if (_playerAgent != null && _playerAgent.State == AgentState.Killed)
            {
                InformationManager.DisplayMessage(new InformationMessage($"You were defeated by {_opponent.Name}!"));
                Mission.EndMission();
            }
        }
    }
}
