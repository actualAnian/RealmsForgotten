using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(DefaultPartyWageModel), "GetCharacterWage")]
public static class SotorSkeletonWagePatch
{
	private static void Postfix(CharacterObject character, ref int __result)
	{
		if (__result != 0 && SkeletonUpkeep.IsSkeletonChar(character))
		{
			__result = 0;
		}
	}
}
