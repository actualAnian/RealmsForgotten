using HarmonyLib;
using NavalDLC.GameComponents;
using SandBox.View.Map;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RealmsForgotten.WarSailsPatches
{
    [HarmonyPatch(typeof(SettlementPositionScript), "RegisterNavigationCachesOnGameLoad")]
    public class FillMissingCachesPatch
    {
        static readonly string path = ModuleHelper.GetModuleFullPath("RF_Map") + "\\ModuleData\\DistanceCaches";
        static bool Prefix(SettlementPositionScript __instance, bool useNavalNavigation)
        {
            var met = AccessTools.Method("SandBox.View.Map.SettlementPositionScript:ReadNavigationCacheForNavigationTypeOnGameLoad");
            var cacheToRegister = (SandBoxNavigationCache)met.Invoke(__instance, new object[] { MobileParty.NavigationType.Default });
            Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Default, cacheToRegister);

            if (useNavalNavigation)
            {
                if (File.Exists(path + "\\settlements_distance_cache_All.bin"))
                {
                    var allCache = (SandBoxNavigationCache)met.Invoke(__instance, new object[] { MobileParty.NavigationType.All });
                    Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.All, allCache);
                }
                else
                    Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.All, cacheToRegister);

                if (File.Exists(path + "\\settlements_distance_cache_Naval.bin"))
                {
                    var navalCache = (SandBoxNavigationCache)met.Invoke(__instance, new object[] { MobileParty.NavigationType.Naval });
                    Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Naval, navalCache);
                }
                else
                    Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Naval, cacheToRegister);
            }
            return false;
        }
    }
}