using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch]
public static class HomesteadNotablesCacheWipeDiagnosticPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		Type t = typeof(Settlement);
		BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		string[] array = new string[4] { "CollectNotablesToCache", "OnGameCreated", "AfterInitialized", "OnFinishLoadState" };
		foreach (string name in array)
		{
			MethodInfo method = t.GetMethod(name, flags);
			if (method != null)
			{
				yield return method;
			}
		}
	}

	private static void Prefix(Settlement __instance, MethodBase __originalMethod, out int __state)
	{
		__state = 0;
		try
		{
			if (__instance != null && __instance.StringId != null && __instance.StringId.StartsWith("hsr_settlement_"))
			{
				__state = __instance.HeroesWithoutParty?.Count ?? (-1);
			}
		}
		catch
		{
		}
	}

	private static void Postfix(Settlement __instance, MethodBase __originalMethod, int __state)
	{
		try
		{
			if (__instance != null && __instance.StringId != null && __instance.StringId.StartsWith("hsr_settlement_"))
			{
				int num = __instance.HeroesWithoutParty?.Count ?? (-1);
				TraceLogger.Write("HomesteadNotablesCacheWipeDiagnosticPatch", __originalMethod?.Name + "() on '" + __instance.StringId + "' " + $"objId={RuntimeHelpers.GetHashCode(__instance)} " + $"HWP {__state} -> {num} notables={__instance.Notables?.Count ?? (-1)}. " + FullStack());
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadNotablesCacheWipeDiagnosticPatch", "Postfix failed: " + ex.Message);
		}
	}

	private static string FullStack()
	{
		StackFrame[] frames = new StackTrace(2, fNeedFileInfo: false).GetFrames();
		if (frames == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder("via ");
		for (int i = 0; i < frames.Length; i++)
		{
			MethodBase method = frames[i].GetMethod();
			stringBuilder.Append(method?.DeclaringType?.Name).Append('.').Append(method?.Name);
			if (i < frames.Length - 1)
			{
				stringBuilder.Append(" <- ");
			}
		}
		return stringBuilder.ToString();
	}
}
