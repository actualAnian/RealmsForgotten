using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(CraftingOrderPopupVM), "RefreshOrders")]
internal static class HomesteadForgeOrdersPatch
{
	[HarmonyPrefix]
	private static bool Prefix(CraftingOrderPopupVM __instance)
	{
		if (Settlement.CurrentSettlement?.Town != null)
		{
			return true;
		}
		try
		{
			__instance.CraftingOrders?.Clear();
			if (!HomesteadForgeContext.IsActive || __instance.CraftingOrders == null)
			{
				return false;
			}
			List<CraftingOrder> list = (from o in HomesteadForgeContext.ActiveOrders()
				orderby o.OrderDifficulty
				select o).ToList();
			if (list.Count == 0)
			{
				return false;
			}
			Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero = (Func<CraftingAvailableHeroItemVM>)(AccessTools.Field(typeof(CraftingOrderPopupVM), "_getCurrentCraftingHero")?.GetValue(__instance));
			Func<CraftingOrder, IEnumerable<CraftingStatData>> func = (Func<CraftingOrder, IEnumerable<CraftingStatData>>)(AccessTools.Field(typeof(CraftingOrderPopupVM), "_getOrderStatDatas")?.GetValue(__instance));
			Action<CraftingOrderItemVM> onSelection = __instance.SelectOrder;
			foreach (CraftingOrder item in list)
			{
				List<CraftingStatData> orderStatDatas = ((func != null) ? func(item).ToList() : new List<CraftingStatData>());
				__instance.CraftingOrders.Add(new CraftingOrderItemVM(item, onSelection, getCurrentCraftingHero, orderStatDatas));
			}
			TextObject textObject = new TextObject("{=MkVTRqAw}Orders ({ORDER_COUNT})");
			textObject.SetTextVariable("ORDER_COUNT", __instance.CraftingOrders.Count);
			__instance.OrderCountText = textObject.ToString();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadForgeOrdersPatch", "Prefix failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		return false;
	}
}
