using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    [HarmonyPatch(typeof(VillageEncounter), "CreateAndOpenMissionController")]
    internal static class ADODInnPatch
    {
        static bool Prefix(VillageEncounter __instance, ref Settlement ____settlement)
        {
            try
            {
                // Your custom logic here for inn/werewolf encounters
                if (____settlement != null && ____settlement.IsVillage && Campaign.Current.IsNight)
                {
                    // Check if werewolf encounter should trigger
                    // Add your custom conditions here
                }

                // IMPORTANT: Return true to continue to the original method
                return true;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ADODInnPatch error: {ex.Message}", Colors.Red));
                return true; // Still call original method on error
            }
        }

        static void Postfix(VillageEncounter __instance, ref Settlement ____settlement)
        {
            try
            {
                // Your post-processing logic here after the original method runs
                if (____settlement != null && ____settlement.IsVillage)
                {
                    // Custom logic after mission controller is created
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ADODInnPatch postfix error: {ex.Message}", Colors.Red));
            }
        }
    }
}