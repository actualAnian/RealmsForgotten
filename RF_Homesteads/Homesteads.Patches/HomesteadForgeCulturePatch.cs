using System;
using System.Linq;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(CraftingHelper), "OpenCrafting")]
internal static class HomesteadForgeCulturePatch
{
	[HarmonyPrefix]
	private static bool Prefix(CraftingTemplate craftingTemplate, CraftingState oldState)
	{
		if (Settlement.CurrentSettlement != null)
		{
			return true;
		}
		try
		{
			CultureObject cultureObject = Hero.MainHero?.Culture ?? Settlement.All.FirstOrDefault()?.Culture;
			if (cultureObject == null)
			{
				return true;
			}
			TextObject textObject = new TextObject("{=homestead_forge_crafted}Homestead Crafted {CURR_TEMPLATE_NAME}");
			textObject.SetTextVariable("CURR_TEMPLATE_NAME", craftingTemplate.TemplateName);
			Crafting crafting = new Crafting(craftingTemplate, cultureObject, textObject);
			crafting.Init();
			crafting.ReIndex();
			if (oldState == null)
			{
				CraftingState craftingState = Game.Current.GameStateManager.CreateState<CraftingState>();
				craftingState.InitializeLogic(crafting);
				Game.Current.GameStateManager.PushState(craftingState);
			}
			else
			{
				oldState.InitializeLogic(crafting, isReplacingWeaponClass: true);
			}
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadForgeCulturePatch", "Prefix failed: " + ex.GetType().Name + ": " + ex.Message);
			return true;
		}
	}
}
