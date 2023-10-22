using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using System;
using static RFCustomSettlements.ArenaBuildData;
using TaleWorlds.Localization;

namespace RFCustomSettlements
{
    internal class ArenaFightMissionController : MissionLogic
    {
        private readonly List<GameEntity> spawnPoints;
        private BasicMissionTimer? endTimer;
        private readonly List<ArenaTeam> aliveTeams;
        private bool isPlayerWinner = true;
        private readonly Action<bool> OnBattleEnd;
        private readonly Equipment playerEquipment;

        public ArenaFightMissionController(StageData stageData, Action<bool> onbattleend)
        {
            spawnPoints = new();
            aliveTeams = new();
            foreach (ArenaTeam team in stageData.ArenaTeams)
                aliveTeams.Add(((ArenaTeam)team.Clone()));
            OnBattleEnd = onbattleend;
            playerEquipment = stageData.playerEquipment;
        }
        public void StartArenaBattle()
        {
            base.Mission.SetMissionMode(MissionMode.Battle, true);
            List<GameEntity>.Enumerator spawnPointEnum = spawnPoints.GetEnumerator();
            GameEntity? spawnPoint;

            foreach (ArenaTeam arenaTeam in aliveTeams)
            {
                spawnPoint = spawnPointEnum.MoveNext() ? spawnPointEnum.Current : null;
                if (spawnPoint == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("error spawning arena tropps", new Color(1, 0, 0)));
                    break;
                }
                BattleSideEnum side = arenaTeam.IsPlayerTeam ? BattleSideEnum.Defender : BattleSideEnum.Attacker; 
                Team team = Mission.Teams.Add(side, arenaTeam.TeamColor, arenaTeam.TeamColor, arenaTeam.TeamBanner);
                arenaTeam.SetTeam(team);
                arenaTeam.MissionTeam = team;
                foreach (CharacterObject troop in arenaTeam.members)
                {
                    SpawnTroop(spawnPoint, team, troop);
                }
            }

            for (int i = 0; i < aliveTeams.Count; i++)
            {
                for (int j = i + 1; j < aliveTeams.Count; j++)
                {
                    aliveTeams[i].SetIsEnemyOf(aliveTeams[j]);
                }
            }
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
            OnBattleEnd(isPlayerWinner);
        }
        public override void AfterStart()
        {
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
        private void SpawnTroop(GameEntity spawnPoint, Team team, CharacterObject troop)
        {
            MatrixFrame frame = spawnPoint.GetGlobalFrame();
            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            frame.Strafe(MBRandom.RandomInt(-2, 2) * 1f);
            frame.Advance(MBRandom.RandomInt(0, 2) * 1f);
            AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(troop, -1, null, default)).Team(team).InitialPosition(frame.origin);
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(frame.rotation.f.AsVec2.Normalized()).ClothingColor1(team.Color).Banner(team.Banner).Controller(troop.IsPlayerCharacter ? Agent.ControllerType.Player : Agent.ControllerType.AI);
            if (troop.IsPlayerCharacter) agentBuildData2 = agentBuildData2.Equipment(playerEquipment);
            Agent agent = Mission.SpawnAgent(agentBuildData2, false);
            if(agent.IsPlayerControlled)
            {
                agent.Health = agent.HealthLimit;
                //agent.Health = troop.HeroObject.HitPoints;
                Mission.PlayerTeam = team;
            }
            else agent.SetWatchState(Agent.WatchState.Alarmed);
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            foreach(ArenaTeam arenaTeam in aliveTeams)
            {
                if (arenaTeam.MissionTeam == affectedAgent.Team)
                { 
                    arenaTeam.RemoveMember();
                    if (arenaTeam.hasNoMembers()) aliveTeams.Remove(arenaTeam);
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
            if (endTimer != null && endTimer.ElapsedTime > 6f) return true;
            else if (IsOneTeamRemaining() && endTimer == null)
            {
                isPlayerWinner = aliveTeams[0].IsPlayerTeam;
                endTimer = new BasicMissionTimer();
                if(isPlayerWinner) MBInformationManager.AddQuickInformation(new TextObject("Your team has won, glory and fame to you!", null), 0, null, "");
                else MBInformationManager.AddQuickInformation(new TextObject("Your team lost, you are a disgrace, and at mercy of your opponent", null), 0, null, "");
            }
            return false;
        }

        private bool IsOneTeamRemaining()
        {
            return aliveTeams.Count == 1;
        }
    }
}
