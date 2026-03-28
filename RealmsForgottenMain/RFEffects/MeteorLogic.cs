using RealmsForgotten.Utility.Magic;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
    public static class MeteorLogic
    {
        public static void FireMeteor(Agent caster, Vec3 targetPoint, ItemObject meteorProjectile)
        {
            float z = 50f;
            var position = targetPoint + new Vec3(0f, 0f, z, -1f);
            var direction = new Vec3(MBRandom.RandomFloat * 0.001f, MBRandom.RandomFloat * 0.001f, -1f, -1f);
            direction.Normalize();
            var missileWeapon = new MissionWeapon(meteorProjectile, null, null);
            missileWeapon.GetWeaponData(true);
            Mat3 identity = Mat3.Identity;
            float speed = meteorProjectile.PrimaryWeapon.MissileSpeed;
            Mission.Current.AddCustomMissile(caster, missileWeapon, position, direction, identity, speed, speed, true, null);
        }
    }
}
