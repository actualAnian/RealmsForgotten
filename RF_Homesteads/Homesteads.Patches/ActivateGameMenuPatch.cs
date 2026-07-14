using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(GameMenu), "ActivateGameMenu")]
internal class ActivateGameMenuPatch
{
	[HarmonyPrefix]
	private static void Prefix(ref string menuId)
	{
		try
		{
			if (menuId == "encounter" && HomesteadBehavior.Instance?.CurrentHomestead != null)
			{
				menuId = "homestead_menu_encounter";
				TraceLogger.Write("ActivateGameMenuPatch", "Redirected ActivateGameMenu('encounter') to 'homestead_menu_encounter'");
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("ActivateGameMenuPatch", $"Exception: {arg}");
		}
	}
}
