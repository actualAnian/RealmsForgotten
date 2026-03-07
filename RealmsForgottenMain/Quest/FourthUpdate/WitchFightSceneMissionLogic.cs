using BehaviorTrees;
using BehaviorTreeWrapper;
using psai.net;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class WitchFightSceneMissionLogic : MissionLogic
    {
        int _balrogHealth = 300;
        float timer;
        bool isInitialized = false;
        bool _isPlayerDead = false;
        float _playerDeadTimer = 0f;
        public override void OnMissionTick(float dt)
        {
            string demonId = VortiakWitchTree.demonSummonStringId;
            Agent balrog = Mission.Agents.FirstOrDefault(agent => agent.Character?.StringId == demonId);
            timer += dt;
            if (timer < 1) return;
            base.OnMissionTick(dt);
            // Check if the mission is over
            if (!isInitialized)
            {
                PsaiCore.Instance.TriggerMusicTheme(41, 0);
                Agent witch = Mission.Agents.FirstOrDefault(agent => agent.Character?.StringId == "evil_witch");
                witch.TeleportToPosition(VortiakWitchTree.platformA);
                balrog.Health = _balrogHealth;
                balrog.TeleportToPosition(VortiakWitchTree.platformC);
                isInitialized = true;
                InitializeWitch(witch);
            }

            if (_isPlayerDead) //kill the player
            {
                _playerDeadTimer += dt;
                if (_playerDeadTimer > 4)
                {
                    _isPlayerDead = false;
                    KillCharacterAction.ApplyByWounds(Hero.MainHero, true);
                }
            }
            //if (Input.IsKeyPressed(InputKey.G))
            //{
            //    Agent.Main.TeleportToPosition(VortiakWitchTree.entrance);
            //}
            //if (Input.IsKeyPressed(InputKey.H))
            //{
            //    Agent.Main.TeleportToPosition(VortiakWitchTree.platformA);
            //}
            //if (Input.IsKeyPressed(InputKey.J))
            //{
            //    Agent.Main.TeleportToPosition(VortiakWitchTree.platformB);
            //}
            //if (Input.IsKeyPressed(InputKey.K))
            //{
            //    Agent.Main.TeleportToPosition(VortiakWitchTree.playerPositionToStartStage3);
            //    //MBMusicManager.Current.StartThemeWithConstantIntensity(MusicTheme.MainTheme);
            //    //PsaiProject.LoadProjectFromXmlFile()
            //}
        }
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.IsHero && affectedAgent.Character.StringId == Hero.MainHero.CharacterObject.StringId)
            {
                _isPlayerDead = true;
            }
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }
        public void InitializeWitch(Agent witch)
        {
            witch.Health = witch.HealthLimit * 3;
            BTRegister.RegisterClass("VortiakWitchTree", objects => VortiakWitchTree.BuildTree(objects));
            witch.AddComponent(new BehaviorTreeAgentComponent(witch, "VortiakWitchTree"));
        }
    }
}
