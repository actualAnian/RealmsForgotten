using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem;
using System.IO;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(MapEventSide), "ApplySimulatedHitRewardToSelectedTroop")]
    public class DebugApplySimulatedHitRewardPatch
    {
        static bool Prefix(MapEventSide __instance, CharacterObject strikerTroop, CharacterObject attackedTroop, int damage, bool isFinishingStrike)
        {
            string logFile = "C:\\Users\\gupol\\Documents\\BannerlordDebugLog.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(logFile, true))
                {
                    writer.WriteLine("========== DEBUG ApplySimulatedHitRewardToSelectedTroop ==========");

                    if (strikerTroop == null)
                    {
                        writer.WriteLine("ERROR: Striker troop is NULL!");
                    }
                    else
                    {
                        writer.WriteLine($"Striker: {strikerTroop.Name}, Culture: {strikerTroop.Culture}, Level: {strikerTroop.Level}");
                    }

                    if (attackedTroop == null)
                    {
                        writer.WriteLine("ERROR: Attacked troop is NULL!");
                    }
                    else
                    {
                        writer.WriteLine($"Attacked: {attackedTroop.Name}, Culture: {attackedTroop.Culture}, Level: {attackedTroop.Level}");
                    }

                    writer.WriteLine($"Damage: {damage}, Finishing Strike: {isFinishingStrike}");
                }

                return true; // Allow execution of the original function
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"EXCEPTION in ApplySimulatedHitRewardToSelectedTroop: {ex.Message}\n");
                return true;
            }
        }
    }
}