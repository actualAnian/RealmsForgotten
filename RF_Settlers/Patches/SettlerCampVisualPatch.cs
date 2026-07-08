using System;
using HarmonyLib;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RF_Settlers.Patches
{
    /// <summary>
    /// Settler camps are stationary MobileParties, so vanilla would draw them
    /// as walking villager figures. This prefix reroutes their map icon to the
    /// base game's siege-camp TENT (the same visual besieger camps get, banner
    /// flag included) — until a custom camp icon is authored. Caravans on the
    /// road keep the normal party figures.
    /// </summary>
    [HarmonyPatch(typeof(MobilePartyVisual), "AddMobileIconComponents")]
    public static class SettlerCampVisualPatch
    {
        private static bool Prefix(
            MobilePartyVisual __instance,
            PartyBase party,
            ref bool clearBannerComponentCache,
            ref bool clearBannerEntityCache)
        {
            if (party?.MobileParty?.PartyComponent is not SettlerCampComponent)
            {
                return true;
            }

            try
            {
                __instance.AddTentEntityForParty(__instance.StrategicEntity, party, ref clearBannerComponentCache);
                return false;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Camp tent visual failed, falling back to default icon: {exception}");
                return true;
            }
        }
    }
}
