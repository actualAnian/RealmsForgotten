using RealmsForgotten.AiMade.Encounters.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.AgentOrigins;

namespace RealmsForgotten.AiMade.Encounters.Scenario
{
    public class TrainMilitia : IEncounterScenario
    {
        public string Name => "Train Local Militia";

        public bool IsPossible(Settlement settlement)
        {
            return settlement.IsTown &&
                   settlement.Notables.Any() &&
                   !settlement.IsUnderSiege &&
                   Hero.MainHero.Clan.Tier >= 2;
        }

        public void OnStartEncounter(Settlement settlement)
        {
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Militia Request",
                    $"A town notable asks if you'll help train the local militia.",
                    true, true,
                    "Help Them Train",
                    "Decline",
                    () => StartMilitiaMission(settlement),
                    null
                )
            );
        }

        private void StartMilitiaMission(Settlement settlement)
        {
            InformationManager.DisplayMessage(new InformationMessage("🛡 Training militia in " + settlement.Name));

            MissionState.OpenNew(
                "TrainMilitia",
                new MissionInitializerRecord("arena"),
                mission => new MissionBehavior[]
                {
                    new TrainMilitiaForVillage(settlement)
                }
            );
        }
    }

    public class TrainMilitiaForVillage : MissionBehavior
    {
        private readonly Settlement _settlement;
        private bool _missionEnded = false;
        private MissionTime _startTime;

        public TrainMilitiaForVillage(Settlement settlement)
        {
            _settlement = settlement;
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            _startTime = MissionTime.Now;

            InformationManager.DisplayMessage(new InformationMessage($"🛡 Training mission started in {_settlement.Name}!"));

            var team = Mission.Current.Teams.Add(BattleSideEnum.Attacker, 0, uint.MaxValue, null);
            Mission.Current.PlayerTeam = team;

            var playerData = new AgentBuildData(Hero.MainHero.CharacterObject)
                .Team(team)
                .InitialPosition(Mission.Current.GetRandomPositionAroundPoint(Vec3.Zero, 1f, 3f, true))
                .InitialDirection(Vec2.Forward)
                .TroopOrigin(new SimpleAgentOrigin(Hero.MainHero.CharacterObject));
            Mission.Current.SpawnAgent(playerData, false);

            var militia = CharacterObject.Find("villager");
            for (int i = 0; i < 5; i++)
            {
                var data = new AgentBuildData(militia)
                    .Team(team)
                    .InitialPosition(Mission.Current.GetRandomPositionAroundPoint(Vec3.Zero, 2f, 6f, true))
                    .InitialDirection(Vec2.Forward)
                    .TroopOrigin(new SimpleAgentOrigin(militia));
                Mission.Current.SpawnAgent(data, false);
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (_missionEnded) return;

            if (MissionTime.Now - _startTime > MissionTime.Seconds(30f))
            {
                _missionEnded = true;
                InformationManager.DisplayMessage(new InformationMessage("✅ Training session complete!"));
                Mission.Current.EndMission();
            }
        }
    }
}