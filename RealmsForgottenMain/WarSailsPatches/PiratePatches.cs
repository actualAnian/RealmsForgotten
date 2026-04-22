using HarmonyLib;
using NavalDLC.View;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

[HarmonyPatch(typeof(NavalMapSceneWrapper), "InitializePirateSpawnPoints")]
public static class InitializePirateSpawnPointsPatch
{
    // Your custom data source
    public static List<(string clanStringId, Vec2 position, float radius)> CustomSpawnPoints = new()
    {
        ("northern_pirates", new Vec2(293, 695), 30f),
        ("southern_pirates", new Vec2(503, 761), 30f),
    };

    static bool Prefix(object __instance)
    {
        var pirateSpawnPoints = Traverse.Create(__instance)
            .Field("_pirateSpawnPoints")
            .GetValue<Dictionary<string, List<(CampaignVec2, float)>>>();

        pirateSpawnPoints.Clear();

        foreach (var (clanStringId, pos, radius) in CustomSpawnPoints)
        {
            if (!pirateSpawnPoints.TryGetValue(clanStringId, out var list))
            {
                list = new List<(CampaignVec2, float)>();
                pirateSpawnPoints[clanStringId] = list;
            }
            var campaignVec = new CampaignVec2(pos, isOnLand: false);
            list.Add((campaignVec, radius));
        }
        return false;
    }
}