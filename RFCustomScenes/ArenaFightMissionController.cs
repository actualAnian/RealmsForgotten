using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.ObjectSystem;
using System.Text.RegularExpressions;
using SandBox.ViewModelCollection.Tournament;
using TaleWorlds.CampaignSystem.Encyclopedia;

namespace RFCustomSettlements
{
    internal class ArenaFightMissionController : MissionLogic
    {
        private List<GameEntity> spawnPoints;

        public void StartArenaBattle()
        {
            List<string> playerTeamTroops = new List<string>() { "looter", "looter" };
            List<string> enemyTeamTroops = new List<string>() { "looter", "looter" };

            int[] array = new int[]
{
                119,
                118,
                120,
                121
};
            int num = 0;
            //for (int i = 0; i < numberOfTeamsPerMatch; i++)
            //{
            //    this._teams[i] = new TournamentTeam(this._teamSize, BannerManager.GetColor(array[num]), Banner.CreateOneColoredEmptyBanner(array[num]));
            //    num++;
            //    num %= 4;
            //}

            base.Mission.SetMissionMode(MissionMode.Battle, true);
            List<Team> list = new List<Team>();
            //BannerManager.GetColor(array[num]), Banner.CreateOneColoredEmptyBanner(array[num]
            BattleSideEnum side = BattleSideEnum.Defender;
            Team team = base.Mission.Teams.Add(side, BannerManager.GetColor(array[0]), BannerManager.GetColor(array[0]), Banner.CreateOneColoredEmptyBanner(array[0]));
            GameEntity spawnPoint = spawnPoints[0];
            list.Add(team);
            SpawnPlayer(spawnPoint, team);
            foreach (string troopId in playerTeamTroops)
            {
                SpawnTroop(spawnPoint, team, troopId);
            }

            side = BattleSideEnum.Attacker;
            team = base.Mission.Teams.Add(side, BannerManager.GetColor(array[1]), BannerManager.GetColor(array[1]), Banner.CreateOneColoredEmptyBanner(array[1]));
            spawnPoint = spawnPoints[1];
            foreach (string troopId in enemyTeamTroops)
            {
                SpawnTroop(spawnPoint, team, troopId);
            }

            list.Add(team);
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    list[i].SetIsEnemyOf(list[j], true);
                }
            }
            //this._aliveParticipants = this._match.Participants.ToList<TournamentParticipant>();
            //this._aliveTeams = this._match.Teams.ToList<TournamentTeam>();
        }

        private void SpawnPlayer(GameEntity spawnPoint, Team team)
        {
            MatrixFrame frame = spawnPoint.GetGlobalFrame();
            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            frame.Strafe(MBRandom.RandomInt(-2, 2) * 1f);
            frame.Advance(MBRandom.RandomInt(0, 2) * 1f);
            CharacterObject character = Hero.MainHero.CharacterObject;
            AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Team(team).InitialPosition(frame.origin);
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(frame.rotation.f.AsVec2.Normalized()).ClothingColor1(team.Color).Banner(team.Banner).Controller(character.IsPlayerCharacter ? Agent.ControllerType.Player : Agent.ControllerType.AI);
            Agent agent = base.Mission.SpawnAgent(agentBuildData2, false);
            agent.Health = character.HeroObject.HitPoints;
            base.Mission.PlayerTeam = team;
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            base.Mission.CanAgentRout_AdditionalCondition += this.CanAgentRout;
        }

        private bool CanAgentRout(Agent agent)
        {
            return false;
        }
        protected override void OnEndMission()
        {
            base.Mission.CanAgentRout_AdditionalCondition -= this.CanAgentRout;
            switch (ArenaSettlementStateHandler.currentState)
            { 
                case ArenaSettlementStateHandler.ArenaState.FightStage1:
                    ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.FightStage2;
                    break;
                case ArenaSettlementStateHandler.ArenaState.FightStage2:
                    ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.FightStage3;
                    break;
                case ArenaSettlementStateHandler.ArenaState.FightStage3:
                    ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.Finishing;
                    break;
            }
        }
        public override void AfterStart()
        {
//            TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_fight"));
            spawnPoints = new List<GameEntity>();
            for (int i = 0; i < 4; i++)
            {
                GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_arena_" + (i + 1));
                if (gameEntity != null)
                {
                    spawnPoints.Add(gameEntity);
                }
            }
            StartArenaBattle();
        }
        private void SpawnTroop(GameEntity spawnPoint, Team team, string TroopStringId)
        {
            MatrixFrame frame = spawnPoint.GetGlobalFrame();
            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            frame.Strafe(MBRandom.RandomInt(-2, 2) * 1f);
            frame.Advance(MBRandom.RandomInt(0, 2) * 1f);
            CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>(TroopStringId);
            AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Team(team).InitialPosition(frame.origin);
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(frame.rotation.f.AsVec2.Normalized()).ClothingColor1(team.Color).Banner(team.Banner).Controller(character.IsPlayerCharacter ? Agent.ControllerType.Player : Agent.ControllerType.AI);
            Agent agent = base.Mission.SpawnAgent(agentBuildData2, false);
            agent.SetWatchState(Agent.WatchState.Alarmed);
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
        }

       // public override InquiryData OnEndMissionRequest(out bool canPlayerLeave) { canPlayerLeave = false; }
    }
}
