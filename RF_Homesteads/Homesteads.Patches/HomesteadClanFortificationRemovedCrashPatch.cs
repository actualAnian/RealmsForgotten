using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Clan), "OnFortificationRemoved")]
internal static class HomesteadClanFortificationRemovedCrashPatch
{
	private static readonly string[] CacheFields = new string[4] { "_fiefsCache", "_townsCache", "_villagesCache", "_settlementsCache" };

	[HarmonyPrefix]
	private static bool Prefix(Clan __instance, Town settlement)
	{
		try
		{
			if (__instance == null)
			{
				return true;
			}
			string[] cacheFields = CacheFields;
			foreach (string text in cacheFields)
			{
				FieldInfo fieldInfo = AccessTools.Field(typeof(Clan), text);
				if (fieldInfo != null && fieldInfo.GetValue(__instance) == null)
				{
					fieldInfo.SetValue(__instance, Activator.CreateInstance(fieldInfo.FieldType));
					TraceLogger.Write("HomesteadClanFortificationRemovedCrashPatch", "Initialized null '" + text + "' on clan '" + __instance.StringId + "' before OnFortificationRemoved ran.");
				}
			}
			if (settlement?.Settlement == null)
			{
				TraceLogger.Write("HomesteadClanFortificationRemovedCrashPatch", "Town has no Settlement back-reference yet (still mid-Deserialize) — skipping OnFortificationRemoved for clan '" + __instance.StringId + "' (nothing valid to remove from freshly-initialized caches).");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadClanFortificationRemovedCrashPatch", "Prefix failed: " + ex.Message);
			return true;
		}
	}

	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, Clan __instance, Town settlement)
	{
		if (__exception == null)
		{
			return null;
		}
		TraceLogger.Write("HomesteadClanFortificationRemovedCrashPatch", "OnFortificationRemoved threw for clan '" + __instance?.StringId + "' / town '" + settlement?.StringId + "' — suppressing instead of crashing: " + __exception.GetType().Name + ": " + __exception.Message);
		return null;
	}
}
