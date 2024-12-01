using HarmonyLib;
using System;
using System.IO; // Add this for File and Path classes
using System.Xml.Serialization; // Add this for XmlSerializer class
using TaleWorlds.Core; // Add this if required by BasePath.Name
using TaleWorlds.Library; // Ensure this is included as it's part of TaleWorlds
using ServeAsSoldier;
using System.Collections.Generic;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(ServeAsSoldier.SubModule), "loadSettings")]
    public static class LoadSettingsPatch
    {
        static bool Prefix()
        {
            try
            {
                string customPath = Path.Combine(BasePath.Name, "Modules/RealmsForgotten/settings.xml");
                ServeAsSoldier.SubModule.settings = new XmlSerializer(typeof(ServeAsSoldier.Settings))
                    .Deserialize(File.OpenRead(customPath)) as ServeAsSoldier.Settings;
                return false; // Skip the original method
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Failed to load settings: {ex.Message}"));
                return true; // Fall back to original method on failure
            }
        }
    }

    [HarmonyPatch(typeof(ServeAsSoldier.SubModule), "loadRecruit")]
    public static class LoadRecruitPatch
    {
        static bool Prefix()
        {
            try
            {
                string customPath = Path.Combine(BasePath.Name, "Modules/RealmsForgotten/ModuleData/Additional_Troops.xml");
                ServeAsSoldier.SubModule.AdditonalTroops = new XmlSerializer(typeof(List<ServeAsSoldier.Recruit>))
                    .Deserialize(File.OpenRead(customPath)) as List<ServeAsSoldier.Recruit>;
                return false; // Skip the original method
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Failed to load recruits: {ex.Message}"));
                return true; // Fall back to original method on failure
            }
        }
    }
}
