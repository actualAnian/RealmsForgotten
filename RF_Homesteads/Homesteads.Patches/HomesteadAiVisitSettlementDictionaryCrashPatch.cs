using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
internal static class HomesteadAiVisitSettlementDictionaryCrashPatch
{
	private static FieldInfo? _dictField;

	private static int _lastKnownSettlementCount = -1;

	[HarmonyPrefix]
	private static void Prefix(AiVisitSettlementBehavior __instance)
	{
		try
		{
			int num = MBObjectManager.Instance?.GetObjectTypeList<Settlement>().Count ?? (-1);
			if (num == _lastKnownSettlementCount)
			{
				return;
			}
			_lastKnownSettlementCount = num;
			if ((object)_dictField == null)
			{
				_dictField = AccessTools.Field(typeof(AiVisitSettlementBehavior), "_numberOfAlliedMobilePartiesTargetingSettlement");
			}
			if (!(_dictField?.GetValue(__instance) is Dictionary<Settlement, int> dictionary))
			{
				return;
			}
			foreach (Settlement item in Settlement.All)
			{
				if (item != null && item.StringId != null && item.StringId.StartsWith("hsr_") && (item.IsFortification || item.IsVillage) && !dictionary.ContainsKey(item))
				{
					dictionary[item] = 0;
					TraceLogger.Write("HomesteadAiVisitSettlementDictionaryCrashPatch", "Seeded missing dictionary entry for '" + item.StringId + "' before AiHourlyTick to avoid a KeyNotFoundException.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadAiVisitSettlementDictionaryCrashPatch", "Prefix failed: " + ex.Message);
		}
	}

	[HarmonyFinalizer]
	private static Exception? Finalizer(Exception? __exception)
	{
		if (__exception is KeyNotFoundException)
		{
			TraceLogger.Write("HomesteadAiVisitSettlementDictionaryCrashPatch", "Suppressed KeyNotFoundException from AiHourlyTick (missing targeting-dictionary entry) — skipped this tick's visit scoring instead of crashing.");
			return null;
		}
		return __exception;
	}
}
