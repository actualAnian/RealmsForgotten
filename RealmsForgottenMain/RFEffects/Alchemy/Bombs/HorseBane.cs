using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects.Alchemy.Bombs
{
    internal class HorseBane : AbstractBomb
    {
        internal override float BaseDuration => 10f;
        internal override float BaseRadius => 10f;

        public HorseBane(Vec3 center, Agent caster) : base(center, caster) { }

        private void CreateBlowToDismountRider(Agent rider)
        {
            if (!rider.HasMount) return;
            var agentBlow = new Blow(rider.Index);
            agentBlow.DamageType = DamageTypes.Blunt;
            agentBlow.BlowFlag = BlowFlags.NoSound;
            agentBlow.BlowFlag |= BlowFlags.CanDismount;

            agentBlow.BoneIndex = rider.Monster.HeadLookDirectionBoneIndex;
            agentBlow.GlobalPosition = rider.Position;
            agentBlow.GlobalPosition.z += rider.GetEyeGlobalHeight();
            agentBlow.BaseMagnitude = 5f;
            agentBlow.DefenderStunPeriod = 20;
            agentBlow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
            agentBlow.InflictedDamage = 1;
            agentBlow.SwingDirection = rider.LookDirection;
            agentBlow.Direction = agentBlow.SwingDirection;
            agentBlow.DamageCalculated = true;

            var attackCollisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                false, false, false, false, false, false, false, false, false, false, false, false,
                CombatCollisionResult.StrikeAgent,
                -1, 0, rider.Index,
                agentBlow.BoneIndex, BoneBodyPartType.Head,
                rider.Monster.MainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1,
                CombatHitResultFlags.NormalHit,
                0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                rider.LookDirection, agentBlow.Direction, agentBlow.GlobalPosition, Vec3.Zero, Vec3.Zero, (Vec3)rider.Velocity,
                Vec3.Zero);
            rider.RegisterBlow(agentBlow, attackCollisionData);
        }
        private void MakeHorseDoRear(Agent mount)
        {
            if (mount.RiderAgent == null) return;
            int dmg = 1;

            var mountBlow = new Blow(mount.Index);
            mountBlow.DamageType = DamageTypes.Blunt;
            mountBlow.BlowFlag |= BlowFlags.MakesRear;
            mountBlow.BlowFlag = BlowFlags.NoSound;

            mountBlow.BoneIndex = mount.Monster.HeadLookDirectionBoneIndex;
            mountBlow.GlobalPosition = mount.Position;
            mountBlow.GlobalPosition.z += mount.GetEyeGlobalHeight();
            mountBlow.BaseMagnitude = 5f;
            mountBlow.DefenderStunPeriod = 20;
            mountBlow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
            mountBlow.InflictedDamage = dmg;
            mountBlow.SwingDirection = mount.LookDirection;
            mountBlow.Direction = mountBlow.SwingDirection;
            mountBlow.DamageCalculated = true;

            var attackCollisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                false, false, false, false, false, false, false, false, false, false, false, false,
                CombatCollisionResult.StrikeAgent,
                -1, 0, mount.Index,
                mountBlow.BoneIndex, BoneBodyPartType.Head,
                mount.Monster.MainHandItemBoneIndex, Agent.UsageDirection.AttackLeft, -1,
                CombatHitResultFlags.NormalHit,
                0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                mount.LookDirection, mountBlow.Direction, mountBlow.GlobalPosition, Vec3.Zero, Vec3.Zero, (Vec3)mount.Velocity,
                Vec3.Zero);
            mount.MountAgent.RegisterBlow(mountBlow, attackCollisionData);
            var nextMountPos = new WorldPosition(Mission.Current.Scene, CalculateNextMovePosition(mount.MountAgent, mount.MountAgent.LookDirection, 20, 30, 240).ToVec3());
            mount.MountAgent.SetScriptedPosition(ref nextMountPos, false, Agent.AIScriptedFrameFlags.None);
        }

        public override void OnEntered(Agent agent)
        {
            if (agent.RiderAgent == null) return;
            MakeHorseDoRear(agent);
        }
        public override void OnInteraction(IMissionPlane anotherPlane)
        {
            if (anotherPlane is not PanicDust) return;
            foreach(var agent in InsideAgents)
            {
                if (!agent.HasMount) return;
                CreateBlowToDismountRider(agent);
            }
        }
        public static Vec2 CalculateNextMovePosition(Agent agent, Vec3 lookDirection, int minDistance, int maxDistance, float maxConeDegrees)
        {
            Vec2 origin = agent.Position.AsVec2;
            float angleDegrees = MBRandom.RandomFloatRanged(-maxConeDegrees * 0.5f, maxConeDegrees * 0.5f);
            float angleRadians = MathF.PI / 180f * angleDegrees;
            var distance = MBRandom.RandomFloatRanged(minDistance, maxDistance);
            var forward = lookDirection.AsVec2.Normalized();
            var cos = MathF.Cos(angleRadians);
            var sin = MathF.Sin(angleRadians);

            var rotated = new Vec2(forward.x * cos - forward.y * sin, forward.x * sin + forward.y * cos);
            return origin + rotated * distance;
        }
    }
}