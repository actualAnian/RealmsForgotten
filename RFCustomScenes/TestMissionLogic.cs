using RealmsForgotten.MusicSounds;
using RealmsForgotten.Quest.FourthUpdate;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RFCustomSettlements
{

    public class TestMissionLogic : MissionLogic
    {
        public override void OnMissionTick(float dt)
        {
            //var lolol = Mission.Current.Agents.First(a => a.Character.StringId.Contains("witch"));
            //float num = _bbBase.Agent.HealthLimit * _healthPercentageThreshold / 100f;
            //return _bbBase.Agent.Health < num;
            base.OnMissionTick(dt);
            //Agent.Main.Health = 1000;
            if (Agent.Main == null)
                return;
            if (Input.IsKeyPressed(InputKey.H))
            {
                //foreach(var agent in Mission.Current.AllAgents)
                //{
                //    Blow b = new(); 
                //    if (agent.GetDistanceTo(Agent.Main) < 10 && agent.Team != Agent.Main.Team)
                //        agent.Die(b);
                //}
                //RFMissionSoundManager? soundManager = Mission.Current.GetMissionBehavior<RFMissionSoundManager>();
                //if (soundManager == null || !soundManager.AddSoundEvent("medieval_alarm_horn", true));

                //Mission.Current.AllAgents[8].TeleportToPosition(Agent.Main.Position);
                //Agent.Main.TryToWieldWeaponInSlot(EquipmentIndex.Weapon1, Agent.WeaponWieldActionType.Instant, false);
                //var enemy = Mission.Current.PlayerEnemyTeam.ActiveAgents[0];
                //var meteor = MBObjectManager.Instance.GetObject<ItemObject>("meteor_test");
                //WingedWitchSpellsLogic.FireMeteor(enemy, Agent.Main.Position + new TaleWorlds.Library.Vec3(0,0, 20), meteor);
                //        //// closest
                //        Agent? closest = null;
                //        float closestDistance = float.MaxValue;

                    //        foreach (Agent a in Mission.Current.Agents)
                    //        {
                    //            if (a == null || a == Agent.Main)
                    //                continue;

                    //            float d = a.GetDistanceTo(Agent.Main);
                    //            if (d < closestDistance)
                    //            {
                    //                closestDistance = d;
                    //                closest = a;
                    //            }
                    //        }

                    //        //var nav = closest.GetComponent<CampaignAgentComponent>();
                    //        //closest.AIStateFlags |= Agent.AIStateFlag.Cautious;
                    //        //WorldPosition lastSuspiciousPosition = Agent.Main.GetWorldPosition();
                    //        //closest.SetAILastSuspiciousPosition(lastSuspiciousPosition, checkNavMeshForCorrection: false);
                    //        var obj = MBObjectManager.Instance.GetObject<ItemObject>("balrog_axe");
                    //        //MissionWeapon weapon = new(obj, null, null);
                    //        //var pos = new Vec3(5, 0, 0) + Agent.Main.Position;
                    //        //var rot = new Vec3(0, 0, 0);
                    //        //this.Mission.SpawnWeaponWithNewEntityAux(weapon, Mission.WeaponSpawnFlags.WithPhysics, new MatrixFrame(Mat3.CreateMat3WithForward(rot), pos), 0, null, false);

                    //        //Agent.Main.EquipWeaponToExtraSlotAndWield(ref weapon);
                    //        //var ab = weapon.GetWeaponData(false).WeaponFrame;
                    //        //closest.EquipWeaponToExtraSlotAndWield(ref weapon);
                    //        //closest.SetTargetPosition(closest.Position.AsVec2);

                    //        //closest.DisableScriptedMovement();
                    //        //closest.SetAgentFlags(AgentFlag.IsHumanoid);
                    //        //closest.SetIsAIPaused(true);
                    //        //closest.TryToWieldWeaponInSlot(EquipmentIndex.ExtraWeaponSlot, Agent.WeaponWieldActionType.WithAnimationUninterruptible, false);

                    //        //var foesThatCanHearHorn = Mission.Current.Agents.Where(agent => agent.IsEnemyOf(TaleWorlds.MountAndBlade.Agent.Main)
                    //        //&& agent.GetDistanceTo(closest) < 50
                    //        //&& agent != closest);
                    //        //foreach (var agent in foesThatCanHearHorn)
                    //        //{
                    //        //    var alarmedBehavior = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
                    //        //    agent.SetAlarmState(TaleWorlds.MountAndBlade.Agent.AIStateFlag.Cautious);
                    //        //    MethodInfo setterMethod = AccessTools.PropertySetter(typeof(AlarmedBehaviorGroup), "AlarmFactor");
                    //        //    setterMethod.Invoke(alarmedBehavior, new object[] { 1f});
                    //        //    WorldPosition lastSuspiciousPosition = closest.GetWorldPosition();
                    //        //    agent.SetAILastSuspiciousPosition(lastSuspiciousPosition, checkNavMeshForCorrection: false);
                    //        //}
                    //    Agent.Main.SetActionChannel(0, ActionIndexCache.Create("act_human_blow_horn"), true);
                    //InformationManager.DisplayMessage(new InformationMessage("TestMissionLogic: H key pressed"));
            }
        }
    }
}
