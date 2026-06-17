using HarmonyLib;
using TaleWorlds.CampaignSystem.Issues;
using System.Reflection;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(HeadmanNeedsToDeliverAHerdIssueBehavior.HeadmanNeedsToDeliverAHerdIssue), "IssueStayAliveConditions")]
    public static class Patch_HeadmanIssueStayAliveConditions
    {
        static bool Prefix(HeadmanNeedsToDeliverAHerdIssueBehavior.HeadmanNeedsToDeliverAHerdIssue __instance, ref bool __result)
        {
            // Use reflection to attempt to get the "Herd" property or field.
            // Replace "Herd" with the actual field or property name if different.
            PropertyInfo herdProperty = __instance.GetType().GetProperty("Herd", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            object herdValue = herdProperty != null ? herdProperty.GetValue(__instance) : null;

            // If the herd (or required object) is null, return false safely.
            if (herdValue == null)
            {
                __result = false;
                return false; // Skip the original method.
            }
            return true; // Otherwise, let the original method run.
        }
    }
}