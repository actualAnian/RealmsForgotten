using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.DualWield
{
    /// <summary>
    /// Lets a dual-wield animation use the left-hand weapon bone without the
    /// engine downgrading the strike to an unarmed/blunt collision.
    /// </summary>
    [HarmonyPatch(typeof(MissionCombatMechanicsHelper), nameof(MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone))]
    internal static class DualWieldCollisionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result, in AttackCollisionData collisionData, int weaponAttachBoneIndex)
        {
            if (collisionData.AttackBoneIndex == (int)HumanBone.ItemL &&
                weaponAttachBoneIndex == (int)HumanBone.ItemR)
            {
                __result = false;
            }
        }
    }
}
