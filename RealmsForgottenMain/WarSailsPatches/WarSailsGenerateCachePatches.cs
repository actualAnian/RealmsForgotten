
// crash prevention??
// patched manually in LateHarmonyPatches
//[HarmonyPatch(typeof(NavalDLCMapDistanceModel), "GetDistance")]
using HarmonyLib;
using NavalDLC.GameComponents;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

public class NavalDLCMapDistanceModelGetDistancePatch
{
    public static MethodBase TargetMethod()
    {
        // Find the specific overload with the "in CampaignVec2" parameter
        var type = typeof(NavalDLCMapDistanceModel);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            if (method.Name != "GetDistance") continue;

            var parameters = method.GetParameters();
            if (parameters.Length == 4 &&
                parameters[0].ParameterType == typeof(Settlement) &&
                parameters[1].ParameterType.IsByRef && // This is the "in CampaignVec2"
                parameters[1].ParameterType.GetElementType() == typeof(CampaignVec2) &&
                parameters[2].ParameterType == typeof(bool) &&
                parameters[3].ParameterType == typeof(MobileParty.NavigationType))
            {
                //LTLogger.Debug($"[NavalDLC Patch] Found target method: {method}");
                return method;
            }
        }
        InformationManager.DisplayMessage(new("[NavalDLC Patch] ERROR: Could not find target method!"));
        return null;
    }

    static bool Prefix(
        Settlement fromSettlement,
        ref CampaignVec2 toPoint,
        bool isFromPort,
        MobileParty.NavigationType customCapability,
        ref float __result)
    {
        try
        {
            // Check for null settlement
            if (fromSettlement == null)
            {
                InformationManager.DisplayMessage(new($"[NavalDLC Crash Prevention IsPositionInsideNavalSafeZone] fromSettlement is NULL | Position: ({toPoint.X}, {toPoint.Y}) | IsFromPort: {isFromPort} | NavType: {customCapability}"));
                __result = float.MaxValue;
                return false;
            }

            // Check gate/port positions
            CampaignVec2 campaignVec = isFromPort ? fromSettlement.PortPosition : fromSettlement.GatePosition;

            if (campaignVec.X == 0f && campaignVec.Y == 0f)
            {
                InformationManager.DisplayMessage(new($"[NavalDLC Crash Prevention] Invalid position for settlement '{fromSettlement.Name}' | IsFromPort: {isFromPort} | Position: ({campaignVec.X}, {campaignVec.Y})"));
                __result = float.MaxValue;
                return false;
            }

            // Check toPoint validity
            if (toPoint.X == 0f && toPoint.Y == 0f)
            {
                InformationManager.DisplayMessage(new($"[NavalDLC Crash Prevention] Invalid toPoint | Position: ({toPoint.X}, {toPoint.Y}) | FromSettlement: '{fromSettlement.Name}'"));
                __result = float.MaxValue;
                return false;
            }

            // Check face records
            PathFaceRecord toFace = toPoint.Face;
            PathFaceRecord fromFace = campaignVec.Face;

            if (toFace.FaceIndex < 0)
            {
                InformationManager.DisplayMessage(new($"[NavalDLC Crash Prevention] Invalid toPoint Face | FaceIndex: {toFace.FaceIndex} | ToPoint: ({toPoint.X}, {toPoint.Y}) | FromSettlement: '{fromSettlement.Name}'"));
                __result = float.MaxValue;
                return false;
            }

            if (fromFace.FaceIndex < 0)
            {
                InformationManager.DisplayMessage(new($"[NavalDLC Crash Prevention] Invalid fromSettlement Face | FaceIndex: {fromFace.FaceIndex} | Position: ({campaignVec.X}, {campaignVec.Y}) | Settlement: '{fromSettlement.Name}' | IsFromPort: {isFromPort}"));
                __result = float.MaxValue;
                return false;
            }

            // Check critical models
            //if (Campaign.Current?.Models?.MapDistanceModel == null)
            //{
            //    LTLogger.Debug("[NavalDLC Crash Prevention] MapDistanceModel is NULL");
            //    __result = float.MaxValue;
            //    return false;
            //}

            //if (Campaign.Current?.Models?.PartyNavigationModel == null)
            //{
            //    LTLogger.Debug("[NavalDLC Crash Prevention] PartyNavigationModel is NULL");
            //    __result = float.MaxValue;
            //    return false;
            //}

            //if (Campaign.Current?.MapSceneWrapper == null)
            //{
            //    LTLogger.Debug("[NavalDLC Crash Prevention] MapSceneWrapper is NULL");
            //    __result = float.MaxValue;
            //    return false;
            //}

            // If different faces, check GetClosestEntranceToFace result
            //if (fromFace.FaceIndex != toFace.FaceIndex)
            //{
            //    MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
            //    ValueTuple<Settlement, bool> closestEntranceToFace = mapDistanceModel.GetClosestEntranceToFace(toFace, customCapability);
            //    Settlement closestSettlement = closestEntranceToFace.Item1;

            //    if (closestSettlement == null)
            //    {
            //        LTLogger.Debug($"[NavalDLC Crash Prevention] GetClosestEntranceToFace returned NULL | FromSettlement: '{fromSettlement.Name}' | ToPoint: ({toPoint.X}, {toPoint.Y}) | ToFace: {toFace.FaceIndex} | FromFace: {fromFace.FaceIndex} | NavType: {customCapability} | IsFromPort: {isFromPort}");
            //        __result = float.MaxValue;
            //        return false;
            //    }

            //    // Additional check for port/gate positions of closest settlement
            //    bool isClosestPort = closestEntranceToFace.Item2;
            //    CampaignVec2 closestEntrance = isClosestPort ? closestSettlement.PortPosition : closestSettlement.GatePosition;

            //    if (closestEntrance.X == 0f && closestEntrance.Y == 0f)
            //    {
            //        LTLogger.Debug($"[NavalDLC Crash Prevention] Invalid entrance for closest settlement '{closestSettlement.Name}' | IsPort: {isClosestPort} | Position: ({closestEntrance.X}, {closestEntrance.Y})");
            //        __result = float.MaxValue;
            //        return false;
            //    }
            //}

            // All checks passed, allow original method to run
            return true;
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new($"[NavalDLC Crash Prevention] Unexpected error in prefix: {ex.Message}"));
            __result = float.MaxValue;
            return false;
        }
    }
}



