using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(HeroCreator), "DeliverOffSpring")]
    internal class DeliverOffspringPatch
    {
        private static readonly string LogFilePath = @"C:\Users\gupol\Desktop\Birthlog.txt"; // Custom file path

        [HarmonyPrefix]
        private static void Prefix(Hero mother, Hero father, bool isOffspringFemale, ref (int, int) __state)
        {
            // Store parent races before modifying them
            __state.Item1 = ((BasicCharacterObject)mother.CharacterObject).Race;
            __state.Item2 = ((BasicCharacterObject)father.CharacterObject).Race;

            ((BasicCharacterObject)mother.CharacterObject).Race = 0;
            ((BasicCharacterObject)father.CharacterObject).Race = 0;

            // Logging to file & game messages
            if (mother != null && father != null)
            {
                string motherFaction = mother.Clan?.Kingdom?.Name?.ToString() ?? "No Faction";
                string fatherFaction = father.Clan?.Kingdom?.Name?.ToString() ?? "No Faction";

                string logEntry = $"[PREFIX] Creating offspring -> " +
                                  $"Mother: {mother.Name} (ID: {mother.StringId}, Race: {__state.Item1}, Culture: {mother.Culture?.Name}, Faction: {motherFaction}) | " +
                                  $"Father: {father.Name} (ID: {father.StringId}, Race: {__state.Item2}, Culture: {father.Culture?.Name}, Faction: {fatherFaction})";

                InformationManager.DisplayMessage(new InformationMessage(logEntry));
                AppendToLogFile(logEntry);
            }
            else
            {
                string errorLog = "[ERROR] One or both parents are null! Possible issue?";
                InformationManager.DisplayMessage(new InformationMessage(errorLog, Colors.Red));
                AppendToLogFile(errorLog);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(Hero mother, Hero father, Hero __result, ref (int, int) __state)
        {
            // Restore parent race values
            ((BasicCharacterObject)mother.CharacterObject).Race = __state.Item1;
            ((BasicCharacterObject)father.CharacterObject).Race = __state.Item2;

            // Apply custom racial mix logic
            CharacterRacialMix.CreateForNewborn(__result);

            // Logging to file & game messages
            if (__result != null)
            {
                string childFaction = __result.Clan?.Kingdom?.Name?.ToString() ?? "No Faction";

                string logEntry = $"[POSTFIX] Offspring Created -> " +
                                  $"Name: {__result.Name} (ID: {__result.StringId}, Culture: {__result.Culture?.Name}, Faction: {childFaction})";

                InformationManager.DisplayMessage(new InformationMessage(logEntry, Colors.Green));
                AppendToLogFile(logEntry);
            }
            else
            {
                string errorLog = "[ERROR] Offspring creation failed! NULL offspring.";
                InformationManager.DisplayMessage(new InformationMessage(errorLog, Colors.Red));
                AppendToLogFile(errorLog);
            }
        }

        private static void AppendToLogFile(string logEntry)
        {
            try
            {
                string timestampedEntry = $"{DateTime.Now}: {logEntry}";
                File.AppendAllText(LogFilePath, timestampedEntry + Environment.NewLine);  // ✅ Fixes "File does not exist" error
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[ERROR] Could not write to log file: {ex.Message}", Colors.Red));
            }
        }
    }
}