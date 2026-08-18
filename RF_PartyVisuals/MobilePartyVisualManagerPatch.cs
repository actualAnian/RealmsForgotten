using HarmonyLib;
using SandBox.View.Map.Managers;

namespace RF_PartyVisuals
{
    /// <summary>
    /// The only patch in the module: a postfix on MobilePartyVisualManager.OnVisualTick — the
    /// per-frame map-visual sync hook (the same safe hook RF_Homesteads uses). It never touches
    /// the icon-build path, so it cannot corrupt agent poses.
    /// </summary>
    [HarmonyPatch(typeof(MobilePartyVisualManager), "OnVisualTick")]
    public static class MobilePartyVisualManager_OnVisualTick_Patch
    {
        private static void Postfix(float dt)
        {
            PartyVisualsEnhancer.Instance?.OnVisualTick(dt);
        }
    }
}
