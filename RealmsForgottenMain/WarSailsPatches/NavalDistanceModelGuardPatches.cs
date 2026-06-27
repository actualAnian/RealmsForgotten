using HarmonyLib;
using NavalDLC.GameComponents;
using RealmsForgotten.AiMade;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.WarSailsPatches
{
    public class NavalDLCMapDistanceModel_GetDistance_GuardPatch
    {
        private static readonly HashSet<string> LoggedInvalidPairs = new HashSet<string>();

        private static MethodBase ResolveTargetMethod()
        {
            return AccessTools.Method(
                typeof(NavalDLCMapDistanceModel),
                nameof(NavalDLCMapDistanceModel.GetDistance),
                new Type[]
                {
                    typeof(Settlement),
                    typeof(Settlement),
                    typeof(bool),
                    typeof(bool),
                    typeof(MobileParty.NavigationType),
                    typeof(float).MakeByRefType()
                });
        }

        public static bool TryApply(Harmony harmony)
        {
            MethodBase target = ResolveTargetMethod();
            if (target == null)
            {
                RFLogger.Log("[Lifecycle] Optional naval patch skipped | NavalDLCMapDistanceModel.GetDistance target not found.");
                return false;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(NavalDLCMapDistanceModel_GetDistance_GuardPatch), nameof(Prefix)));
            return true;
        }

        public static bool Prefix(
            Settlement fromSettlement,
            Settlement toSettlement,
            bool isFromPort,
            bool isTargetingPort,
            MobileParty.NavigationType navigationCapability,
            ref float __result,
            ref float landRatio)
        {
            if (fromSettlement == null || toSettlement == null)
                return true;

            CampaignVec2 fromPosition = isFromPort ? fromSettlement.PortPosition : fromSettlement.GatePosition;
            CampaignVec2 toPosition = isTargetingPort ? toSettlement.PortPosition : toSettlement.GatePosition;

            if (HasInvalidFace(fromPosition) || HasInvalidFace(toPosition))
            {
                landRatio = GetFallbackLandRatio(navigationCapability);
                __result = fromPosition.ToVec2().Distance(toPosition.ToVec2());
                LogInvalidPairOnce(fromSettlement, toSettlement, isFromPort, isTargetingPort);
                return false;
            }

            return true;
        }

        private static bool HasInvalidFace(CampaignVec2 position)
        {
            return !position.IsValid() || position.Face.FaceIndex < 0;
        }

        private static float GetFallbackLandRatio(MobileParty.NavigationType navigationCapability)
        {
            return navigationCapability switch
            {
                MobileParty.NavigationType.All => 0.5f,
                MobileParty.NavigationType.Default => 1f,
                MobileParty.NavigationType.Naval => 0f,
                _ => -1f
            };
        }

        private static void LogInvalidPairOnce(Settlement fromSettlement, Settlement toSettlement, bool isFromPort, bool isTargetingPort)
        {
            string key = $"{fromSettlement.StringId}:{isFromPort}->{toSettlement.StringId}:{isTargetingPort}";
            if (!LoggedInvalidPairs.Add(key))
                return;

            Debug.Print(
                $"[RF NavalDistanceGuard] Invalid nav face blocked for {fromSettlement.StringId} {(isFromPort ? "port" : "gate")} -> {toSettlement.StringId} {(isTargetingPort ? "port" : "gate")}",
                0,
                Debug.DebugColor.Red);
        }
    }

    public class NavalDLCMapDistanceModel_GetPortToGateDistanceForSettlement_GuardPatch
    {
        public static bool TryApply(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(
                typeof(NavalDLCMapDistanceModel),
                nameof(NavalDLCMapDistanceModel.GetPortToGateDistanceForSettlement),
                new[] { typeof(Settlement) });

            if (target == null)
            {
                RFLogger.Log("[Lifecycle] Optional naval patch skipped | NavalDLCMapDistanceModel.GetPortToGateDistanceForSettlement target not found.");
                return false;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(NavalDLCMapDistanceModel_GetPortToGateDistanceForSettlement_GuardPatch), nameof(Prefix)));
            return true;
        }

        public static bool Prefix(Settlement settlement, ref float __result)
        {
            if (settlement == null)
                return true;

            if (!settlement.PortPosition.IsValid() || settlement.PortPosition.Face.FaceIndex < 0 ||
                !settlement.GatePosition.IsValid() || settlement.GatePosition.Face.FaceIndex < 0)
            {
                __result = settlement.PortPosition.ToVec2().Distance(settlement.GatePosition.ToVec2());
                return false;
            }

            return true;
        }
    }
}
