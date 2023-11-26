using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RealmsForgotten.CustomSkills;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass")]
    public static class GetCartridgeSkillPatch
    {
        public static void Postfix(WeaponClass weaponClass, ref SkillObject __result)
        {
            if (weaponClass == WeaponClass.Cartridge || weaponClass == WeaponClass.Musket)
                __result = RFSkills.Arcane;
        }
    }
}
