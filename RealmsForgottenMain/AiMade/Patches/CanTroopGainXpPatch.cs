using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using System.Diagnostics;

/*
namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(MobilePartyHelper))]
    [HarmonyPatch(nameof(MobilePartyHelper.CanTroopGainXp))]
    public static class CanTroopGainXpPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PartyBase owner, CharacterObject character, ref int gainableMaxXp, ref bool __result)
        {
            Debug.WriteLine($"[CanTroopGainXpPatch] Called with owner: {(owner != null ? owner.Name : "null")}, troop: {(character != null ? character.StringId : "null")}");

            if (owner?.MemberRoster == null)
            {
                Debug.WriteLine("[CanTroopGainXpPatch] MemberRoster is null.");
                gainableMaxXp = 0;
                __result = false;
                return false;
            }

            int rosterCount = owner.MemberRoster.Count;
            int index = owner.MemberRoster.FindIndexOfTroop(character);
            Debug.WriteLine($"[CanTroopGainXpPatch] Roster count: {rosterCount}, Found index: {index}");

            if (index < 0)
            {
                Debug.WriteLine("[CanTroopGainXpPatch] Troop not found in roster.");
                gainableMaxXp = 0;
                __result = false;
                return false;
            }

            if (character?.UpgradeTargets == null)
            {
                Debug.WriteLine("[CanTroopGainXpPatch] UpgradeTargets is null.");
                gainableMaxXp = 0;
                __result = false;
                return false;
            }

            return true;
        }
    }
}
*/


