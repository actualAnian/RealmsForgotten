using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace RealmsForgotten.RFEffects.MissionEffects
{
    public class BurnEffect : IMissionEffect
    {
        private readonly float _damagePerSecond;
        private float _tickAccumulator = 0f;

        public BurnEffect(float damagePerSecond = 5f)
        {
            _damagePerSecond = damagePerSecond;
        }

        public void OnApply(object target)
        {
            // Could add VFX or sound here when starting the burn.
        }

        public void OnUpdate(object target, float dt)
        {
            if (target is not Agent agent || !agent.IsActive()) return;

            _tickAccumulator += dt;
            // Apply damage each full second
            while (_tickAccumulator >= 1f)
            {
                ApplyDamage(agent, _damagePerSecond);
                _tickAccumulator -= 1f;
            }
        }

        public void OnEnd(object target)
        {
            // Cleanup VFX or state here
        }

        private void ApplyDamage(Agent victim, float amount)
        {
            int dmg = (int)Math.Round(amount);
            if (dmg <= 0) return;

            Blow blow = new Blow(victim.Index);
            blow.DamageType = DamageTypes.Blunt;
            blow.BlowFlag = BlowFlags.NoSound;
            blow.BoneIndex = victim.Monster.HeadLookDirectionBoneIndex;
            blow.GlobalPosition = victim.Position;
            blow.GlobalPosition.z += victim.GetEyeGlobalHeight();
            blow.BaseMagnitude = 0f;
            blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
            blow.InflictedDamage = dmg;
            blow.SwingDirection = victim.LookDirection;
            blow.Direction = blow.SwingDirection;
            blow.DamageCalculated = true;

            AttackCollisionData attackCollisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                false, false, false, false, false, false, false, false, false, false, false, false,
                CombatCollisionResult.StrikeAgent,
                -1, 0, victim.Index,
                blow.BoneIndex, BoneBodyPartType.Head,
                victim.Monster.MainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1,
                CombatHitResultFlags.NormalHit,
                0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                victim.LookDirection, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, victim.Velocity,
                Vec3.Zero);

            victim.RegisterBlow(blow, attackCollisionData);
        }
    }
}
