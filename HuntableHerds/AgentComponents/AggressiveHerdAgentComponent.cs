using RealmsForgotten.HuntableHerds.Extensions;
using RealmsForgotten.HuntableHerds.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.HuntableHerds.AgentComponents
{
    /// <summary>
    /// Aggressive prey. The loop is: chase -> telegraph (animation + voice + charge speed) -> blow
    /// -> short recoil -> chase again. It never runs 10-50m away after a hit, which is what made
    /// the old "bite and flee" behaviour read as random wandering with phantom damage.
    /// </summary>
    public class AggressiveHerdAgentComponent : HerdAgentComponent
    {
        private enum AggroPhase
        {
            Idle,
            Pursue,
            Telegraph,
            Recoil
        }

        /// <summary>
        /// Candidate wind-up clips, in preference order. NOTHING here is played blindly: each name is
        /// checked against the agent's own action set with
        /// <see cref="MBActionSet.CheckActionAnimationClipExists"/> before it can ever be used, so an
        /// animal whose skeleton has no such clip simply telegraphs with sound + charge speed.
        /// Verified in Modules/*/ModuleData/action_sets.xml for 1.4.8:
        ///   as_rat  (RFMonsters xslt) -> act_rat_attack_1..6
        ///   as_horse                  -> act_horse_rear, act_horse_kick, act_horse_dash
        ///   as_cow / as_sheep         -> act_horse_dash only
        ///   as_hog / as_chicken       -> none (sound-only telegraph)
        /// </summary>
        private static readonly string[] TelegraphActionNames =
        {
            "act_rat_attack_1",
            "act_rat_attack_2",
            "act_rat_attack_3",
            "act_rat_attack_4",
            "act_rat_attack_5",
            "act_rat_attack_6",
            "act_horse_rear",
            "act_horse_kick",
            "act_horse_dash"
        };

        private AggroPhase _phase = AggroPhase.Idle;
        private float _aggroTimer;
        private float _attackCooldown;
        private float _telegraphTimer;
        private float _recoilTimer;
        private float _repathTimer;

        private ActionIndexCache[]? _telegraphActions;
        private int _telegraphSoundId = int.MinValue;

        public AggressiveHerdAgentComponent(Agent agent, HerdBuildData data) : base(agent, data)
        {
        }

        public override void HuntableAITick(float dt)
        {
            Agent mainAgent = Agent.Main;
            if (mainAgent == null || !mainAgent.IsActive())
            {
                LeaveAggro();
                return;
            }

            if (_attackCooldown > 0f)
                _attackCooldown = MathF.Max(0f, _attackCooldown - dt);

            // SightRange is METERS, passed as the named "distance" argument. The old call passed it
            // as the angle, so the cone was always true and the distance stuck at the 30m default.
            bool canSee = Agent.CanSeeOtherAgent(mainAgent,
                                                 angleMax: Settings.Instance.SightConeHalfAngle,
                                                 distance: Data.SightRange);
            if (canSee)
                _aggroTimer = Settings.Instance.AggroDurationSeconds;
            else if (_aggroTimer > 0f)
                _aggroTimer = MathF.Max(0f, _aggroTimer - dt);

            if (_aggroTimer <= 0f)
            {
                LeaveAggro();
                return;
            }

            Agent victim = (mainAgent.HasMount && mainAgent.MountAgent != null) ? mainAgent.MountAgent : mainAgent;

            switch (_phase)
            {
                case AggroPhase.Telegraph:
                    TickTelegraph(dt, victim);
                    break;
                case AggroPhase.Recoil:
                    TickRecoil(dt, victim);
                    break;
                default:
                    TickPursue(dt, victim);
                    break;
            }
        }

        /// <summary>Being shot from cover provokes a charge even if the animal never saw the player.</summary>
        protected override void OnProvoked(Agent affectorAgent)
        {
            _aggroTimer = Settings.Instance.AggroDurationSeconds;
            if (_phase == AggroPhase.Idle)
                _phase = AggroPhase.Pursue;
            _repathTimer = 0f;
        }

        private void LeaveAggro()
        {
            if (_phase == AggroPhase.Idle)
                return;
            _phase = AggroPhase.Idle;
            _aggroTimer = 0f;
            _telegraphTimer = 0f;
            _recoilTimer = 0f;
            // Back to the herd's designed top speed (never a negative sentinel: the engine would
            // take it literally and freeze the animal).
            Agent.SetMaximumSpeedLimit(Data.MaxSpeed, false);
        }

        private void TickPursue(float dt, Agent victim)
        {
            _phase = AggroPhase.Pursue;
            Agent.SetMaximumSpeedLimit(Data.MaxSpeed, false);

            _repathTimer -= dt;
            if (_repathTimer <= 0f)
            {
                _repathTimer = Settings.Instance.PursueRepathInterval;
                SetMoveToPosition(victim.Position.ToWorldPosition(), false, Agent.AIScriptedFrameFlags.NeverSlowDown);
            }

            if (_attackCooldown <= 0f && IsWithinDistance(victim, Data.HitboxRange + Settings.Instance.TelegraphExtraRange))
                BeginTelegraph(victim);
        }

        private void BeginTelegraph(Agent victim)
        {
            _phase = AggroPhase.Telegraph;
            _telegraphTimer = Settings.Instance.TelegraphDurationSeconds;
            _repathTimer = 0f;

            Agent.SetMaximumSpeedLimit(Data.MaxSpeed * Settings.Instance.ChargeSpeedMultiplier, false);
            SetMoveToPosition(victim.Position.ToWorldPosition(), false, Agent.AIScriptedFrameFlags.NeverSlowDown);

            PlayTelegraphAnimation();
            PlayTelegraphSound();
        }

        private void TickTelegraph(float dt, Agent victim)
        {
            _telegraphTimer -= dt;

            _repathTimer -= dt;
            if (_repathTimer <= 0f)
            {
                _repathTimer = Settings.Instance.PursueRepathInterval;
                SetMoveToPosition(victim.Position.ToWorldPosition(), false, Agent.AIScriptedFrameFlags.NeverSlowDown);
            }

            if (_telegraphTimer > 0f)
                return;

            // A small grace band so a charge that just barely connected still lands.
            if (IsWithinDistance(victim, Data.HitboxRange + 0.75f))
            {
                AttackAgent(victim);
                BeginRecoil(victim);
            }
            else
            {
                // Missed: back to chasing, with only half the cooldown so it stays pressuring.
                _phase = AggroPhase.Pursue;
                _attackCooldown = Settings.Instance.AttackCooldownSeconds * 0.5f;
                Agent.SetMaximumSpeedLimit(Data.MaxSpeed, false);
            }
        }

        private void BeginRecoil(Agent victim)
        {
            _phase = AggroPhase.Recoil;
            _recoilTimer = Settings.Instance.RecoilDurationSeconds;
            _attackCooldown = Settings.Instance.AttackCooldownSeconds;
            Agent.SetMaximumSpeedLimit(Data.MaxSpeed, false);

            Vec3 away = Agent.Position - victim.Position;
            float length = away.Length;
            if (length < 0.01f)
            {
                away = new Vec3(MBRandom.RandomFloatRanged(-1f, 1f), MBRandom.RandomFloatRanged(-1f, 1f), 0f);
                length = MathF.Max(away.Length, 0.01f);
            }

            float distance = MBRandom.RandomFloatRanged(Settings.Instance.RecoilMinDistance, Settings.Instance.RecoilMaxDistance);
            Vec3 target = Agent.Position + away * (distance / length);
            SetMoveToPosition(target.ToWorldPosition(), false, Agent.AIScriptedFrameFlags.NeverSlowDown);
        }

        private void TickRecoil(float dt, Agent victim)
        {
            _recoilTimer -= dt;
            if (_recoilTimer <= 0f)
            {
                _phase = AggroPhase.Pursue;
                _repathTimer = 0f;
            }
        }

        private void AttackAgent(Agent otherAgent)
        {
            if (!Agent.CanSeeOtherAgent(otherAgent, angleMax: 1.2f, distance: Data.HitboxRange + 1.5f))
                return;

            // lazy block checking
            bool isBlocked = !otherAgent.HasMount && Input.IsKeyDown(InputKey.RightMouseButton) && otherAgent.CanSeeOtherAgent(Agent, angleMax: 1.25f, distance: Data.HitboxRange + 2f);
            int blockedDamageToPlayer = (int)Math.Round(Data.DamageToPlayer * (isBlocked ? 0.25f : 1f));

            Blow blow = new Blow(Agent.Index);
            blow.DamageType = DamageTypes.Cut;
            blow.BoneIndex = otherAgent.Monster.HeadLookDirectionBoneIndex;
            blow.GlobalPosition = otherAgent.Position;
            blow.GlobalPosition.z = blow.GlobalPosition.z + otherAgent.GetEyeGlobalHeight();
            blow.BaseMagnitude = blockedDamageToPlayer;
            blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
            blow.InflictedDamage = blockedDamageToPlayer;
            blow.SwingDirection = otherAgent.LookDirection;
            blow.Direction = blow.SwingDirection;
            blow.DamageCalculated = true;

            sbyte mainHandItemBoneIndex = Agent.Monster.MainHandItemBoneIndex;
            AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(isBlocked, false, false, true, false, false, false, false, false, false, false, false, isBlocked ? CombatCollisionResult.Blocked : CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, mainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, otherAgent.Velocity, Vec3.Up);

            otherAgent.RegisterBlow(blow, attackCollisionDataForDebugPurpose);
        }

        private bool IsWithinDistance(Agent otherAgent, float distance)
        {
            return otherAgent.Position.Distance(Agent.Position) < distance;
        }

        // ------------------------------------------------------------------------------------------
        // Telegraph: animation
        // ------------------------------------------------------------------------------------------

        private void PlayTelegraphAnimation()
        {
            try
            {
                _telegraphActions ??= ResolveTelegraphActions();
                if (_telegraphActions.Length == 0)
                    return;

                // Don't restart a wind-up that is already playing.
                if (Agent.GetCurrentAction(0).Index >= 0)
                {
                    string current = Agent.GetCurrentAction(0).GetName();
                    if (current != null && (current.Contains("attack") || current.Contains("rear") || current.Contains("kick") || current.Contains("dash")))
                        return;
                }

                ActionIndexCache chosen = _telegraphActions[MBRandom.RandomInt(0, _telegraphActions.Length)];
                Agent.SetActionChannel(0, chosen, true);
            }
            catch (Exception e)
            {
                _telegraphActions = Array.Empty<ActionIndexCache>();
                SubModule.PrintDebugMessage($"HuntableHerds: telegraph animation failed, disabling it for this animal ({e.Message})", 255, 120, 0);
            }
        }

        /// <summary>
        /// Canary rule: only actions whose clip actually exists in THIS agent's action set survive.
        /// Never apply an action from another skeleton (that is what crashed RBM with as_horse).
        /// </summary>
        private ActionIndexCache[] ResolveTelegraphActions()
        {
            List<ActionIndexCache> valid = new();
            try
            {
                MBActionSet actionSet = Agent.ActionSet;
                if (!actionSet.IsValid)
                    return Array.Empty<ActionIndexCache>();

                foreach (string name in TelegraphActionNames)
                {
                    ActionIndexCache cache = ActionIndexCache.Create(name);
                    if (cache.Index < 0)
                        continue; // the action type itself is not registered
                    if (!MBActionSet.CheckActionAnimationClipExists(actionSet, cache))
                        continue; // registered, but this skeleton has no clip for it
                    valid.Add(cache);
                }
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: could not resolve telegraph clips ({e.Message})", 255, 120, 0);
                return Array.Empty<ActionIndexCache>();
            }
            return valid.ToArray();
        }

        // ------------------------------------------------------------------------------------------
        // Telegraph: sound
        // ------------------------------------------------------------------------------------------

        private void PlayTelegraphSound()
        {
            try
            {
                if (_telegraphSoundId == int.MinValue)
                    _telegraphSoundId = ResolveTelegraphSoundId();
                if (_telegraphSoundId < 0)
                    return;
                Agent.Mission.MakeSound(_telegraphSoundId, Agent.Position, false, true, Agent.Index, -1);
            }
            catch (Exception e)
            {
                _telegraphSoundId = -1;
                SubModule.PrintDebugMessage($"HuntableHerds: telegraph sound failed, disabling it for this animal ({e.Message})", 255, 120, 0);
            }
        }

        private int ResolveTelegraphSoundId()
        {
            foreach (string candidate in GetTelegraphSoundCandidates())
            {
                int id = SoundEvent.GetEventIdFromString(candidate);
                if (id >= 0)
                    return id;
            }
            return -1;
        }

        /// <summary>
        /// All of these were verified to exist in Native/ModuleData/sound_event_data.gen.xml for
        /// 1.4.8, and every one is still guarded by GetEventIdFromString >= 0 at runtime.
        /// </summary>
        private string[] GetTelegraphSoundCandidates()
        {
            string monsterId = Agent.Monster?.StringId ?? string.Empty;
            switch (monsterId)
            {
                case "hog":
                    return new[]
                    {
                        "event:/mission/movement/foley/animals/boar/pain",
                        "event:/mission/movement/foley/animals/boar/idle"
                    };
                case "cow":
                    return new[] { "event:/mission/movement/foley/animals/cow/pain" };
                case "sheep":
                    return new[] { "event:/mission/movement/foley/animals/sheep/pain" };
                case "chicken":
                    return new[] { "event:/mission/movement/foley/animals/chicken/idle" };
                case "rat":
                    return new[]
                    {
                        "event:/mission/movement/foley/animals/dog/pain",
                        "event:/mission/movement/foley/animals/dog/idle"
                    };
                default:
                    return new[]
                    {
                        "event:/mission/movement/foley/animals/horse/rear_foley",
                        "event:/mission/ambient/battle/horses_vocal"
                    };
            }
        }
    }
}
