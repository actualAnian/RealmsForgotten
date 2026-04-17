using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

/*
load your map to game with DLC
in console: lt.generate_sdc
wait
rename SDC files by removing _new from their filenames
put them into your mod when doneg
enjoy!
*/

namespace RealmsForgotten.NavalPatches
{
    public class SDCGenerator
    {
        public static void GenerateNavalCaches()
        {
            InformationManager.DisplayMessage(new InformationMessage("Generating SDC caches..."));
            if (Campaign.Current == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(("Campaign not loaded, cannot generate caches")));
                return;
            }
            string modPath = "..\\..\\Modules\\RF_Map\\ModuleData\\DistanceCaches\\";

            // Generate Default (new) cache
            InformationManager.DisplayMessage(new InformationMessage(("Generating Default cache...")));
            SandBoxNavigationCache defaultCache = new SandBoxNavigationCache(MobileParty.NavigationType.Default);
            defaultCache.GenerateCacheData();
            string defaultPath = modPath + "settlements_distance_cache_Default_new.bin";
            defaultCache.Serialize(defaultPath);
            InformationManager.DisplayMessage(new InformationMessage(($"Default cache saved to: {defaultPath}")));

            // Generate Naval cache
            InformationManager.DisplayMessage(new InformationMessage(("Generating Naval cache...")));
            SandBoxNavigationCache navalCache = new SandBoxNavigationCache(MobileParty.NavigationType.Naval);
            navalCache.GenerateCacheData();
            string navalPath = modPath + "settlements_distance_cache_Naval_new.bin";
            navalCache.Serialize(navalPath);
            InformationManager.DisplayMessage(new InformationMessage(($"Naval cache saved to: {navalPath}")));

            // Generate All cache
            InformationManager.DisplayMessage(new InformationMessage(("Generating All cache...")));
            SandBoxNavigationCache allCache = new SandBoxNavigationCache(MobileParty.NavigationType.All);
            allCache.GenerateCacheData();
            string allPath = modPath + "settlements_distance_cache_All_new.bin";
            allCache.Serialize(allPath);
            InformationManager.DisplayMessage(new InformationMessage(($"All cache saved to: {allPath}")));


            InformationManager.DisplayMessage(new InformationMessage(("Naval caches generated successfully!")));
            InformationManager.DisplayMessage(new InformationMessage("SDC generated successfully!"));
        }


        // console command to generate SDC
        [CommandLineFunctionality.CommandLineArgumentFunction("generate_sdc", "lt")]
        public static string GenerateNavalCaches(List<string> args)
        {
            try
            {
                SDCGenerator.GenerateNavalCaches();
                return "SDC generated successfully!";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
