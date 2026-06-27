using HarmonyLib;
using TaleWorlds.CampaignSystem.GameState;

namespace RF_Enlistment;

internal static class RFMapStateEnlistmentPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapState), nameof(MapState.OnMapConversationOver))]
    private static void OnMapConversationOverPostfix()
    {
        RFEnlistmentCampaignBehavior.Instance?.TryResolvePendingCommanderMenuEscape();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapState), nameof(MapState.EnterMenuMode))]
    private static void EnterMenuModePostfix()
    {
        RFEnlistmentCampaignBehavior.Instance?.TryResolvePendingCommanderMenuEscape();
    }
}
