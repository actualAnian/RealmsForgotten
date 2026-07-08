using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace RFCustomSettlements.Patches
{
    [HarmonyPatch(typeof(PlayerCaptivityCampaignBehavior), nameof(PlayerCaptivityCampaignBehavior.CheckCaptivityChange))]
    internal class CheckCaptivityChangePatch
    {
        static readonly float hoursNeeded = 24;
        static float tickStart = new Random().Next(0, 12);
        private static void Prefix(PlayerCaptivityCampaignBehavior __instance, float dt)
        {
            List<string> culturesCapturingSlaves = new() { "aserai", "athas_enslavers" };
            if (culturesCapturingSlaves.Contains(PlayerCaptivity.CaptorParty.Culture.StringId))
            {
                if (tickStart < hoursNeeded)
                {
                    tickStart += dt;
                }
                else
                {
                    tickStart = new Random().Next(0, 12);
                    ArenaCampaignBehavior.TeleportCapturedPlayerToArena();
                    return;
                }
            }
        }
    }
}