using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(CraftingOrderPopupVM), "RefreshOrders")]
internal static class HomesteadCraftingOrderCrashPatch
{
	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception)
	{
		if (__exception == null)
		{
			return null;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement != null && currentSettlement.StringId != null && currentSettlement.StringId.StartsWith("hsr_settlement_"))
		{
			TraceLogger.Write("HomesteadCraftingOrderCrashPatch", "Swallowed CraftingOrderPopupVM.RefreshOrders error in '" + currentSettlement.StringId + "': " + __exception.Message);
			return null;
		}
		return __exception;
	}
}
