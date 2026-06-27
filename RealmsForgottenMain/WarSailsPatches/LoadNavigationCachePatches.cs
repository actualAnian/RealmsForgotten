using HarmonyLib;
using NavalDLC.GameComponents;
using SandBox.View.Map;
using System;
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
        static readonly string defaultCachePath = path + "\\settlements_distance_cache_Default.bin";
        static readonly string allCachePath = path + "\\settlements_distance_cache_All.bin";
        static readonly string navalCachePath = path + "\\settlements_distance_cache_Naval.bin";

        static bool IsCacheFreshEnough(string candidatePath)
        {
            if (!File.Exists(defaultCachePath) || !File.Exists(candidatePath))
                return false;

            DateTime defaultWrite = File.GetLastWriteTimeUtc(defaultCachePath);
            DateTime candidateWrite = File.GetLastWriteTimeUtc(candidatePath);

            // If settlements were edited and only the default cache was regenerated,
            // using stale All/Naval caches can crash native path queries during load.
            return candidateWrite >= defaultWrite;
        }

        static bool Prefix(SettlementPositionScript __instance, bool useNavalNavigation)
        {
            try
            {
                if (__instance == null || Campaign.Current?.Models?.MapDistanceModel == null)
                {
                    return true;
                }

                MethodInfo met = AccessTools.Method("SandBox.View.Map.SettlementPositionScript:ReadNavigationCacheForNavigationTypeOnGameLoad");
                if (met == null)
                {
                    return true;
                }

                SandBoxNavigationCache cacheToRegister = met.Invoke(__instance, new object[] { MobileParty.NavigationType.Default }) as SandBoxNavigationCache;
                if (cacheToRegister == null)
                {
                    return true;
                }

                Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Default, cacheToRegister);

                if (useNavalNavigation)
                {
                    if (IsCacheFreshEnough(allCachePath))
                    {
                        try
                        {
                            SandBoxNavigationCache allCache = met.Invoke(__instance, new object[] { MobileParty.NavigationType.All }) as SandBoxNavigationCache;
                            Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.All, allCache ?? cacheToRegister);
                        }
                        catch
                        {
                            Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.All, cacheToRegister);
                        }
                    }
                    else
                    {
                        Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.All, cacheToRegister);
                    }

                    if (IsCacheFreshEnough(navalCachePath))
                    {
                        try
                        {
                            SandBoxNavigationCache navalCache = met.Invoke(__instance, new object[] { MobileParty.NavigationType.Naval }) as SandBoxNavigationCache;
                            Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Naval, navalCache ?? cacheToRegister);
                        }
                        catch
                        {
                            Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Naval, cacheToRegister);
                        }
                    }
                    else
                    {
                        Campaign.Current.Models.MapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Naval, cacheToRegister);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.Print($"[RF LoadNavigationCache] Prefix fallback to vanilla because patch failed: {ex.Message}", 0, Debug.DebugColor.Red);
                return true;
            }
        }
    }
}
