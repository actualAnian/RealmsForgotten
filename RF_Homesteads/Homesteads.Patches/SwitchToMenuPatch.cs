using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(GameMenu), "SwitchToMenu")]
internal class SwitchToMenuPatch
{
	[HarmonyPrefix]
	private static void Prefix(ref string menuId)
	{
		try
		{
			if (menuId == "encounter" && HomesteadBehavior.Instance?.CurrentHomestead != null)
			{
				menuId = "homestead_menu_encounter";
				TraceLogger.Write("SwitchToMenuPatch", "Redirected SwitchToMenu('encounter') to 'homestead_menu_encounter'");
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("SwitchToMenuPatch", $"Exception: {arg}");
		}
	}
}
