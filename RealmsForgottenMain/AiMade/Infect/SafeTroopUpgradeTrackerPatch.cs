using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Infect
{
    [HarmonyPatch(typeof(TroopUpgradeTracker), "CheckUpgradedCount")]
    public static class SafeTroopUpgradeTrackerPatch
    {
        static bool Prefix(ref int __result, PartyBase party, CharacterObject character)
        {
            if (party == null || character == null)
            {
                __result = 0;
                return false; // skip original
            }

            return true; // run original otherwise
        }
    }
}
