#region assembly FireLord2, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// C:\Users\Pedrinho\Desktop\FireLord2.dll
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime;
using RealmsForgotten.RFEffects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.RFEffects
{

    internal class WeaponParticlesBehavior : MissionLogic
    {
        private static bool dropLock;
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            Blow b = blow;
            MissionWeapon attackerWeapon = affectorWeapon;
            AttackCollisionData collisionData = attackCollisionData;
            ExceptionHandler.HandleMethod(() =>
            {
                if (b.InflictedDamage < 1 || attackerWeapon.Item == null) return;

                if (RFEffectsLibrary.CurrentWeaponEffects.TryGetValue(attackerWeapon.Item.StringId, out WeaponEffectData effect) && effect.Effect != null)
                {
                    TOWParticleSystem.ApplyParticleToAgent(affectedAgent, effect.VictimParticle, out GameEntity childEntity);


                    if (WeaponEffectConsequences.Methods.TryGetValue(effect.Effect, out VictimAgentConsequence method))
                        method?.Invoke(affectedAgent, affectorAgent, attackerWeapon, b, collisionData, childEntity);


                }
            });
        }
        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            if (!hasRigidBody)
            {
                Mission.Missile missile = Mission.Missiles.ElementAt(0);

                Skeleton skeleton = missile.Entity.Skeleton;
                Scene scene = Mission.Current.Scene;
                GameEntity childEntity = GameEntity.CreateEmpty(scene);
                MatrixFrame localFrame = new (Mat3.Identity, new (0, 0, 0));
                float elevationOffset = 0f;
                MissionWeapon missionWeapon = missile.Weapon;

                if (missionWeapon.Item == null)
                    return;

                if (!RFEffectsLibrary.CurrentWeaponEffects.TryGetValue(missionWeapon.Item.StringId, out WeaponEffectData effect))
                    return;

                localFrame.Elevate(elevationOffset);
                ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(effect.ItemParticle, childEntity, ref localFrame);
                if (particle != null)
                {
                    missile.Entity.AddChild(childEntity);
                    skeleton.AddComponentToBone((sbyte)0, particle);
                }
            }
        }
        public class AgentWeaponEffectData
        {
            public bool enabled;
            public Agent agent;
            public GameEntity entity;
            public MissionTimer timer;


            public void OnAgentWieldedItemChange()
            {
                if (dropLock || agent == null)
                {
                    return;
                }

                MissionWeapon wieldedWeapon = agent.WieldedWeapon;
                if (wieldedWeapon.Item == null)
                {
                    if(enabled)
                        SetFireSwordEnable(false);
                    return;
                }

                if (!RFEffectsLibrary.CurrentWeaponEffects.ContainsKey(wieldedWeapon.Item.StringId))
                {
                    if(enabled)
                        SetFireSwordEnable(false);
                    return;
                    
                }

                timer = new MissionTimer(0.1f);
                
            }

            public void OnAgentHealthChanged(Agent agent, float oldHealth, float newHealth)
            {
                if (newHealth <= 0)
                {
                    SetFireSwordEnable(false);
                }
            }

            public void SetFireSwordEnable(bool enable)
            {
                ExceptionHandler.HandleMethod(() =>
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


                        MissionWeapon wieldedWeapon = agent.WieldedWeapon;
                        if (wieldedWeapon.IsEmpty)
                        {
                            return;
                        }

                        if (!RFEffectsLibrary.CurrentWeaponEffects.TryGetValue(wieldedWeapon.Item?.StringId, out WeaponEffectData Effects))
                        {
                            return;
                        }


                        int WeaponLength = (int)Math.Round((double)wieldedWeapon.GetWeaponStatsData()[0].WeaponLength / 10.0);

                        MBAgentVisuals agentVisuals = agent.AgentVisuals;
                        if (agentVisuals is null)
                        {
                            return;
                        }


                        GameEntity weaponEntityFromEquipmentSlot = null;
                        Skeleton skeleton = agentVisuals.GetSkeleton();

                        switch (wieldedWeapon.CurrentUsageItem.WeaponClass)
                        {
                            default:
                                return;
                            case WeaponClass.Javelin:
                                for (int i = -(WeaponLength / 2); i < WeaponLength / 2; i++)
                                {
                                    TOWParticleSystem.ApplyParticleToWeapon(agent, Effects.ItemParticle, wieldedItemIndex, i * 0.1f, skeleton, out weaponEntityFromEquipmentSlot);
                                }
                                break;
                            case WeaponClass.OneHandedSword:
                            case WeaponClass.TwoHandedSword:
                            case WeaponClass.Mace:
                            case WeaponClass.TwoHandedMace:
                            case WeaponClass.ThrowingKnife:
                            case WeaponClass.Boulder:
                            case WeaponClass.Stone:
                            case WeaponClass.Bow:
                            case WeaponClass.Dagger:
                                for (int i = 1; i < WeaponLength; i++)
                                {
                                    TOWParticleSystem.ApplyParticleToWeapon(agent, Effects.ItemParticle, wieldedItemIndex, i * 0.1f, skeleton, out weaponEntityFromEquipmentSlot);
                                }
                                break;

                            case WeaponClass.Arrow:
                            case WeaponClass.Bolt:
                            case WeaponClass.OneHandedAxe:
                            case WeaponClass.TwoHandedAxe:
                            case WeaponClass.ThrowingAxe:
                            case WeaponClass.OneHandedPolearm:
                            case WeaponClass.TwoHandedPolearm:
                            case WeaponClass.LowGripPolearm:
                            case WeaponClass.Pick:

                                {
                                    int num = WeaponLength - 1;
                                    while (num > 0 && num > WeaponLength - 4)
                                    {
                                        TOWParticleSystem.ApplyParticleToWeapon(agent, Effects.ItemParticle, wieldedItemIndex, num * 0.1f, skeleton, out weaponEntityFromEquipmentSlot);
                                        num--;
                                    }

                                    break;
                                }
                        }

                        if (weaponEntityFromEquipmentSlot == null)
                            return;
                        dropLock = true;
                        agent.DropItem(wieldedItemIndex);
                        SpawnedItemEntity firstScriptOfType = weaponEntityFromEquipmentSlot.GetFirstScriptOfType<SpawnedItemEntity>();
                        if (firstScriptOfType != null)
                        {
                            agent.OnItemPickup(firstScriptOfType, EquipmentIndex.None, out var _);
                        }

                        dropLock = false;

                        entity = weaponEntityFromEquipmentSlot;
                        enabled = true;
                        return;
                    }

                    enabled = false;
                    if (entity is null || agent is null)
                    {
                        return;
                    }


                    entity.RemoveAllParticleSystems();
                });
            }
        }


        private Dictionary<Agent, AgentWeaponEffectData> _agentFireSwordData = new();
        public WeaponParticlesBehavior()
        {
            RFEffectsLibrary.Initialize();


        }
        public override void AfterStart()
        {
            Mission.OnItemDrop += OnItemDrop;
            dropLock = false;
        }



        private void OnItemDrop(Agent agent, SpawnedItemEntity entity)
        {
            if (IsInBattle() && _agentFireSwordData.ContainsKey(agent) && !dropLock)
            {
                _agentFireSwordData[agent].SetFireSwordEnable(false);
            }
        }

        public bool IsInBattle()
        {
            return Mission.IsFieldBattle || Mission.IsSiegeBattle || !Mission.IsFriendlyMission || Mission.Mode == MissionMode.Duel || Mission.Mode == MissionMode.Stealth || Mission.Mode == MissionMode.Tournament;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (IsInBattle() && agent.IsHuman && !_agentFireSwordData.ContainsKey(agent))
            {
                AgentWeaponEffectData agentWeaponEffectData = new()
                {
                    agent = agent
                };

                agent.OnAgentWieldedItemChange = (Action)Delegate.Combine(agent.OnAgentWieldedItemChange, new Action(agentWeaponEffectData.OnAgentWieldedItemChange));

                agent.OnAgentHealthChanged += agentWeaponEffectData.OnAgentHealthChanged;

                agentWeaponEffectData.timer = new MissionTimer(1f);

                _agentFireSwordData.Add(agent, agentWeaponEffectData);
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

            foreach (KeyValuePair<Agent, AgentWeaponEffectData> agentFireSwordDatum in _agentFireSwordData)
            {
                AgentWeaponEffectData value = agentFireSwordDatum.Value;
                if (value.timer?.Check() == true)
                {
                    value.SetFireSwordEnable(enable: true);
                    value.timer = null;
                }
            }
        }
    }
}