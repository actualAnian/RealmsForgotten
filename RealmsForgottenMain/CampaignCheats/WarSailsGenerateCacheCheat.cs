using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.CampaignCheats
{
    public class SDCGenerator
    {
        public static void GenerateNavalCaches()
        {
            string modPath = "..\\..\\Modules\\RF_Map\\ModuleData\\DistanceCaches\\";

            // DEFAULT CACHE CREATION SEEMS TO BE UNECESSAY

            // Generate Default (new) cache
            //SandBoxNavigationCache defaultCache = new SandBoxNavigationCache(MobileParty.NavigationType.Default);
            //defaultCache.GenerateCacheData();
            //string defaultPath = modPath + "settlements_distance_cache_Default_new.bin";
            //defaultCache.Serialize(defaultPath);

            // Generate Naval cache
            SandBoxNavigationCache navalCache = new SandBoxNavigationCache(MobileParty.NavigationType.Naval);
            navalCache.GenerateCacheData();
            string navalPath = modPath + "settlements_distance_cache_Naval_new.bin";
            navalCache.Serialize(navalPath);

            // Generate All cache
            SandBoxNavigationCache allCache = new SandBoxNavigationCache(MobileParty.NavigationType.All);
            allCache.GenerateCacheData();
            string allPath = modPath + "settlements_distance_cache_All_new.bin";
            allCache.Serialize(allPath);
        }
        // console command to generate SDC
        [CommandLineFunctionality.CommandLineArgumentFunction("generate_sdc", "lt")]
        public static string GenerateNavalCaches(List<string> args)
        {
            try
            {
                GenerateNavalCaches();
                return "SDC generated successfully!";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
}
