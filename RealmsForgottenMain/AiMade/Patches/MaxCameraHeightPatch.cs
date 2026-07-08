using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SandBox.View.Map;
using SandBox;

namespace RealmsForgotten.AiMade.Patches
{
    // slightly better camera max angle down for better zoom
    [HarmonyPatch(typeof(MapCameraView), "CalculateCameraElevation")]
    public static class MapCameraView_CalculateCameraElevation_Patch
    {
        public static bool Prefix(float cameraDistance, ref float __result)
        {
            __result = cameraDistance * 0.5f * 0.015f + 0.0f;
            return false;
        }
    }

    // more camera zoom-out by overriding max map height
    [HarmonyPatch(typeof(MapScene), "GetMapBorders")]
    public static class MapScene_GetMapBorders_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(out float maximumHeight)
        {
            maximumHeight = 1650f;
        }
    }
}