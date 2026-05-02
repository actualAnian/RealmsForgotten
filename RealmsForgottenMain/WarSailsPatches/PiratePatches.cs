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
       ("northern_pirates", new Vec2(994, 1108), 10f),
        ("northern_pirates", new Vec2(305, 736), 10f),
        ("northern_pirates", new Vec2(683, 713), 10f),
        ("northern_pirates", new Vec2(400, 1138), 10f),
        ("southern_pirates", new Vec2(418, 444), 10f),
        ("southern_pirates", new Vec2(946, 827), 10f),
        ("southern_pirates", new Vec2(624, 501), 10f),
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