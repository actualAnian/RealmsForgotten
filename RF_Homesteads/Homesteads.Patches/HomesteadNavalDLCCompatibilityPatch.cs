using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

internal static class HomesteadNavalDLCCompatibilityPatch
{
	private static bool _applied;

	internal static void TryApply(Harmony harmony)
	{
		if (!_applied)
		{
			try
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				TryPatchOnSettlementEntered(harmony, assemblies);
				TryPatchGetDistance(harmony, assemblies);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadNavalDLCCompatibilityPatch", "TryApply failed: " + ex.GetType().Name + ": " + ex.Message);
			}
			_applied = true;
		}
	}

	private static void TryPatchOnSettlementEntered(Harmony harmony, Assembly[] assemblies)
	{
		Type type = assemblies.SelectMany(delegate(Assembly a)
		{
			try
			{
				return a.GetTypes();
			}
			catch
			{
				return Array.Empty<Type>();
			}
		}).FirstOrDefault((Type t) => t.FullName == "NavalDLC.CampaignBehaviors.ShipUpgradeCampaignBehavior");
		if (!(type == null))
		{
			MethodInfo method = type.GetMethod("OnSettlementEntered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (!(method == null))
			{
				MethodInfo method2 = typeof(HomesteadNavalDLCCompatibilityPatch).GetMethod("OnSettlementEnteredPrefix", BindingFlags.Static | BindingFlags.NonPublic);
				harmony.Patch(method, new HarmonyMethod(method2));
				TraceLogger.Write("HomesteadNavalDLCCompatibilityPatch", "Patched NavalDLC ShipUpgradeCampaignBehavior.OnSettlementEntered.");
			}
		}
	}

	private static bool OnSettlementEnteredPrefix(MobileParty? mobileParty, Settlement settlement, Hero? hero)
	{
		if (settlement != null && settlement.StringId?.StartsWith("hsr_settlement_") == true && !settlement.HasPort)
		{
			return false;
		}
		return true;
	}

	private static void TryPatchGetDistance(Harmony harmony, Assembly[] assemblies)
	{
		Type type = assemblies.SelectMany(delegate(Assembly a)
		{
			try
			{
				return a.GetTypes();
			}
			catch
			{
				return Array.Empty<Type>();
			}
		}).FirstOrDefault((Type t) => t.FullName == "NavalDLC.Models.NavalDLCMapDistanceModel");
		if (!(type == null))
		{
			MethodInfo method = type.GetMethod("GetDistance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (!(method == null))
			{
				MethodInfo method2 = typeof(HomesteadNavalDLCCompatibilityPatch).GetMethod("GetDistanceFinalizer", BindingFlags.Static | BindingFlags.NonPublic);
				harmony.Patch(method, null, null, null, new HarmonyMethod(method2));
				TraceLogger.Write("HomesteadNavalDLCCompatibilityPatch", "Patched NavalDLC NavalDLCMapDistanceModel.GetDistance with exception finalizer.");
			}
		}
	}

	private static Exception? GetDistanceFinalizer(Exception? __exception, ref float __result, Settlement? fromSettlement, Settlement? toSettlement)
	{
		if (__exception == null)
		{
			return null;
		}
		if ((fromSettlement != null && fromSettlement.StringId?.StartsWith("hsr_settlement_") == true) || (toSettlement != null && toSettlement.StringId?.StartsWith("hsr_settlement_") == true))
		{
			__result = float.MaxValue;
			TraceLogger.Write("HomesteadNavalDLCCompatibilityPatch", "GetDistance swallowed exception for '" + fromSettlement?.StringId + "'↔'" + toSettlement?.StringId + "': " + __exception.GetType().Name);
			return null;
		}
		return __exception;
	}
}
