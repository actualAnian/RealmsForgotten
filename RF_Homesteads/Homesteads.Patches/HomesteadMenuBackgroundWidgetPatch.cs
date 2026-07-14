using System.Reflection;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace Homesteads.Patches;

[HarmonyPatch]
internal static class HomesteadMenuBackgroundWidgetPatch
{
	private static Sprite? _customSprite;

	private static bool _hasLoggedForceApply;

	private static MethodBase? TargetMethod()
	{
		if (!(HomesteadMenuBackgroundPatch.GameMenuWidgetType == null))
		{
			return AccessTools.Method(HomesteadMenuBackgroundPatch.GameMenuWidgetType, "OnLateUpdate");
		}
		return null;
	}

	[HarmonyPostfix]
	private static void Postfix(object __instance)
	{
		if (!(__instance is Widget obj))
		{
			return;
		}
		string text = HomesteadMenuBackgroundPatch.GameMenuWidgetMenuIdProperty?.GetValue(__instance) as string;
		if (!HomesteadMenuBackgroundPatch.IsHomesteadMenu(text))
		{
			return;
		}
		if (_customSprite == null)
		{
			_customSprite = UIResourceManager.SpriteData.GetSprite("homestead_background");
		}
		if (_customSprite != null)
		{
			HomesteadMenuBackgroundPatch.GameMenuWidgetSpriteNameProperty?.SetValue(__instance, "homestead_background");
			HomesteadMenuBackgroundPatch.GameMenuWidgetOverriddenSpriteMapBrushProperty?.SetValue(__instance, null);
			HomesteadMenuBackgroundPatch.WidgetSpriteProperty?.SetValue(obj, _customSprite);
			if (!_hasLoggedForceApply)
			{
				_hasLoggedForceApply = true;
				TraceLogger.Write("HomesteadMenuBackgroundWidgetPatch", "Forced sprite-backed background on GameMenuWidget for '" + text + "'");
			}
		}
	}
}
