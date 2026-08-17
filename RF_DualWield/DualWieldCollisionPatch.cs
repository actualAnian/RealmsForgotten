using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_DualWield
{
    /// <summary>
    /// O UNICO patch Harmony do modulo, e ele e indispensavel.
    ///
    /// Mecanica: o golpe da arma da mao esquerda colide no osso ItemL (20), mas a arma empunhada
    /// esta anexada ao osso ItemR (27). O engine trata "osso de colisao != osso de anexo da arma"
    /// como colisao invalida e descarta o golpe -- o ataque da offhand nunca causaria dano.
    /// Este postfix diz "nao e diferente" exatamente para o par (ItemL, ItemR).
    ///
    /// Escopo deliberadamente estreito:
    ///   - so mexe no resultado quando o par de ossos e (ItemL -> ItemR); nenhuma combinacao
    ///     vanilla produz isso (escudo/bash tem osso de anexo ItemL, nao ItemR);
    ///   - so age quando DualWieldMissionBehavior validou o sistema na missao atual;
    ///   - nao toca em dano, moral, knockdown, nem em Mission.RegisterBlow.
    ///
    /// Assinatura conferida por reflexao nos assemblies reais da 1.4.8:
    ///   public static bool MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone(
    ///       in AttackCollisionData collisionData, int weaponAttachBoneIndex)
    /// (identica a 1.1/1.2 -- e um dos poucos alvos que nao mudou.)
    /// </summary>
    [HarmonyPatch(typeof(MissionCombatMechanicsHelper),
        nameof(MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone))]
    internal static class DualWieldCollisionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result, in AttackCollisionData collisionData, int weaponAttachBoneIndex)
        {
            if (!__result)
            {
                return;
            }

            if (!DualWieldMissionBehavior.IsSystemHealthy)
            {
                return;
            }

            if (collisionData.AttackBoneIndex == (sbyte)HumanBone.ItemL &&
                weaponAttachBoneIndex == (int)HumanBone.ItemR)
            {
                __result = false;
            }
        }
    }
}
