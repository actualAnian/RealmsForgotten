using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using RealmsForgotten.RFCustomSettlements;
using System;
using static RFCustomSettlements.ArenaBuildData;
using static System.Net.Mime.MediaTypeNames;

namespace RFCustomSettlements
{
    internal class ArenaFightMissionController : MissionLogic
    {
        private List<GameEntity> spawnPoints;
        private ArenaSettlementStateHandler Arenahandler;
        private BasicMissionTimer _endTimer;
        private List<ArenaTeam> battleTeams;
        private List<ArenaTeam> _aliveTeams;

        public ArenaFightMissionController(ArenaSettlementStateHandler handler, List<ArenaTeam> arenaTeams)
        {
            Arenahandler = handler;
            battleTeams = arenaTeams;
        }
        public void StartArenaBattle(StageData stageData)
        {
            base.Mission.SetMissionMode(MissionMode.Battle, true);
            List<GameEntity>.Enumerator spawnPointEnum = spawnPoints.GetEnumerator();
            GameEntity? spawnPoint;

            foreach (ArenaTeam arenaTeam in stageData.ArenaTeams)
            {
                spawnPoint = spawnPointEnum.MoveNext() ? spawnPointEnum.Current : null;
                if (spawnPoint == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("error spawning arena tropps", new Color(1, 0, 0)));
                    break;
                }
                BattleSideEnum side = arenaTeam.IsPlayerTeam ? BattleSideEnum.Defender : BattleSideEnum.Attacker; 
                Team team = Mission.Teams.Add(side, arenaTeam.TeamColor, arenaTeam.TeamColor, arenaTeam.TeamBanner);
                if (arenaTeam.IsPlayerTeam) SpawnPlayer(spawnPoint, team);
                arenaTeam.MissionTeam = team;
                foreach (CharacterObject troop in arenaTeam.members)
                {
                    SpawnTroop(spawnPoint, team, troop);
                }
            }

            for (int i = 0; i < stageData.ArenaTeams.Count; i++)
            {
                for (int j = i + 1; j < stageData.ArenaTeams.Count; j++)
                {
                    stageData.ArenaTeams[i].SetIsEnemyOf(stageData.ArenaTeams[j]);
                }
            }
            //this._aliveParticipants = this._match.Participants.ToList<TournamentParticipant>();
            _aliveTeams = stageData.ArenaTeams;
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
            if (true) // if player won the battle
                Arenahandler.OnPlayerBattleWin();
            else Arenahandler.OnPlayerBattleLoss();
        }
        public override void AfterStart()
        {
            spawnPoints = new List<GameEntity>();
            for (int i = 0; i < 4; i++)
            {
                GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_arena_" + (i + 1));
                if (gameEntity != null)
                {
                    spawnPoints.Add(gameEntity);
                }
            }
            StartArenaBattle(Arenahandler.BuildData.Challenges[0].StageDatas[0]);
        }
        private void SpawnTroop(GameEntity spawnPoint, Team team, CharacterObject troop)
        {
            MatrixFrame frame = spawnPoint.GetGlobalFrame();
            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            frame.Strafe(MBRandom.RandomInt(-2, 2) * 1f);
            frame.Advance(MBRandom.RandomInt(0, 2) * 1f);
            AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(troop, -1, null, default(UniqueTroopDescriptor))).Team(team).InitialPosition(frame.origin);
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(frame.rotation.f.AsVec2.Normalized()).ClothingColor1(team.Color).Banner(team.Banner).Controller(troop.IsPlayerCharacter ? Agent.ControllerType.Player : Agent.ControllerType.AI);
            Agent agent = base.Mission.SpawnAgent(agentBuildData2, false);
            agent.SetWatchState(Agent.WatchState.Alarmed);
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
        }
        public override void OnAgentDeleted(Agent affectedAgent)
        {
            foreach(ArenaTeam arenaTeam in _aliveTeams)
            {
                if (arenaTeam.MissionTeam == affectedAgent.Team)
                { 
                    arenaTeam.RemoveMember();
                    if (arenaTeam.hasNoMembers()) _aliveTeams.Remove(arenaTeam);
                    break;
                }
            }
        }
        // public override InquiryData OnEndMissionRequest(out bool canPlayerLeave) { canPlayerLeave = false; }
        public override void OnMissionTick(float dt)
        {
            if (MatchEnded())
                EndMatch();
        }

        private void EndMatch()
        {
            Mission.Current.EndMission();
        }

        private bool MatchEnded()
        {
            if (_endTimer != null && _endTimer.ElapsedTime > 6f) return true;
            else if (!AreThereEnemies())
            {
                _endTimer = new BasicMissionTimer();
            }
            return false;
        }

        private bool AreThereEnemies()
        {
            return _aliveTeams.Count != 1;
        }
    }
}
