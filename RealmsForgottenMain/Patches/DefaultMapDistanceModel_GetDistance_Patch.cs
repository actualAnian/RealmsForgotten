using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(DefaultMapDistanceModel))]
    [HarmonyPatch("GetDistance")]
    [HarmonyPatch(new Type[] { typeof(Settlement), typeof(Settlement) })]
    public class DefaultMapDistanceModel_GetDistance_Patch
    {
        static bool Prefix(Settlement fromSettlement, Settlement toSettlement, ref float __result)
        {
            if (fromSettlement == null || toSettlement == null)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }
}