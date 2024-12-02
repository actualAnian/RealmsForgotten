using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TaleWorlds.CampaignSystem.Issues.ScoutEnemyGarrisonsIssueBehavior;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Localization;

namespace RealmsForgotten.Quest.KnightQuest
{
    internal class KnightQuestDuelMissionController : MissionLogic
    {
        Agent? _player;
        Agent? _knightMaester;
        BasicMissionTimer? _missionEndTimer;
        CharacterObject knightMaester;
        bool playerWon;
        bool duelOnHorse;
        private readonly Action<bool> _onFinishedAction;
        public KnightQuestDuelMissionController(CharacterObject knightMaester, bool duelOnHorse, Action<bool> onFinishedAction)
        {
            this.knightMaester = knightMaester;
            _onFinishedAction = onFinishedAction;
            this.duelOnHorse = duelOnHorse;
        }
        public override void AfterStart()
        {
            _missionEndTimer = null;
            InitializeTeams();
            List<MatrixFrame> list = (from e in Mission.Current.Scene.FindEntitiesWithTag("sp_arena_respawn")
                                        select e.GetGlobalFrame()).ToList<MatrixFrame>();
            MatrixFrame matrixFrame = list[MBRandom.RandomInt(list.Count)];
            float maxValue = float.MaxValue;
            MatrixFrame frame = matrixFrame;
            foreach (MatrixFrame matrixFrame2 in list)
            {
                if (matrixFrame != matrixFrame2)
                {
                    Vec3 origin = matrixFrame2.origin;
                    if (origin.DistanceSquared(matrixFrame.origin) < maxValue)
                    {
                        frame = matrixFrame2;
                    }
                }
            }
            matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            _player = SpawnArenaAgent(CharacterObject.PlayerCharacter, Mission.Current.PlayerTeam, matrixFrame, duelOnHorse);
            _knightMaester = SpawnArenaAgent(knightMaester, Mission.Current.PlayerEnemyTeam, frame, duelOnHorse);
        }
        private void InitializeTeams()
        {
            Mission.Current.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
            Mission.Current.Teams.Add(BattleSideEnum.Attacker, Hero.MainHero.MapFaction.Color2, Hero.MainHero.MapFaction.Color, null, true, false, true);
            Mission.Current.PlayerTeam = Mission.Current.DefenderTeam;
        }
        private Agent SpawnArenaAgent(CharacterObject character, Team team, MatrixFrame frame, bool duelOnHorse)
        {
            Equipment equipment;
            try
            {
                if (character == CharacterObject.PlayerCharacter)
                {
                    equipment = character.Equipment;
                }
                else
                {
                    MBEquipmentRoster equipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("knight_maester_armor");
                    equipment = equipmentRoster.GetBattleEquipments().First();
                }
            }
            catch(Exception)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error, Could not find an equipment roster with id \"knight_maester_armor\"."));
                equipment = Settlement.CurrentSettlement.Culture.DuelPresetEquipmentRoster.GetBattleEquipments().GetRandomElementInefficiently();
            }

            Mission mission = Mission.Current;
            AgentBuildData agentBuildData = new AgentBuildData(character).Team(team).ClothingColor1(team.Color).ClothingColor2(team.Color2).InitialPosition(frame.origin);
            Vec2 vec = frame.rotation.f.AsVec2;
            vec = vec.Normalized();
            Agent agent = mission.SpawnAgent(agentBuildData.InitialDirection(vec).NoHorses(!duelOnHorse).Equipment(equipment).TroopOrigin(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Controller((character == CharacterObject.PlayerCharacter) ? Agent.ControllerType.Player : Agent.ControllerType.AI), false);
            if (agent.IsAIControlled)
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }
            return agent;
        }
        public override InquiryData OnEndMissionRequest(out bool canPlayerLeave) 
        {
            canPlayerLeave = false;
            return null;
        }
        public override void OnMissionTick(float dt)
        {
            if (_missionEndTimer != null && _missionEndTimer.ElapsedTime > 8f)
            {
                _onFinishedAction?.Invoke(playerWon);
                Mission.Current.EndMission();
                return;
            }
            if (_missionEndTimer == null && ((_player != null && !_player.IsActive()) || (_knightMaester != null && !_knightMaester.IsActive())))
            {
                playerWon = (_player != null && _player.IsActive());
                string message = playerWon ? "The crowd is roaring, as the undefeated maester falls!" : "The crowd cheers at the great spectacle, though with some disappointment that the undefeated champion remains without a loss.";
                MBInformationManager.AddQuickInformation(new TextObject(message));
                _missionEndTimer = new BasicMissionTimer();
            }
        }
    }
}
