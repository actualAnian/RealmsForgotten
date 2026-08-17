using HarmonyLib;
using SOTOR.AbilitySystem;
using TaleWorlds.Core;

namespace SOTOR.RFIntegration;

/// <summary>
/// Mantem cajados e varinhas como polearms para animacao e combate, mas liga o
/// requisito <c>difficulty</c> deles a Arcane. A lista de focos vem do mesmo
/// registro usado pelo sistema de magia; polearms comuns nao sao afetadas.
/// </summary>
[HarmonyPatch(typeof(ItemObject), nameof(ItemObject.RelevantSkill), MethodType.Getter)]
public static class ArcaneFocusDifficultyPatch
{
	public static void Postfix(ItemObject __instance, ref SkillObject __result)
	{
		if (__instance == null ||
			!ArcaneFocusRegistry.TryGetFocus(__instance.StringId, out ArcaneFocusData _))
		{
			return;
		}

		SkillObject arcane = SotorSkills.Spellcraft;
		if (arcane != null)
		{
			__result = arcane;
		}
	}
}
