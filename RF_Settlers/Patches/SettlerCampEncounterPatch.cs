using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RF_Settlers.Patches
{
    /// <summary>
    /// Approaching a settler camp opens a proper game menu instead of dumping
    /// the player straight into a conversation. This mirrors what vanilla does
    /// for armies in the same method: DoMeetingInternal switches to the
    /// "army_encounter" menu before any conversation starts. Hostile
    /// encounters (kingdom at war with the player) never reach the meeting
    /// path, so battles work exactly as before.
    /// </summary>
    [HarmonyPatch(typeof(PlayerEncounter), "DoMeetingInternal")]
    public static class SettlerCampEncounterPatch
    {
        private static bool Prefix(PlayerEncounter __instance)
        {
            // Covers every stationary camp-style component (settler camps,
            // resource zones) — each supplies its own menu id. Even a
            // bandit-held zone opens this menu (its "Attack" option then routes
            // into the standard battle "encounter" menu, exactly like a
            // village's hostile-action menu does).
            if (PlayerEncounter.EncounteredParty?.MobileParty?.PartyComponent
                    is not IRFStationaryCampParty camp
                || string.IsNullOrEmpty(camp.EncounterMenuId))
            {
                return true;
            }

            try
            {
                // Same bookkeeping vanilla's army-menu branch performs before
                // switching menus.
                AccessTools.Property(typeof(PlayerEncounter), "EncounterState")
                    ?.SetValue(__instance, PlayerEncounterState.Begin);
                AccessTools.Field(typeof(PlayerEncounter), "_stateHandled")
                    ?.SetValue(__instance, true);
                GameMenu.SwitchToMenu(camp.EncounterMenuId);
                return false;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Camp menu redirect failed, falling back to conversation: {exception}");
                return true;
            }
        }
    }
}
