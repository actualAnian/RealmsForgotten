using System;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Crafting), "ReIndex")]
internal static class HomesteadForgeCraftNamePatch
{
	[HarmonyPostfix]
	private static void Postfix(Crafting __instance)
	{
		if (!HomesteadForgeContext.IsActive)
		{
			return;
		}
		try
		{
			CraftingTemplate currentCraftingTemplate = __instance.CurrentCraftingTemplate;
			if (currentCraftingTemplate != null)
			{
				TextObject textObject = new TextObject("{=homestead_forge_crafted}Homestead Crafted {CURR_TEMPLATE_NAME}");
				textObject.SetTextVariable("CURR_TEMPLATE_NAME", currentCraftingTemplate.TemplateName);
				__instance.SetCraftedWeaponName(textObject);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadForgeCraftNamePatch", "Postfix failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
