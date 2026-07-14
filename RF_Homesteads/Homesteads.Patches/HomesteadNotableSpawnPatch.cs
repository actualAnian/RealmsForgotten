using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(DefaultNotableSpawnModel), "GetTargetNotableCountForSettlement")]
public class HomesteadNotableSpawnPatch
{
	public static void Postfix(Settlement settlement, Occupation occupation, ref int __result)
	{
		if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_village_") && occupation == Occupation.RuralNotable && __result > 2)
		{
			__result = 2;
		}
	}
}
