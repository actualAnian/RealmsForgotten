using BehaviorTrees;
using BehaviorTreeWrapper;
using RealmsForgotten.MissionLogic;
using RealmsForgotten.Quest.FourthUpdate.BehaviorTrees;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class WitchFightSceneMissionLogic : TaleWorlds.MountAndBlade.MissionLogic
    {
        float timer;
        bool isInitialized = false;

        public override void OnMissionTick(float dt)
        {
            timer += dt;
            if (timer < 1) return;
            base.OnMissionTick(dt);
            // Check if the mission is over
            if (!isInitialized)
            {
                Agent witch = Mission.Agents.FirstOrDefault(agent => agent.Character?.StringId == "evil_witch");
                witch.TeleportToPosition(VortiakWitchTree.platformA);
                string demonId = VortiakWitchTree.demonSummonStringId;
                Agent balrog = Mission.Agents.FirstOrDefault(agent => agent.Character?.StringId == demonId);
                balrog.TeleportToPosition(VortiakWitchTree.platformC);
                isInitialized = true;
                InitializeWitch(witch);
            }
            if(Input.IsKeyPressed(InputKey.G))
            {
                Agent.Main.TeleportToPosition(VortiakWitchTree.entrance);
            }
            if (Input.IsKeyPressed(InputKey.H))
            {
                Agent.Main.TeleportToPosition(VortiakWitchTree.platformA);
            }
            if (Input.IsKeyPressed(InputKey.J))
            {
                Agent.Main.TeleportToPosition(VortiakWitchTree.platformB);
            }
            if (Input.IsKeyPressed(InputKey.K))
            {
                Agent.Main.TeleportToPosition(VortiakWitchTree.playerPositionToStartStage3);
            }

        }
        public void InitializeWitch(Agent witch)
        {
            witch.Health = witch.HealthLimit * 3;
            BTRegister.RegisterClass("VortiakWitchTree", objects => VortiakWitchTree.BuildTree(objects));
            witch.AddComponent(new BehaviorTreeAgentComponent(witch, "VortiakWitchTree"));
        }
        public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
        {
            base.OnAgentAlarmedStateChanged(agent, flag);
        }
    }
}
