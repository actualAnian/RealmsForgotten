using RealmsForgotten.Utility.Magic;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public static class WingedWitchSpellsLogic
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
            //identity.f = vec;
            //identity.u = new Vec3(0f, 1f, 0f, -1f);
            //identity.s = Vec3.CrossProduct(identity.u, identity.f);
            float speed = meteorProjectile.PrimaryWeapon.MissileSpeed;
            Mission.Current.AddCustomMissile(caster, missileWeapon, targetPoint, direction, identity, speed, speed, true, null);
        }
        public static void OnTeleportingMissleHit(Agent victim, IEnumerable<Vec3> teleportLocations)
        {
            Teleport.TeleportToPosition(victim, teleportLocations.GetRandomElementInefficiently());
        }
    }
}
