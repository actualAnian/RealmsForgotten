using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Logic;

/// <summary>
/// Optional bridge from the campaign-map territory model to the war planner.
/// The war system keeps its distance-based fallback until a territory snapshot
/// has been built, so loading screens and non-map campaign states stay safe.
/// </summary>
internal static class RFWarPoliticalBorderContext
{
    internal static Func<Kingdom, Kingdom, float>? AdjacencyProvider { get; set; }

    internal static Func<Kingdom, Settlement, Kingdom?, float>? FrontierProvider { get; set; }

    internal static bool IsAvailable => AdjacencyProvider != null && FrontierProvider != null;

    internal static float GetAdjacency(Kingdom first, Kingdom second)
    {
        if (first == null || second == null || first == second || AdjacencyProvider == null)
        {
            return 0f;
        }

        try
        {
            return Clamp01(AdjacencyProvider(first, second));
        }
        catch
        {
            return 0f;
        }
    }

    internal static float GetFrontierWeight(Kingdom owner, Settlement settlement, Kingdom? specificEnemy)
    {
        if (owner == null || settlement == null || FrontierProvider == null)
        {
            return 0f;
        }

        try
        {
            return Clamp01(FrontierProvider(owner, settlement, specificEnemy));
        }
        catch
        {
            return 0f;
        }
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }
}
