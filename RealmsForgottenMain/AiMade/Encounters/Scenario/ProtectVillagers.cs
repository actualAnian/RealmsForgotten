using RealmsForgotten.AiMade.Encounters.Interfaces;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.AgentOrigins;

namespace RealmsForgotten.AiMade.Encounters.Scenario
{
    public class ProtectVillagers : IEncounterScenario
    {
        public string Name => "Protect Villagers";

        public bool IsPossible(Settlement settlement)
        {
            return settlement.IsVillage && !settlement.IsUnderSiege;
        }

        public void OnStartEncounter(Settlement settlement)
        {
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Bandits Raid",
                    $"Bandits are attacking villagers near {settlement.Name}. Help them?",
                    true, true,
                    "Help",
                    "Ignore",
                    () => StartMission(settlement),
                    null
                )
            );
        }

        private void StartMission(Settlement settlement)
        {
            MissionState.OpenNew(
                "ProtectVillagers",
                new MissionInitializerRecord("arena"),
                mission => new MissionBehavior[]
                {
                    new ProtectVillagersLogic(settlement)
                }
            );
        }
    }

    public class ProtectVillagersLogic : MissionBehavior
    {
        private readonly Settlement _settlement;
        private bool _missionEnded = false;

        public ProtectVillagersLogic(Settlement settlement)
        {
            _settlement = settlement;
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            InformationManager.DisplayMessage(
                new InformationMessage($"🏹 Defend mission started outside {_settlement.Name}!")
            );

            var spawnLogic = Mission.Current.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();

            var playerTeam = Mission.Current.Teams.Add(BattleSideEnum.Attacker, 0, uint.MaxValue, null);
            var banditTeam = Mission.Current.Teams.Add(BattleSideEnum.Defender, 1, uint.MaxValue, null);

            Mission.Current.PlayerTeam = playerTeam;
            Mission.Current.PlayerTeam.SetIsEnemyOf(banditTeam, true);

            var playerData = new AgentBuildData(Hero.MainHero.CharacterObject)
                .Team(playerTeam)
                .InitialPosition(Mission.Current.GetRandomPositionAroundPoint(Vec3.Zero, 1f, 5f, true))
                .InitialDirection(Vec2.Forward)
                .TroopOrigin(new SimpleAgentOrigin(Hero.MainHero.CharacterObject));
            Mission.Current.SpawnAgent(playerData, false);

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

            var looter = CharacterObject.Find("looter");
            for (int i = 0; i < 4; i++)
            {
                var data = new AgentBuildData(looter)
                    .Team(banditTeam)
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
                InformationManager.DisplayMessage(new InformationMessage("✅ Villagers are safe!"));
                Mission.Current.EndMission();
            }
        }
    }
}
