using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_town_outside_on_init")]
    public class Patch_GameMenuTownOutsideOnInit
    {
        static bool Prefix(MenuCallbackArgs args)
        {
            // Check if PlayerEncounter.EncounterSettlement is null
            if (PlayerEncounter.EncounterSettlement == null)
            {
                // Log the issue for debugging
                Console.WriteLine("Error: PlayerEncounter.EncounterSettlement is null in game_menu_town_outside_on_init.");

                // Provide a fallback TextObject to avoid crashes
                args.MenuTitle = new TextObject("Invalid Settlement");

                // Skip the base method to prevent the crash
                return false;
            }

            // If everything is fine, let the base method run
            return true;
        }
    }
}