using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SOTOR.AbilitySystem;

public static class SotorMindControlHelper
{
	public const float BaseChance = 0.5f;

	public const float PerSocialSkillRate = 0.000625f;

	public const float GateEffectiveness = 1.15f;

	public const float PerLevelPenalty = 0.02f;

	public const float MinChance = 0.05f;

	public static float GetBaseChance(Hero caster)
	{
		if (caster == null)
		{
			return 0.5f;
		}
		int skillValue = caster.GetSkillValue(DefaultSkills.Charm);
		int skillValue2 = caster.GetSkillValue(DefaultSkills.Leadership);
		int skillValue3 = caster.GetSkillValue(DefaultSkills.Roguery);
		float num = (float)(skillValue + skillValue2 + skillValue3) * 0.000625f;
		float num2 = SotorSpellcraftHelper.GetSpellDamageFactor(caster) / 1.15f;
		float num3 = (0.5f + num) * num2;
		if (!(num3 < 0.05f))
		{
			return num3;
		}
		return 0.05f;
	}

	public static float GetTargetChance(Hero caster, int casterLevel, int enemyLevel, float enemyHpFraction)
	{
		float num = GetBaseChance(caster) - (float)(enemyLevel - casterLevel) * 0.02f * enemyHpFraction;
		if (!(num < 0.05f))
		{
			return num;
		}
		return 0.05f;
	}
}
