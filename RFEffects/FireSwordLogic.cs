#region assembly FireLord2, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// C:\Users\Pedrinho\Desktop\FireLord2.dll
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace FireLord
{

    public static class FireSwordProperties
    {
        public static int FireSwordLightColorR { get; set; } = 216;

        public static int FireSwordLightColorG { get; set; } = 138;
        public static int FireSwordLightColorB { get; set; } = 0;
        public static Vec3 FireSwordLightColor => new Vec3((float)FireSwordLightColorR, (float)FireSwordLightColorG, (float)FireSwordLightColorB);
        public static float FireSwordLightRadius { get; set; } = 5f;
        public static float FireSwordLightIntensity { get; set; } = 4f;
        public static bool IgniteTargetWithFireSword { get; set; } = true;
        public static float IgnitionPerFireSwordHit { get; set; } = 100f;
        public static bool IgnitePlayerBody { get; set; } = false;

        public static InputKey FireSwordToggleKey { get; set; } = (InputKey)Enum.Parse(typeof(InputKey), "C");
    }
    internal class FireSwordLogic : MissionLogic
    {
        public class AgentFireSwordData
        {
            public bool enabled;

            public Agent agent;

            public GameEntity entity;

            public Light light;

            public bool dropLock;

            public MissionTimer timer;

            public bool lastWieldedWeaponEmpty;

            public void OnAgentWieldedItemChange()
            {
                if (dropLock || agent == null)
                {
                    return;
                }

                MissionWeapon wieldedWeapon = agent.WieldedWeapon;
                MissionWeapon wieldedOffhandWeapon = agent.WieldedOffhandWeapon;
                if (lastWieldedWeaponEmpty && !wieldedWeapon.IsEmpty)
                {
                    if (!agent.IsMainAgent || _playerFireSwordEnabled)
                    {
                        timer = new MissionTimer(0.1f);
                    }
                }
                else
                {
                    SetFireSwordEnable(enable: false);
                }

                lastWieldedWeaponEmpty = wieldedWeapon.IsEmpty;
            }

            public void OnAgentHealthChanged(Agent agent, float oldHealth, float newHealth)
            {
                if (!((double)newHealth > 0.0))
                {
                    SetFireSwordEnable(enable: false);
                }
            }

            public static string particleName = "ice_flame";
            public void SetFireSwordEnable(bool enable)
            {
                if (agent == null)
                {
                    return;
                }

                if (enable)
                {
                    if (agent.State == AgentState.Routed)
                    {
                        return;
                    }

                    SetFireSwordEnable(enable: false);
                    EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                    if (wieldedItemIndex == EquipmentIndex.None)
                    {
                        return;
                    }

                    GameEntity weaponEntityFromEquipmentSlot = agent.GetWeaponEntityFromEquipmentSlot(wieldedItemIndex);
                    MissionWeapon wieldedWeapon = agent.WieldedWeapon;
                    if (wieldedWeapon.IsEmpty)
                    {
                        return;
                    }

                    if (wieldedWeapon.Item?.StringId.Contains("_fire") == false)
                        return;

                    int num = (int)Math.Round((double)wieldedWeapon.GetWeaponStatsData()[0].WeaponLength / 10.0);
                    MBAgentVisuals agentVisuals = agent.AgentVisuals;
                    if ((object)agentVisuals == null)
                    {
                        return;
                    }



                    Skeleton skeleton = agentVisuals.GetSkeleton();
                    Light light = Light.CreatePointLight(FireSwordProperties.FireSwordLightRadius);
                    light.Intensity = FireSwordProperties.FireSwordLightIntensity;
                    light.LightColor = FireSwordProperties.FireSwordLightColor;
                    switch (wieldedWeapon.CurrentUsageItem.WeaponClass)
                    {
                        default:
                            return;
                        case WeaponClass.OneHandedSword:
                        case WeaponClass.TwoHandedSword:
                        case WeaponClass.Mace:
                        case WeaponClass.TwoHandedMace:
                            {
                                for (int i = 1; i < num; i++)
                                {
                                    MatrixFrame matrixFrame4 = new MatrixFrame(Mat3.Identity, default(Vec3));
                                    MatrixFrame boneLocalFrame2 = matrixFrame4.Elevate((float)i * 0.1f);
                                    ParticleSystem component2 = ParticleSystem.CreateParticleSystemAttachedToEntity(particleName, weaponEntityFromEquipmentSlot, ref boneLocalFrame2);
                                    skeleton.AddComponentToBone(Game.Current.DefaultMonster.MainHandItemBoneIndex, component2);
                                }

                                Light light3 = light;
                                MatrixFrame matrixFrame6 = (light3.Frame = light.Frame.Elevate((float)(num - 1) * 0.1f));
                                break;
                            }
                        case WeaponClass.OneHandedAxe:
                        case WeaponClass.TwoHandedAxe:
                        case WeaponClass.OneHandedPolearm:
                        case WeaponClass.TwoHandedPolearm:
                        case WeaponClass.LowGripPolearm:
                            {
                                int num2 = ((num > 19) ? 9 : ((num > 15) ? 6 : ((num > 12) ? 5 : ((num > 10) ? 4 : 3))));
                                int num3 = num - 1;
                                while (num3 > 0 && num3 > num - num2)
                                {
                                    MatrixFrame matrixFrame = new MatrixFrame(Mat3.Identity, default(Vec3));
                                    MatrixFrame boneLocalFrame = matrixFrame.Elevate((float)num3 * 0.1f);
                                    ParticleSystem component = ParticleSystem.CreateParticleSystemAttachedToEntity(particleName, weaponEntityFromEquipmentSlot, ref boneLocalFrame);
                                    skeleton.AddComponentToBone(Game.Current.DefaultMonster.MainHandItemBoneIndex, component);
                                    num3--;
                                }

                                Light light2 = light;
                                MatrixFrame matrixFrame3 = (light2.Frame = light.Frame.Elevate((float)(num - 1) * 0.1f));
                                break;
                            }
                        case WeaponClass.Pick:
                            return;
                    }

                    skeleton.AddComponentToBone(Game.Current.DefaultMonster.MainHandItemBoneIndex, light);
                    if (agent.IsMainAgent && FireSwordProperties.IgnitePlayerBody)
                    {
                        int boneCount = skeleton.GetBoneCount();
                        for (sbyte b = 0; b < boneCount; b = (sbyte)(b + 1))
                        {
                            MatrixFrame boneLocalFrame3 = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, 0f, -1f)).Elevate(0.2f);
                            ParticleSystem component3 = ParticleSystem.CreateParticleSystemAttachedToEntity(particleName, weaponEntityFromEquipmentSlot, ref boneLocalFrame3);
                            skeleton.AddComponentToBone(b, component3);
                        }
                    }

                    dropLock = true;
                    agent.DropItem(wieldedItemIndex);
                    SpawnedItemEntity firstScriptOfType = weaponEntityFromEquipmentSlot.GetFirstScriptOfType<SpawnedItemEntity>();
                    if (firstScriptOfType != null)
                    {
                        agent.OnItemPickup(firstScriptOfType, EquipmentIndex.None, out var _);
                    }

                    dropLock = false;
                    this.light = light;
                    entity = weaponEntityFromEquipmentSlot;
                    enabled = true;
                    return;
                }

                enabled = false;
                if ((object)entity == null || agent == null)
                {
                    return;
                }

                MBAgentVisuals agentVisuals2 = agent.AgentVisuals;
                if (agentVisuals2 != null)
                {
                    Skeleton skeleton2 = agentVisuals2.GetSkeleton();
                    if (this.light != null && skeleton2 != null)
                    {
                        skeleton2.RemoveComponent(this.light);
                    }
                }

                entity.RemoveAllParticleSystems();
            }
        }

        private static bool _playerFireSwordEnabled;


        private Dictionary<Agent, AgentFireSwordData> _agentFireSwordData = new();

        public FireSwordLogic()
        {

            _playerFireSwordEnabled = true;
        }


        public bool IsInBattle()
        {
            return base.Mission.IsFieldBattle || base.Mission.IsSiegeBattle || !base.Mission.IsFriendlyMission || base.Mission.Mode == MissionMode.Duel || base.Mission.Mode == MissionMode.Stealth || base.Mission.Mode == MissionMode.Tournament;
        }

        public override void OnAgentCreated(Agent agent)
        {
            if (IsInBattle() && agent.IsHuman && !_agentFireSwordData.ContainsKey(agent))
            {
                AgentFireSwordData agentFireSwordData = new AgentFireSwordData();
                agentFireSwordData.agent = agent;
                agent.OnAgentWieldedItemChange = (Action)Delegate.Combine(agent.OnAgentWieldedItemChange, new Action(agentFireSwordData.OnAgentWieldedItemChange));
                agent.OnAgentHealthChanged += agentFireSwordData.OnAgentHealthChanged;
                AgentFireSwordData agentFireSwordData2 = agentFireSwordData;
                int num = (agent.WieldedWeapon.IsEmpty ? 1 : 0);
                agentFireSwordData2.lastWieldedWeaponEmpty = num != 0;
                if (!agent.IsMainAgent || _playerFireSwordEnabled)
                {
                    agentFireSwordData.timer = new MissionTimer(1f);
                }

                _agentFireSwordData.Add(agent, agentFireSwordData);
            }
        }

        public override void OnAgentDeleted(Agent agent)
        {
            if (IsInBattle())
            {
                _agentFireSwordData.Remove(agent);
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (!IsInBattle())
            {
                return;
            }

            foreach (KeyValuePair<Agent, AgentFireSwordData> agentFireSwordDatum in _agentFireSwordData)
            {
                AgentFireSwordData value = agentFireSwordDatum.Value;
                if (agentFireSwordDatum.Key.IsMainAgent && !_playerFireSwordEnabled)
                {
                    value.timer = null;
                }
                else if (value.timer != null && value.timer.Check())
                {
                    value.SetFireSwordEnable(enable: true);
                    value.timer = null;
                }
            }

            if (!Input.IsKeyPressed(FireSwordProperties.FireSwordToggleKey) || Agent.Main == null)
            {
                return;
            }

            _playerFireSwordEnabled = !_playerFireSwordEnabled;
            foreach (KeyValuePair<Agent, AgentFireSwordData> agentFireSwordDatum2 in _agentFireSwordData)
            {
                if (agentFireSwordDatum2.Key.IsPlayerControlled)
                {
                    AgentFireSwordData value2 = agentFireSwordDatum2.Value;
                    value2.SetFireSwordEnable(_playerFireSwordEnabled);
                    value2.timer = null;
                    break;
                }
            }
        }

        public override void OnScoreHit(Agent victim, Agent attacker, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damageHp, float hitDistance, float shotDifficulty)
        {
            Agent attacker2 = attacker;
            if (IsInBattle())
            {
                _agentFireSwordData.TryGetValue(attacker2, out var value);
                value = _agentFireSwordData.FirstOrDefault<KeyValuePair<Agent, AgentFireSwordData>>((KeyValuePair<Agent, AgentFireSwordData> r) => r.Key.Character == attacker2.Character).Value;
                if (value != null && FireSwordProperties.IgniteTargetWithFireSword && value.enabled && victim != null && victim.IsHuman)
                {
                    float firebarAdd = (isBlocked ? (FireSwordProperties.IgnitionPerFireSwordHit / 2f) : FireSwordProperties.IgnitionPerFireSwordHit);
                }
            }
        }
    }
}