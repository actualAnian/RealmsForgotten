using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Homesteads.Patches;

internal static class HomesteadMenuBackgroundPatch
{
	private const string CustomBackgroundSprite = "homestead_background";

	internal static readonly Type? GameMenuWidgetType = AccessTools.TypeByName("GameMenuWidget");

	internal static readonly PropertyInfo? GameMenuWidgetMenuIdProperty = ((GameMenuWidgetType == null) ? null : AccessTools.Property(GameMenuWidgetType, "MenuId"));

	internal static readonly PropertyInfo? GameMenuWidgetSpriteNameProperty = ((GameMenuWidgetType == null) ? null : AccessTools.Property(GameMenuWidgetType, "SpriteName"));

	internal static readonly PropertyInfo? GameMenuWidgetOverriddenSpriteMapBrushProperty = ((GameMenuWidgetType == null) ? null : AccessTools.Property(GameMenuWidgetType, "OverriddenSpriteMapBrush"));

	internal static readonly PropertyInfo? WidgetSpriteProperty = AccessTools.Property(typeof(Widget), "Sprite");

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameMenuVM), MethodType.Constructor, new Type[] { typeof(MenuContext) })]
	private static void ConstructorPostfix(GameMenuVM __instance)
	{
		ApplyCustomBackground(__instance, "constructor");
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameMenuVM), "UpdateMenuContext")]
	private static void UpdateMenuContextPostfix(GameMenuVM __instance)
	{
		ApplyCustomBackground(__instance, "update");
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameMenuVM), "RefreshValues")]
	private static void RefreshValuesPostfix(GameMenuVM __instance)
	{
		ApplyCustomBackground(__instance, "refresh");
	}

	private static void ApplyCustomBackground(GameMenuVM gameMenuVm, string reason)
	{
		string text = gameMenuVm.MenuId ?? gameMenuVm.MenuContext?.GameMenu?.StringId;
		if (IsHomesteadMenu(text) && (!(gameMenuVm.Background == "homestead_background") || !(gameMenuVm.BackgroundCopy == "homestead_background")))
		{
			gameMenuVm.Background = "homestead_background";
			gameMenuVm.BackgroundCopy = "homestead_background";
			TraceLogger.Write("HomesteadMenuBackgroundPatch", "Applied custom menu background for '" + text + "' via " + reason);
		}
	}

	internal static bool IsHomesteadMenu(string? menuId)
	{
		if (!(menuId == "homestead_menu_main") && !(menuId == "homestead_menu_manage_main"))
		{
			return menuId == "homestead_menu_wait_waiting";
		}
		return true;
	}
}
