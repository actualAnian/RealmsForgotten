using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RF_Settlers.Patches
{
    /// <summary>
    /// Two protections around vanilla's settlement-visit AI:
    ///
    /// 1. Settler caravans/camps are LEADERLESS kingdom-faction parties — a
    ///    shape vanilla's visit/merge logic only half-expects (it is written
    ///    for lord parties that momentarily lost their leader). Our parties
    ///    are driven by their own behavior anyway, so they skip this AI
    ///    entirely.
    ///
    /// 2. A KeyNotFoundException inside this hourly think (seen in testing
    ///    after a settler village entered the kingdom's settlement lists) must
    ///    not take the whole campaign down: the finalizer swallows it, logs
    ///    WHICH party was thinking, and lets the game continue — the party
    ///    simply skips one AI evaluation. The log line is the telemetry to
    ///    pinpoint the vanilla cache that missed the new settlement.
    /// </summary>
    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
    public static class SettlerAiThinkPatch
    {
        private static bool Prefix(MobileParty mobileParty)
        {
            // Settler parties/camps plus every self-driven RF party (resource
            // zones, their caravans): leaderless — and bandit-held zones are
            // OWNERLESS, which vanilla's suitability check dereferences
            // (mobileParty.Party.Owner.MapFaction) without a null guard.
            return mobileParty?.PartyComponent
                is not (SettlerPartyComponent or SettlerCampComponent or IRFSelfDrivenParty);
        }

        private static Exception Finalizer(Exception __exception, MobileParty mobileParty)
        {
            if (__exception == null)
            {
                return null;
            }

            if (__exception is KeyNotFoundException or NullReferenceException)
            {
                // Skipping ONE hourly visit-evaluation for one party is always
                // safer than ending the campaign — and the log line identifies
                // exactly which party shape vanilla choked on.
                SettlersLog.Write($"Suppressed {__exception.GetType().Name} in AiVisitSettlementBehavior.AiHourlyTick "
                    + $"for party '{mobileParty?.StringId}' (component={mobileParty?.PartyComponent?.GetType().Name ?? "none"}, "
                    + $"owner={mobileParty?.Party?.Owner?.Name?.ToString() ?? "<null>"}, "
                    + $"leader={mobileParty?.LeaderHero?.Name?.ToString() ?? "<none>"}, faction={mobileParty?.MapFaction?.Name?.ToString() ?? "?"}). "
                    + $"Stack: {__exception.StackTrace}");
                return null;
            }

            return __exception;
        }
    }
}
