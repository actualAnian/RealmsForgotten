using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MissionNameMarkerTargetBaseVM), "RefreshValues")]
internal static class HomesteadNameMarkerCrashPatch
{
	private static readonly Dictionary<Type, MethodInfo> GetNameCache = new Dictionary<Type, MethodInfo>();

	[HarmonyPrefix]
	private static bool Prefix(MissionNameMarkerTargetBaseVM __instance)
	{
		try
		{
			Type type = ((object)__instance).GetType();
			if (!GetNameCache.TryGetValue(type, out MethodInfo value))
			{
				value = AccessTools.Method(type, "GetName");
				GetNameCache[type] = value;
			}
			if (value == null)
			{
				return true;
			}
			if (value.Invoke(__instance, null) as TextObject == null)
			{
				TraceLogger.WriteOnce("NameMarkerNullName:" + type.Name, "HomesteadNameMarkerCrashPatch", "Marker type '" + type.Name + "' resolved a null name — skipping RefreshValues to avoid a crash.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.WriteOnce("NameMarkerPrefixFail:" + ((object)__instance).GetType().Name, "HomesteadNameMarkerCrashPatch", "GetName() threw for marker type '" + ((object)__instance).GetType().Name + "' — skipping instead of crashing: " + ex.GetType().Name + ": " + ex.Message);
			return false;
		}
	}
}
