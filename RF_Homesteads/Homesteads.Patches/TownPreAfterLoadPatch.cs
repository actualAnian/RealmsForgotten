using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Town), "PreAfterLoad")]
public static class TownPreAfterLoadPatch
{
	private static readonly string[] CacheFields = new string[4] { "_fiefsCache", "_townsCache", "_villagesCache", "_settlementsCache" };

	public static bool Prefix(Town __instance)
	{
		try
		{
			Clan ownerClan = __instance.OwnerClan;
			if (ownerClan != null)
			{
				EnsureClanCacheListsNonNull(ownerClan);
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("TownPreAfterLoadPatch", $"Prefix failed: {arg}");
		}
		return true;
	}

	private static void EnsureClanCacheListsNonNull(Clan clan)
	{
		string[] cacheFields = CacheFields;
		foreach (string name in cacheFields)
		{
			FieldInfo fieldInfo = AccessTools.Field(typeof(Clan), name);
			if (fieldInfo != null && fieldInfo.GetValue(clan) == null)
			{
				fieldInfo.SetValue(clan, Activator.CreateInstance(fieldInfo.FieldType));
			}
		}
	}
}
