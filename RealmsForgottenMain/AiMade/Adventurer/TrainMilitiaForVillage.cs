using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Adventurer
{
    public class TrainMilitiaForVillage : MissionLogic
    {
        private readonly Settlement _settlement;
        private bool _missionEnded = false;

        public TrainMilitiaForVillage(Settlement settlement)
        {
            _settlement = settlement;
        }

        public override void AfterStart()
        {
            InformationManager.DisplayMessage(new InformationMessage($"🛡 Training militia in {_settlement.Name}"));

            var spawnLogic = Mission.Current.GetMissionBehavior<MissionAgentSpawnLogic>();

            var playerTeam = Mission.Current.Teams.Add(BattleSideEnum.Attacker, 0, uint.MaxValue, null);
            var dummyTeam = Mission.Current.Teams.Add(BattleSideEnum.Defender, 1, uint.MaxValue, null);

            Mission.Current.PlayerTeam = playerTeam;
            Mission.Current.PlayerTeam.SetIsEnemyOf(dummyTeam, true);

            // Spawn player
            var playerData = new AgentBuildData(Hero.MainHero.CharacterObject)
                .Team(playerTeam)
                .InitialPosition(Mission.Current.GetRandomPositionAroundPoint(Vec3.Zero, 1f, 5f, true))
                .InitialDirection(Vec2.Forward)
                .TroopOrigin(new SimpleAgentOrigin(Hero.MainHero.CharacterObject));
            Mission.Current.SpawnAgent(playerData, false);

            // Spawn militia trainees
            var militia = CharacterObject.Find("villager");
            for (int i = 0; i < 3; i++)
            {
                var data = new AgentBuildData(militia)
                    .Team(playerTeam)
                    .InitialPosition(Mission.Current.GetRandomPositionAroundPoint(Vec3.Zero, 1f, 5f, true))
                    .InitialDirection(Vec2.Forward)
                    .TroopOrigin(new SimpleAgentOrigin(militia));
                Mission.Current.SpawnAgent(data, false);
            }

            // Spawn training enemies
            var looter = CharacterObject.Find("looter");
            for (int i = 0; i < 2; i++)
            {
                var data = new AgentBuildData(looter)
                    .Team(dummyTeam)
                    .InitialPosition(Mission.Current.GetRandomPositionAroundPoint(Vec3.Zero, 1f, 7f, true))
                    .InitialDirection(Vec2.Forward)
                    .TroopOrigin(new SimpleAgentOrigin(looter));
                Mission.Current.SpawnAgent(data, false);
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (_missionEnded) return;

            var remainingEnemies = Mission.Current.Agents
                .Where(agent => agent.Team?.IsEnemyOf(Mission.Current.PlayerTeam) == true && agent.IsActive())
                .ToList();

            if (remainingEnemies.Count == 0)
            {
                _missionEnded = true;
                InformationManager.DisplayMessage(new InformationMessage("✅ Training complete!"));
                Mission.Current.EndMission();
            }
        }
    }
}