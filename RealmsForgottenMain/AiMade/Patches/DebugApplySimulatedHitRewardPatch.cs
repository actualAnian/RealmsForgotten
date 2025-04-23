using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Roster;
using System.IO;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(TroopRoster), "ClampXp")]
    public class ClampXpDebugPatch
    {
        static bool Prefix(TroopRoster __instance, int index)
        {
            string mainPath = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);
            string logFile = Path.Combine(mainPath, "BannerlordClampXpLog.txt");
            try
            {
                using (StreamWriter writer = new StreamWriter(logFile, true))
                {
                    writer.WriteLine("========== DEBUG ClampXp ==========");

                    if (__instance == null)
                    {
                        writer.WriteLine("ERROR: TroopRoster instance is NULL!");
                        return true; // Allow original function to proceed
                    }

                    if (index < 0 || index >= __instance.Count)
                    {
                        writer.WriteLine($"ERROR: Invalid index {index} for TroopRoster!");
                        return true;
                    }

                    // Safe access using GetCharacterAtIndex
                    CharacterObject character = __instance.GetCharacterAtIndex(index);
                    if (character == null)
                    {
                        writer.WriteLine($"ERROR: CharacterObject at index {index} is NULL!");
                        return true;
                    }

                    // Retrieve troop data safely
                    TroopRosterElement troopData = __instance.GetElementCopyAtIndex(index);
                    if (troopData.Character == null)
                    {
                        writer.WriteLine($"ERROR: Troop at index {index} has NULL CharacterObject!");
                        return true;
                    }

                    writer.WriteLine($"ClampXp called for: {troopData.Character.Name} (Index: {index}, XP: {troopData.Xp}, Count: {troopData.Number})");
                }

                return true; // Allow execution of the original function
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"EXCEPTION in ClampXp: {ex.Message}\n");
                return true;
            }
        }
    }
}