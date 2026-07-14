using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MobileParty), "Banner", MethodType.Getter)]
public static class HomesteadPartyBannerPatch
{
	public static void Postfix(MobileParty __instance, ref Banner __result)
	{
		try
		{
			if (__instance.Party?.CustomBanner != null || __instance.PartyComponent?.GetDefaultComponentBanner() != null)
			{
				return;
			}
			Settlement settlement = __instance.HomeSettlement ?? __instance.CurrentSettlement;
			if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_"))
			{
				Banner banner = settlement.OwnerClan?.Banner;
				if (banner != null)
				{
					__result = banner;
				}
			}
		}
		catch
		{
		}
	}
}
