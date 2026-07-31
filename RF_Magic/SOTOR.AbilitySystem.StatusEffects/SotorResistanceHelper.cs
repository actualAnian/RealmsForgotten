using System;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.StatusEffects;

public static class SotorResistanceHelper
{
	public static AttackTypeMask ChannelForWeapon(bool isMissile)
	{
		if (!isMissile)
		{
			return AttackTypeMask.Melee;
		}
		return AttackTypeMask.Ranged;
	}

	public static float GetDamageFactor(Agent attacker, Agent victim, AttackTypeMask channel, DamageType resistDamageType, out float amp, out float resist, out float ward)
	{
		amp = 0f;
		resist = 0f;
		ward = 0f;
		StatusEffectComponent statusEffectComponent = attacker?.GetComponent<StatusEffectComponent>();
		if (statusEffectComponent != null)
		{
			amp = SumSlots(statusEffectComponent.GetAmplifiers(channel));
		}
		StatusEffectComponent statusEffectComponent2 = victim?.GetComponent<StatusEffectComponent>();
		if (statusEffectComponent2 != null)
		{
			float[] resistances = statusEffectComponent2.GetResistances(channel);
			if (resistances != null)
			{
				if (resistDamageType >= DamageType.Invalid && (int)resistDamageType < resistances.Length)
				{
					resist = resistances[(int)resistDamageType];
				}
				int num = 7;
				if (num >= 0 && num < resistances.Length)
				{
					ward = resistances[num];
				}
			}
		}
		if (amp == 0f && resist == 0f && ward == 0f)
		{
			return 1f;
		}
		float num2 = (1f + amp) * (1f - Math.Min(resist, 1f)) * (1f - Math.Min(ward, 1f));
		if (!(num2 < 0f))
		{
			return num2;
		}
		return 0f;
	}

	private static float SumSlots(float[] arr)
	{
		if (arr == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < arr.Length; i++)
		{
			num += arr[i];
		}
		return num;
	}
}
