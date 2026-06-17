using HarmonyLib;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(VillageEncounter), "CreateAndOpenMissionController")]
    internal static class ADODInnPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref IMission __result, Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
        {
            if (nextLocation.StringId == "village_inn")
            {
                __result = CampaignMission.OpenIndoorMission(nextLocation.GetSceneName(0), 0, nextLocation, talkToChar);
                return false;
            }
            return true;
        }
    }
}