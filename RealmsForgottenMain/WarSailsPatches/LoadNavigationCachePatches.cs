using HarmonyLib;
using SandBox.View.Map;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ModuleManager;

namespace RealmsForgotten.WarSailsPatches
{
    // This patch is applied manually from SubModule.RunWarSailsPatches
    public class FillMissingCachesPatch
    {
        static readonly string path = ModuleHelper.GetModuleFullPath("RF_Map") + "\\ModuleData\\DistanceCaches";
        public static bool Prefix(SettlementPositionScript __instance, bool useNavalNavigation)
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