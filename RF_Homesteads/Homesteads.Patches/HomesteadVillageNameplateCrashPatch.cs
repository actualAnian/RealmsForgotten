using System;
using HarmonyLib;
using Homesteads.Models;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(SettlementNameplateEventsVM), MethodType.Constructor, new Type[] { typeof(Settlement) })]
internal static class HomesteadVillageNameplateCrashPatch
{
	[HarmonyPrefix]
	private static void Prefix(Settlement settlement)
	{
		try
		{
			Village village = settlement?.Village;
			if (village?.VillageType != null && (village.VillageType.Productions == null || village.VillageType.Productions.Count <= 0))
			{
				VillageType villageType = HomesteadSettlementBuilder.ResolveRealVillageType(village.VillageType.StringId);
				if (villageType != null)
				{
					village.VillageType = villageType;
					TraceLogger.Write("HomesteadVillageNameplateCrashPatch", "Re-resolved village type '" + villageType.StringId + "' for '" + settlement.StringId + "' before nameplate build.");
				}
			}
		}
		catch
		{
		}
	}

	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, Settlement settlement)
	{
		if (__exception == null)
		{
			return null;
		}
		if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_"))
		{
			TraceLogger.Write("HomesteadVillageNameplateCrashPatch", "Swallowed nameplate error for '" + settlement.StringId + "': " + __exception.Message);
			return null;
		}
		return __exception;
	}
}
