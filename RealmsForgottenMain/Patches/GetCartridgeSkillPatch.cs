using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass")]
    public static class GetCartridgeSkillPatch
    {
        public static void Postfix(WeaponClass weaponClass, ref SkillObject __result)
        {
            if (Campaign.Current == null || (weaponClass != WeaponClass.Cartridge && weaponClass != WeaponClass.Musket))
                return;

            // [RF-LEGACY] com o motor legado LIGADO, cajado-musket conta como magia.
            //
            // Com ele DESLIGADO nao basta "nao sobrescrever": o vanilla devolve NULL
            // para estas classes de arma, e o consumidor adiante desreferencia
            // (CalculateLearningRate) — foi exatamente o NRE que essa gate causou na
            // primeira tentativa. Entao continuamos devolvendo uma skill VALIDA,
            // apenas nao a de magia: Crossbow e o analogo vanilla mais proximo de uma
            // arma de fogo, e e o que fara sentido quando Musket/Cartridge forem
            // liberados para armas de fogo de verdade.
            __result = RFLegacyMagic.Enabled ? RFSkills.Arcane : DefaultSkills.Crossbow;
        }
    }
}
