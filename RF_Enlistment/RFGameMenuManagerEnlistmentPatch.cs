using HarmonyLib;
using TaleWorlds.CampaignSystem.GameMenus;

namespace RF_Enlistment;

internal static class RFGameMenuManagerEnlistmentPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameMenuManager), nameof(GameMenuManager.SetNextMenu))]
    private static void SetNextMenuPrefix(ref string name)
    {
        RFEnlistmentCampaignBehavior.Instance?.TryOverrideRequestedNativeMenu(ref name);
    }
}
