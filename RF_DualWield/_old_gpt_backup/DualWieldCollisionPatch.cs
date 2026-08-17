using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_DualWield
{
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
