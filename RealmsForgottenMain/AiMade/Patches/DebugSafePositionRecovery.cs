using HarmonyLib;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Settlements;


namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(MobileParty), "RecoverPositionsForNavMeshUpdate")]
    public class DebugSafePositionRecovery
    {
        // Hardcoded to your Documents folder
        private static readonly string LogFilePath =
            @"C:\Users\gupol\Documents\party_position_debug.txt";

        static bool Prefix(MobileParty __instance)
        {
            try
            {
                if (__instance.Position2D.IsNonZero() &&
                    !PartyBase.IsPositionOkForTraveling(__instance.Position2D))
                {
                    Log($"INVALID party position: {__instance.StringId} at {__instance.Position2D}");

                    Vec2 fallback = new Vec2(500f, 500f);

                    if (__instance.CurrentSettlement != null)
                    {
                        __instance.Position2D = __instance.CurrentSettlement.GatePosition;
                        Log($"Moved {__instance.StringId} to its settlement gate.");
                    }
                    else
                    {
                        Settlement village = Settlement.All
                            .Where(s => s.IsVillage && s.IsVisible && s.Position2D != Vec2.Zero)
                            .OrderBy(s => s.Position2D.DistanceSquared(__instance.Position2D))
                            .FirstOrDefault();

                        if (village != null)
                        {
                            __instance.Position2D = village.GatePosition;
                            Log($"Moved {__instance.StringId} to nearest village {village.StringId}.");
                        }
                        else
                        {
                            Log($"No fallback village found for {__instance.StringId}. Moving to safe zone.");
                            __instance.Position2D = fallback;
                        }
                    }
                }

                if (__instance.CurrentSettlement != null)
                {
                    float epsilon = __instance.CurrentSettlement.IsFortification
                        ? Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringTown
                        : Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringVillage;

                    if (!__instance.CurrentSettlement.GatePosition.NearlyEquals(__instance.Position2D, epsilon))
                    {
                        Log($"{__instance.StringId} not near gate. Adjusting...");
                        __instance.Position2D = __instance.CurrentSettlement.GatePosition;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"CRASH AVOIDED for {__instance?.StringId ?? "unknown"} → {ex}");
            }

            return false; // Skip original method
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Fails silently if file write is blocked
            }
        }
    }
}