// Crash prevention for NavalDLCBanditDensityModel.IsPositionInsideNavalSafeZone
// GetClosestEntranceToFace returns null Item1 on custom map faces with no reachable naval settlement,
// causing NullReferenceException both in GetDistance(item, ...) and item.IsVillage (1.4beta).
// Called from NavalDLCMobilePartyAIModel.ShouldConsiderAttacking during every AI tick.
[HarmonyPatch(typeof(NavalDLCBanditDensityModel), nameof(NavalDLCBanditDensityModel.IsPositionInsideNavalSafeZone))]
public class NavalDLCBanditDensityModel_IsPositionInsideNavalSafeZone_Patch
{
    public static bool Prefix(CampaignVec2 position, ref bool __result)
    {
        try
        {
            __result = false;

            if (!position.IsValid() || position.IsOnLand)
                return false;

            Settlement item = Campaign.Current.Models.MapDistanceModel
                .GetClosestEntranceToFace(position.Face, MobileParty.NavigationType.Naval)
                .Item1;

            if (item == null)
            {
                InformationManager.DisplayMessage(new($"[NavalDLC SafeZone] GetClosestEntranceToFace returned NULL for position ({position.X}, {position.Y}) - no reachable naval settlement"));
                return false; // skip original, __result = false
            }

            // item is valid — let original method run safely
            return true;
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new($"[NavalDLC SafeZone] Unexpected error in prefix: {ex.Message}"));
            __result = false;
            return false;
        }
    }
}

