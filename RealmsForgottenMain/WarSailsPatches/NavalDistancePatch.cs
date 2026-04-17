using HarmonyLib;
using NavalDLC.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace RealmsForgotten.NavalPatches
{
    // =========================================================================
    // Patch 1: Fill missing Naval/All caches with Default cache
    // =========================================================================
    [HarmonyPatch(typeof(NavalDLCMapDistanceModel))]
    [HarmonyPatch("RegisterDistanceCache")]
    public class FillMissingCachesPatch
    {
        static void Postfix(NavalDLCMapDistanceModel __instance)
        {
            try
            {
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

    // =========================================================================
    // Patch 2: Safety net for GetDistance if caches aren't populated yet
    // =========================================================================
    [HarmonyPatch(typeof(NavalDLCMapDistanceModel))]
    [HarmonyPatch("GetDistance")]
    [HarmonyPatch(new Type[]
    {
        typeof(Settlement),
        typeof(Settlement),
        typeof(bool),
        typeof(bool),
        typeof(MobileParty.NavigationType),
        typeof(float)
    },
    new ArgumentType[]
    {
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Out
    })]
    public static class NavalDistancePatch
    {
        static bool Prefix(
            NavalDLCMapDistanceModel __instance,
            Settlement fromSettlement,
            Settlement toSettlement,
            bool isFromPort,
            bool isTargetingPort,
            ref MobileParty.NavigationType navigationCapability,
            out float landRatio,
            ref float __result)
        {
            landRatio = -1f;

            var cachesField = typeof(NavalDLCMapDistanceModel)
                .GetField("_navigationCaches", BindingFlags.NonPublic | BindingFlags.Instance);

            if (cachesField == null)
                return true;

            var caches = cachesField.GetValue(__instance)
                as Dictionary<MobileParty.NavigationType, MapDistanceModel.INavigationCache>;

            if (caches == null || !caches.ContainsKey(navigationCapability))
            {
                // Try falling back to default cache
                if (caches != null && caches.ContainsKey(MobileParty.NavigationType.Default))
                {
                    navigationCapability = MobileParty.NavigationType.Default;
                    return true;
                }

                // No caches at all
                __result = float.MaxValue;
                landRatio = 1f;
                return false;
            }

            return true;
        }
    }

    //// =========================================================================
    //// Patch 3: Fix navmesh pathfinding crash
    //// =========================================================================
    //[HarmonyPatch(typeof(SandBoxNavigationCache))]
    //[HarmonyPatch("GetRealDistanceAndLandRatioBetweenSettlements")]
    //public static class NavmeshPathfindingPatch
    //{
    //    static bool Prefix(
    //        ref float __result,
    //        out float landRatio)
    //    {
    //        landRatio = 1f;
    //        __result = float.MaxValue;

    //        return true;
    //    }

    //    static Exception Finalizer(Exception __exception, ref float __result, ref float landRatio)
    //    {
    //        if (__exception != null)
    //        {
    //            __result = float.MaxValue;
    //            landRatio = 1f;
    //            return null;
    //        }
    //        return null;
    //    }
    //}

    // =========================================================================
    // Patch 4: Null guard for Army.FindBestGatheringSettlementAndMoveTheLeader
    // When a naval navigation party triggers a siege, FindNearestFortification
    // can return null, then get_GatePosition() crashes on it. Swallow the null
    // ref so the army simply stays put rather than crashing.
    // =========================================================================
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.Army), "FindBestGatheringSettlementAndMoveTheLeader")]
    public class ArmyGatheringSettlementPatch
    {
        static Exception Finalizer(Exception __exception)
        {
            if (__exception is NullReferenceException)
                return null;
            return __exception;
        }
    }
}
