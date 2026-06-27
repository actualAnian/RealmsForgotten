using HarmonyLib;
using NavalDLC.View;
using RealmsForgotten.AiMade;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.WarSailsPatches
{
    public static class InitializePirateSpawnPointsPatch
    {
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

        public static bool TryApply(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(NavalMapSceneWrapper), "InitializePirateSpawnPoints");
            if (target == null)
            {
                RFLogger.Log("[Lifecycle] Optional naval patch skipped | InitializePirateSpawnPoints target not found.");
                return false;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(InitializePirateSpawnPointsPatch), nameof(Prefix)));
            return true;
        }

        public static bool Prefix(object __instance)
        {
            try
            {
                var pirateSpawnPoints = Traverse.Create(__instance)
                    .Field("_pirateSpawnPoints")
                    .GetValue<Dictionary<string, List<(CampaignVec2, float)>>>();

                if (pirateSpawnPoints == null)
                    return true;

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
            catch (Exception ex)
            {
                RFLogger.Log($"[Lifecycle] Optional naval patch failed inside InitializePirateSpawnPoints prefix. Falling back to vanilla. error={ex}");
                return true;
            }
        }
    }
}
