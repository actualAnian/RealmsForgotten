using HarmonyLib;
using RealmsForgotten.MusicSounds;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten._1_3_beta_patches
{
    public class TestMissionLogic : MissionLogic
    {
        //SpawnedItemEntity rememberedScript;
        //public override void OnMissionTick(float dt)
        //{
        //    if (Agent.Main == null)
        //        return;
        //    if (Input.IsKeyPressed(InputKey.H))
        //    {
        //        var obj = MBObjectManager.Instance.GetObject<ItemObject>("vlandia_sword_2_t3");
        //        MissionWeapon weapon = new(obj, null, null);
        //        var pos = new Vec3(5, 0, 0) + Agent.Main.Position;
        //        var rot = new Vec3(0, 0, 0);
        //        var entity = Mission.SpawnWeaponWithNewEntityAux(weapon, Mission.WeaponSpawnFlags.WithPhysics, new MatrixFrame(Mat3.CreateMat3WithForward(rot), pos), 0, null, false);
        //        var script = entity.GetFirstScriptOfType<SpawnedItemEntity>();
        //        rememberedScript = script;
        //        InformationManager.DisplayMessage(new InformationMessage("TestMissionLogic: H key pressed"));

        //    }
        //}
    }
}
