using HuntableHerds.Extensions;
using HuntableHerds.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HuntableHerds.AgentComponents {
    public class AggressiveHerdAgentComponent : HerdAgentComponent {
        private float _attackTimer = 0f;
        private float _aggroTimer = 0f;

        public AggressiveHerdAgentComponent(Agent agent) : base(agent) {
        }

        public override void HuntableAITick(float dt) {
            Agent mainAgent = Agent.Main;

            if (_attackTimer > 0f)
                _attackTimer -= dt;
            else if (_attackTimer < 0f)
                _attackTimer = 0f;

            if (Agent.CanSeeOtherAgent(mainAgent, HerdBuildData.CurrentHerdBuildData.SightRange)) {
                _aggroTimer = 15f;
            }
            else {
                if (_aggroTimer > 0f)
                    _aggroTimer -= dt;
            }

            if (_aggroTimer > 0f) {
                TickIsAggroed(dt, mainAgent);
            }
            else if (_aggroTimer < 0f) {
                _aggroTimer = 0f;
            }
        }

        private void TickIsAggroed(float dt, Agent mainAgent) {
            Agent.SetMaximumSpeedLimit(HerdBuildData.CurrentHerdBuildData.MaxSpeed, false);
            if (_attackTimer <= 1f)
                SetMoveToPosition(mainAgent.Position.ToWorldPosition(), false, Agent.AIScriptedFrameFlags.NeverSlowDown);

            Agent victim = mainAgent.HasMount ? mainAgent.MountAgent : mainAgent;
            if (_attackTimer == 0f && GetWithinAttackRangeOfAgent(victim))
                AttackAgent(victim);
        }

        private void AttackAgent(Agent otherAgent) {
            _attackTimer = 5f;
            Vec3 nextPosition = Agent.Mission.GetTrueRandomPositionAroundPoint(otherAgent.Position, 10f, 50f, true);
            SetMoveToPosition(nextPosition.ToWorldPosition(), false, Agent.AIScriptedFrameFlags.NeverSlowDown);

            if (!Agent.CanSeeOtherAgent(otherAgent, 0.3f))
                return;
            // lazy block checking
            bool isBlocked = !otherAgent.HasMount && Input.IsKeyDown(InputKey.RightMouseButton) && otherAgent.CanSeeOtherAgent(Agent, 1.25f);
            int blockedDamageToPlayer = (int)Math.Round(HerdBuildData.CurrentHerdBuildData.DamageToPlayer * (isBlocked ? 0.25f : 1f));

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

        private bool GetWithinAttackRangeOfAgent(Agent otherAgent) {
            if (otherAgent.Position.Distance(Agent.Position) < HerdBuildData.CurrentHerdBuildData.HitboxRange)
                return true;
            return false;
        }
    }
}
