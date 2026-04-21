using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RealmsForgotten.CampaignCheats
{
    public class SDCGenerator
    {
        const string modPath = "..\\..\\Modules\\RF_Map\\ModuleData\\DistanceCaches\\";
        readonly static string navalPath = modPath + "settlements_distance_cache_Naval_new.bin";
        readonly static string allPath = modPath + "settlements_distance_cache_All_new.bin";
        readonly static string defaultPath = modPath + "settlements_distance_cache_Default_new.bin";

        public static void GenerateNavalCaches(MobileParty.NavigationType navTypeChosen)
        {
            var cache = new SandBoxNavigationCache(navTypeChosen);
            cache.GenerateCacheData();
            string path = "";
            switch (navTypeChosen)
            {
                case MobileParty.NavigationType.Naval:
                    path = navalPath;
                    break;
                case MobileParty.NavigationType.All:
                    path = allPath;
                    break;
                case MobileParty.NavigationType.Default:
                    path = defaultPath;
                    break;
                default:
                    break;
            }
            cache.Serialize(path);

        }

        [CommandLineFunctionality.CommandLineArgumentFunction("generate_sdc", "lt")]
        public static string GenerateNavalCaches(List<string> args)
        {
            if (args.Count == 0)
                return "Possible options: all, naval, default";
            if (!Enum.TryParse<MobileParty.NavigationType>(
                    args[0],
                    ignoreCase: true,
                    out var optionChosen))
                return "Invalid option. Possible options: all, naval, default";

            try
            {
                GenerateNavalCaches(optionChosen);
                return "SDC generated successfully!";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}