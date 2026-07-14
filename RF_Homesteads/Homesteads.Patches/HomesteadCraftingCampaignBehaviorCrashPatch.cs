using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(CraftingCampaignBehavior), "CreateTownOrder")]
internal static class HomesteadCraftingCampaignBehaviorCrashPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Hero orderOwner, int orderSlot)
	{
		try
		{
			if (orderOwner == null || orderOwner.CurrentSettlement?.Town == null)
			{
				TraceLogger.Write("HomesteadCraftingCampaignBehaviorCrashPatch", "Prevented crash in CreateTownOrder: order owner '" + (orderOwner?.Name?.ToString() ?? "null") + "' has CurrentSettlement='" + (orderOwner?.CurrentSettlement?.StringId ?? "null") + "' (no Town). Skipping order.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadCraftingCampaignBehaviorCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}

	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception)
	{
		if (__exception is NullReferenceException || __exception is KeyNotFoundException)
		{
			TraceLogger.Write("HomesteadCraftingCampaignBehaviorCrashPatch", "Suppressed " + __exception.GetType().Name + " in CreateTownOrder (order skipped this tick).");
			return null;
		}
		return __exception;
	}
}
