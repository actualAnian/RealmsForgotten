using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultTradeItemPriceFactorModel), "GetPrice", new Type[]
{
	typeof(EquipmentElement),
	typeof(MobileParty),
	typeof(PartyBase),
	typeof(bool),
	typeof(float),
	typeof(float),
	typeof(float)
})]
public static class HomesteadTradeDiscountPatch
{
	public static void Postfix(EquipmentElement itemRosterElement, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStoreValue, float supply, float demand, ref int __result)
	{
		if (clientParty != MobileParty.MainParty || HomesteadBehavior.Instance == null || !HomesteadBehavior.Instance.HasMarketLadyTradeDiscountUnlocked)
		{
			return;
		}
		if (isSelling)
		{
			__result = (int)((float)__result * 1.05f);
			return;
		}
		__result = (int)((float)__result * 0.95f);
		if (__result < 1)
		{
			__result = 1;
		}
	}
}
