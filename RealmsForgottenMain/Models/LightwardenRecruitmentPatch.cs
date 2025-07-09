using HarmonyLib;
using RealmsForgotten.Career;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.Models
{
    [HarmonyPatch(typeof(RecruitVolunteerTroopVM), nameof(RecruitVolunteerTroopVM.Cost), MethodType.Getter)]
    internal class LightwardenRecruitmentPatch
    {
        static void Postfix(RecruitVolunteerTroopVM __instance, ref int __result)
        {
            CharacterObject ch = __instance.Character;
            if (IsInfantry(ch))
                __result = (int)MathF.Floor(__result * 0.95f); // 5 % de desconto
        }
    }
}

