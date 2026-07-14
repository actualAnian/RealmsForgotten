using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(Clan), "get_CompanionLimit")]
internal static class HomesteadCompanionLimitPatch
{
	[HarmonyPostfix]
	private static void Postfix(Clan __instance, ref int __result)
	{
		if (__instance != null && __instance == Clan.PlayerClan)
		{
			int num = HomesteadBehavior.Instance?.AmbassadorCompanionSlotBonus ?? 0;
			if (num > 0)
			{
				__result += num;
			}
		}
	}
}
