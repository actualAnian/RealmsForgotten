using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch]
public static class HomesteadHwpRemoveHeroDiagnosticPatch
{
	private static MethodBase TargetMethod()
	{
		return typeof(Settlement).GetMethod("RemoveHeroWithoutParty", BindingFlags.Instance | BindingFlags.NonPublic);
	}

	private static void Prefix(Settlement __instance, Hero individual)
	{
		try
		{
			if (__instance != null && __instance.StringId != null && __instance.StringId.StartsWith("hsr_settlement_"))
			{
				TraceLogger.Write("HomesteadHwpRemoveHeroDiagnosticPatch", $"RemoveHeroWithoutParty('{individual?.Name}') on '{__instance.StringId}'. {ShortStack()}");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadHwpRemoveHeroDiagnosticPatch", "Prefix failed: " + ex.Message);
		}
	}

	internal static string ShortStack()
	{
		StackFrame[] frames = new StackTrace(2, fNeedFileInfo: false).GetFrames();
		if (frames == null)
		{
			return "";
		}
		int num = Math.Min(4, frames.Length);
		StringBuilder stringBuilder = new StringBuilder("via ");
		for (int i = 0; i < num; i++)
		{
			MethodBase method = frames[i].GetMethod();
			stringBuilder.Append(method?.DeclaringType?.Name).Append('.').Append(method?.Name);
			if (i < num - 1)
			{
				stringBuilder.Append(" <- ");
			}
		}
		return stringBuilder.ToString();
	}
}
