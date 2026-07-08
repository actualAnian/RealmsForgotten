using System;
using HarmonyLib;
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
            if (PlayerEncounter.EncounteredParty?.MobileParty?.PartyComponent is not SettlerCampComponent)
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
                GameMenu.SwitchToMenu("rf_settler_camp");
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
