using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.RFEffects.Alchemy.Bombs
{
    public class SmokeBomb : AbstractBomb
    {
        internal override float BaseDuration => 5f;
        internal override float BaseRadius => 6f;
        public SmokeBomb(Agent caster, Vec3 center) : base(center, caster) { }
        public override string ParticleId { get; set; } = "alchemical_mist2";
        public override void OnEntered(Agent agent) => agent.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire);
        public override void OnLeft(Agent agent) => agent.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.FireAtWill);

        public override void OnProjectileEntered(Missile missile)
        {
            var velocity = missile.GetVelocity();
            var random = new Random();
            var xDiff = random.NextFloat();
            var xVec = velocity.X + ((xDiff / 5) - 0.1f);
            var yDiff = random.NextFloat();
            var yVec = velocity.Y + ((yDiff / 5) - 0.1f);
            missile.SetVelocity(new(xVec, yVec, velocity.z));
        }
    }
}
