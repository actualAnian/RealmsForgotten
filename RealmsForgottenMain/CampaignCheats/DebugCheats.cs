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
    public static class DebugCheats
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("check_caches", "rf.debug")]
        public static string CheckDistanceCaches(List<string> args)
        {
            if (args.Count == 0)
                return "Possible options: all, naval, default";
            if (!Enum.TryParse<MobileParty.NavigationType>(
                    args[0],
                    ignoreCase: true,
                    out var optionChosen))
                return "Invalid option. Possible options: all, naval, default";
            var message = "";
            if (Campaign.Current.Models.MapDistanceModel is not NavalDLC.GameComponents.NavalDLCMapDistanceModel distanceModel)
                return message + "\n the distance model is not NavalDLCMapDistanceModel, skipping cache check";

            var navigationCachesField = distanceModel.GetType()
                .GetField("_navigationCaches", BindingFlags.Instance | BindingFlags.NonPublic);

            var navigationCaches = navigationCachesField?.GetValue(distanceModel)
                as Dictionary<MobileParty.NavigationType, MapDistanceModel.INavigationCache>;

            foreach (var kvp in navigationCaches!)
            {
                var cacheType = kvp.Key;
                var cache = kvp.Value;
                if (cacheType != optionChosen) continue;

                var settlementsWithBrokenCaches = new HashSet<string>();
                var sandboxCache = cache as SandBoxNavigationCache;
                var distanceField = typeof(NavigationCache<Settlement>).GetField("_settlementToSettlementDistanceWithLandRatio", BindingFlags.Instance | BindingFlags.NonPublic);

                var distances = distanceField?.GetValue(sandboxCache)
                    as Dictionary<NavigationCacheElement<Settlement>, Dictionary<NavigationCacheElement<Settlement>, (float, float)>>;
                var maxConnections = distances!.Values.Count - 1;

                foreach (var outerEntry in distances!)
                {
                    var fromElement = outerEntry.Key;
                    var innerDict = outerEntry.Value;

                    if (innerDict.Count != maxConnections)
                        message += $"Info: settlement with id {fromElement.Settlement.StringId} has {innerDict.Count} connections to other settlements. all settlements with ports - {maxConnections}\n";
                    foreach (var innerEntry in innerDict)
                    {
                        var toElement = innerEntry.Key;
                        var tuple = innerEntry.Value;

                        float distance = tuple.Item1;

                        if (distance > 1E8f)
                            settlementsWithBrokenCaches.Add($"from: {fromElement.Settlement.StringId}, to: {toElement.Settlement.StringId}");
                    }
                }
                message += "\n----------------------------\n";
                message += "Broken connections in cache: " + cacheType + "\n";
                message += settlementsWithBrokenCaches.Count == 0 ? "None" : string.Join("\n", settlementsWithBrokenCaches);
            }

            return message;
        }
        [CommandLineFunctionality.CommandLineArgumentFunction("check_port_terrains", "rf.debug")]
        public static string CheckPortTerrains(List<string> args)
        {
            List<string> wrongPorts = new();
            foreach (var settlement in Settlement.All.Where(s => s.HasPort))
            {
                var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(settlement.PortPosition.Face);
                if (!Campaign.Current.Models.PartyNavigationModel.IsTerrainTypeValidForNavigationType(terrain, MobileParty.NavigationType.Naval))
                    wrongPorts.Add(settlement.StringId);
            }
            return wrongPorts.Count == 0 ? "All ports are in correct terrains" : "Ports of settlements: " + string.Join(", ", wrongPorts) + " are using wrong terrains";
        }
        [CommandLineFunctionality.CommandLineArgumentFunction("check_settlement_entrances", "rf.debug")]
        public static string CheckSettlementEntrances(List<string> args)
        {
            List<string> withBrokenFaces = new();
            foreach (Settlement settlement in Settlement.All)
            {
                if (!settlement.GatePosition.Face.IsValid())
                    withBrokenFaces.Add(settlement.StringId);
            }
            var message = "Settlements with broken entrances: ";
            message += withBrokenFaces.Count == 0 ? "None" : string.Join(", ", withBrokenFaces);
            return message;
        }
    }
}
