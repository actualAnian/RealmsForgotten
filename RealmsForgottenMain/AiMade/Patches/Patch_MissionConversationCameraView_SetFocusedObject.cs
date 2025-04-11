using HarmonyLib;
using SandBox.View.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(MissionConversationCameraView), "SetFocusedObjectForCameraFocus")]
    public static class Patch_MissionConversationCameraView_SetFocusedObject
    {
        static bool Prefix(MissionConversationCameraView __instance)
        {
            // Attempt to retrieve the field that holds the focused object.
            // The field name is assumed to be "_focusedObject" (adjust if necessary).
            FieldInfo focusedField = AccessTools.Field(__instance.GetType(), "_focusedObject");
            if (focusedField != null)
            {
                object focusedObj = focusedField.GetValue(__instance);
                if (focusedObj == null)
                {
                    // No object is set to be focused; skip the camera update.
                    return false;
                }
            }
            // Otherwise, allow the original method to run.
            return true;
        }
    }
}
