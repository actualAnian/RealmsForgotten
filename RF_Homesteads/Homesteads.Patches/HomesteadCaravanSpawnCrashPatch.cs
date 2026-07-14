using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultCaravanModel), "CanHeroCreateCaravan")]
internal static class HomesteadCaravanSpawnCrashPatch
{
	[HarmonyPostfix]
	private static void Postfix(Hero hero, ref bool __result)
	{
		if (__result && hero != null)
		{
			Settlement currentSettlement = hero.CurrentSettlement;
			string text = currentSettlement?.StringId;
			bool flag = text?.StartsWith("hsr_settlement_") ?? false;
			if (text == null || (flag && !currentSettlement.HasPort))
			{
				__result = false;
				TraceLogger.Write("HomesteadCaravanSpawnCrashPatch", string.Format("Blocked caravan for '{0}' ({1}) — settlement is '{2}' hasPort={3}.", hero.StringId, hero.Name, text ?? "null", currentSettlement?.HasPort ?? false));
			}
		}
	}
}
