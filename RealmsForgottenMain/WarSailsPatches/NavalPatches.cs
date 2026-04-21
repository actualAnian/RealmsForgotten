using HarmonyLib;
using NavalDLC.GameComponents;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RealmsForgotten.WarSailsPatches
{
    [HarmonyPatch(typeof(NavalDLCMapDistanceModel), "RegisterDistanceCache")]
    //[HarmonyPatch("RegisterDistanceCache")]
    public class FillMissingCachesPatch
    {
        static void Postfix(NavalDLCMapDistanceModel __instance)
        {
            try
            {
                var path = ModuleHelper.GetModuleFullPath("RF_Map") + "\\ModuleData\\DistanceCaches";
                if (File.Exists(path + "\\settlements_distance_cache_Naval.bin"))
                    return;
                InformationManager.DisplayMessage(new InformationMessage("missing naval caches for RF_Map, filling with default data"));
                var cachesField = typeof(NavalDLCMapDistanceModel)
                    .GetField("_navigationCaches", BindingFlags.NonPublic | BindingFlags.Instance);

                if (cachesField == null) return;

                var caches = cachesField.GetValue(__instance)
                    as Dictionary<MobileParty.NavigationType, MapDistanceModel.INavigationCache>;

                if (caches == null) return;

                // Once default cache is registered, fill naval and all with the same data
                if (caches.ContainsKey(MobileParty.NavigationType.Default))
                {
                    var defaultCache = caches[MobileParty.NavigationType.Default];

                    if (!caches.ContainsKey(MobileParty.NavigationType.Naval))
                        caches[MobileParty.NavigationType.Naval] = defaultCache;

                    if (!caches.ContainsKey(MobileParty.NavigationType.All))
                        caches[MobileParty.NavigationType.All] = defaultCache;
                }
            }
            catch { }
        }
    }
}
