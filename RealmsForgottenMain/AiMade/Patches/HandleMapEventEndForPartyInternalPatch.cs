using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using System.Diagnostics;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(MapEventSide))]
    [HarmonyPatch("HandleMapEventEndForPartyInternal")]
    public static class HandleMapEventEndForPartyInternalPatch
    {
        // Using a Prefix to log the current state, and a Postfix to catch exceptions.
        // Note: Harmony supports __exception in Postfix for non-void methods.
        // If the original method is void and the exception is rethrown, you may need to use a try/catch transpiler.
        [HarmonyPrefix]
        public static void Prefix(PartyBase party)
        {
            Debug.WriteLine($"[HandleMapEventEndForPartyInternalPatch] Called for party: {(party != null ? party.Name : "null")}");
        }

        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                Debug.WriteLine($"[HandleMapEventEndForPartyInternalPatch] Exception caught: {__exception}");
                // Return null to suppress the exception (use with caution).
                return null;
            }
            return __exception;
        }
    }
}
