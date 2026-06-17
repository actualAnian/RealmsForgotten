using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Encounters.Behaviors
{
    public class DuelMissionController : TaleWorlds.MountAndBlade.MissionLogic
    {
        private readonly CharacterObject duelOpponent;
        private readonly bool spawnOnHorse;
        private Agent playerAgent;
        private Agent opponentAgent;
        private bool isDuelEnded;
        private BasicMissionTimer endTimer;
        private bool isPlayerWinner;

        public DuelMissionController(CharacterObject opponent, bool onHorse)
        {
            duelOpponent = opponent;
            spawnOnHorse = onHorse;
        }

        public override void AfterStart()
        {
            base.AfterStart();
            Mission.Current.SetMissionMode(MissionMode.Duel, true);
            isDuelEnded = false;
            endTimer = new BasicMissionTimer();

            InitializeMissionTeams();
            GetSpawnFrames(out var playerSpawnFrame, out var opponentSpawnFrame);

            playerAgent = SpawnAgent(CharacterObject.PlayerCharacter, playerSpawnFrame);
            opponentAgent = SpawnAgent(duelOpponent, opponentSpawnFrame);
        }

        private void InitializeMissionTeams()
        {
            Mission.Current.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, Hero.MainHero.Clan.Banner, true, false, true);
            Mission.Current.Teams.Add(BattleSideEnum.Attacker, duelOpponent.HeroObject.MapFaction.Color, duelOpponent.HeroObject.MapFaction.Color2, duelOpponent.HeroObject.Clan.Banner, true, false, true);
            Mission.Current.PlayerTeam = Mission.Current.Teams.Defender;
        }

        private void GetSpawnFrames(out MatrixFrame playerSpawnFrame, out MatrixFrame opponentSpawnFrame)
        {
            if (PlayerEncounter.Current != null && PlayerEncounter.InsideSettlement)
            {
                var arenaSpawns = Mission.Current.Scene.FindEntitiesWithTag("sp_arena").Select(e => e.GetGlobalFrame()).ToList();
                for (int i = 0; i < arenaSpawns.Count; i++)
                {
                    var frame = arenaSpawns[i];
                    frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                    arenaSpawns[i] = frame;
                }
                playerSpawnFrame = arenaSpawns.GetRandomElement();
                arenaSpawns.Remove(playerSpawnFrame);
                opponentSpawnFrame = arenaSpawns.GetRandomElement();
            }
            else
            {
                var attackerInfantrySpawn = Mission.Current.Scene.FindEntityWithTag("attacker_infantry") ?? Mission.Current.Scene.FindEntityWithName("sp_attacker_infantry");
                Vec2 spawnPoint = attackerInfantrySpawn.GlobalPosition.AsVec2;

                Vec2 playerPosVec2 = new Vec2(spawnPoint.X, spawnPoint.Y - 5f);
                Vec2 opponentPosVec2 = new Vec2(spawnPoint.X, spawnPoint.Y + 5f);
                float playerHeight = 0, opponentHeight = 0;

                Mission.Current.Scene.GetHeightAtPoint(playerPosVec2, BodyFlags.CommonCollisionExcludeFlags, ref playerHeight);
                Mission.Current.Scene.GetHeightAtPoint(opponentPosVec2, BodyFlags.CommonCollisionExcludeFlags, ref opponentHeight);

                var playerPos = new Vec3(playerPosVec2.X, playerPosVec2.Y, playerHeight);
                var opponentPos = new Vec3(opponentPosVec2.X, opponentPosVec2.Y, opponentHeight);

                var playerRot = Mat3.Identity;
                playerRot.RotateAboutUp((opponentPos - playerPos).AsVec2.RotationInRadians);
                var oppRot = Mat3.Identity;
                oppRot.RotateAboutUp((playerPos - opponentPos).AsVec2.RotationInRadians);

                playerSpawnFrame = new MatrixFrame(playerRot, playerPos);
                opponentSpawnFrame = new MatrixFrame(oppRot, opponentPos);
            }
        }

        private Agent SpawnAgent(CharacterObject character, MatrixFrame frame)
        {
            var agentBuildData = new AgentBuildData(character)
                .BodyProperties(character.GetBodyPropertiesMax())
                .Team(character == CharacterObject.PlayerCharacter ? Mission.Current.PlayerTeam : Mission.Current.PlayerEnemyTeam)
                .InitialPosition(frame.origin)
                .InitialDirection(frame.rotation.f.AsVec2.Normalized())
                .NoHorses(!spawnOnHorse)
                .Equipment(character.FirstBattleEquipment)
                .TroopOrigin(new SimpleAgentOrigin(character));

            var agent = Mission.Current.SpawnAgent(agentBuildData);
            agent.FadeIn();

            if (agent.IsAIControlled)
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }
            agent.WieldInitialWeapons();
            return agent;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (isDuelEnded) return;

            if (affectedAgent == opponentAgent)
            {
                isPlayerWinner = true;
                isDuelEnded = true;
            }
            else if (affectedAgent == playerAgent)
            {
                isPlayerWinner = false;
                isDuelEnded = true;
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (isDuelEnded && endTimer.ElapsedTime > 4.0f)
            {
                EndDuel();
                endTimer.Reset();
            }
            else if (isDuelEnded)
            {
                MBInformationManager.AddQuickInformation(new TaleWorlds.Localization.TextObject("{=duel_has_ended}The duel has ended."));
            }
        }

        private void EndDuel()
        {
            var encounterBehavior = Campaign.Current.GetCampaignBehavior<EncounterSystemBehavior>();
            if (encounterBehavior != null)
            {
                encounterBehavior.EndDuel(isPlayerWinner);
            }
            Mission.Current.EndMission();
        }

        public override InquiryData OnEndMissionRequest(out bool canLeave)
        {
            canLeave = true;
            return null;
        }
    }
}