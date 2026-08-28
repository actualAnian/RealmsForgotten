using System;
using HarmonyLib;
using SandBox.GameComponents;
using SOTOR.AbilitySystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.MagicAccessories;

public static class MagicRuneCombatPatches
{
	public static float ApplyWeaponDamage(float damage, in AttackInformation attack)
	{
		if (damage <= 0f || attack.AttackerAgent == null || attack.VictimAgent == null)
		{
			return damage;
		}
		if (MagicRuneService.TryGetForWeapon(attack.AttackerAgent, in attack.AttackerWeapon, out MagicRuneData rune))
		{
			float factor = 1f;
			switch (rune.Effect)
			{
			case MagicRuneEffect.KeenEdge:
			case MagicRuneEffect.Piercing:
				factor += rune.PrimaryValue / 100f;
				break;
			case MagicRuneEffect.Executioner:
				if (attack.VictimAgent.HealthLimit > 0f && attack.VictimAgent.Health / attack.VictimAgent.HealthLimit <= rune.SecondaryValue / 100f)
				{
					factor += rune.PrimaryValue / 100f;
				}
				break;
			case MagicRuneEffect.Huntsman:
				if (attack.VictimAgent.IsMount || !attack.VictimAgent.IsHuman)
				{
					factor += rune.PrimaryValue / 100f;
				}
				break;
			}
			damage *= factor;
			if (factor > 1f)
			{
				MagicRuneCombatFeedback.RecordWeaponModifier(attack.AttackerAgent, attack.VictimAgent, rune);
			}
		}
		if (SotorDamageHelper.InSpellBlow)
		{
			MagicRuneService.HasEffect(attack.VictimAgent, MagicRuneEffect.Mirror, out MagicRuneData mirror);
			damage = MagicPowerRingCombat.ApplyMagicMirrorDamage(damage, in attack, mirror);
		}
		return damage;
	}
}

[HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "CalculateShieldDamage")]
public static class MagicRuneShieldDamagePatch
{
	public static void Postfix(ref float __result, in AttackInformation attackInformation)
	{
		try
		{
			if (MagicRuneService.TryGetForWeapon(attackInformation.AttackerAgent, in attackInformation.AttackerWeapon, out MagicRuneData attackRune) &&
				attackRune.Effect == MagicRuneEffect.Sundering)
			{
				__result *= 1f + attackRune.PrimaryValue / 100f;
				MagicRuneCombatFeedback.Report(attackInformation.AttackerAgent, MagicRuneEffect.Sundering, "shield_damage",
					$"{attackRune.Name}: shield-damage calculation: {__result:0} (+{attackRune.PrimaryValue:0}%)", Colors.Green);
			}
			if (MagicRuneService.TryGetForWeapon(attackInformation.VictimAgent, in attackInformation.VictimShield, out MagicRuneData shieldRune) &&
				shieldRune.Effect == MagicRuneEffect.Bulwark)
			{
				__result *= Math.Max(0f, 1f - shieldRune.PrimaryValue / 100f);
				MagicRuneCombatFeedback.Report(attackInformation.VictimAgent, MagicRuneEffect.Bulwark, "shield_protection",
					$"{shieldRune.Name}: shield-damage calculation: {__result:0} (-{shieldRune.PrimaryValue:0}%)", Colors.Cyan);
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MagicRuneShieldDamagePatch failed: " + ex.Message);
		}
	}
}

[HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "DecideCrushedThrough")]
public static class MagicRuneCrushThroughPatch
{
	public static void Postfix(ref bool __result, Agent attackerAgent)
	{
		if (!__result && MagicRuneService.TryGetForWieldedWeapon(attackerAgent, out MagicRuneData rune) &&
			rune.Effect == MagicRuneEffect.Sundering && MBRandom.RandomFloat * 100f < rune.SecondaryValue)
		{
			__result = true;
			MagicRuneCombatFeedback.Report(attackerAgent, MagicRuneEffect.Sundering, "crush_through",
				$"{rune.Name}: crush through", Colors.Green);
		}
	}
}

[HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "CanWeaponKnockback")]
public static class MagicRuneKnockbackPatch
{
	public static void Postfix(ref bool __result, Agent attackerAgent)
	{
		if (!__result && MagicRuneService.TryGetForWieldedWeapon(attackerAgent, out MagicRuneData rune) &&
			rune.Effect == MagicRuneEffect.Impact && MBRandom.RandomFloat * 100f < rune.PrimaryValue)
		{
			__result = true;
			MagicRuneCombatFeedback.Report(attackerAgent, MagicRuneEffect.Impact, "knockback",
				$"{rune.Name}: stagger", Colors.Green);
		}
	}
}

[HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "CanWeaponKnockDown")]
public static class MagicRuneKnockDownPatch
{
	public static void Postfix(ref bool __result, Agent attackerAgent)
	{
		if (!__result && MagicRuneService.TryGetForWieldedWeapon(attackerAgent, out MagicRuneData rune) &&
			rune.Effect == MagicRuneEffect.Impact && MBRandom.RandomFloat * 100f < rune.SecondaryValue)
		{
			__result = true;
			MagicRuneCombatFeedback.Report(attackerAgent, MagicRuneEffect.Impact, "knockdown",
				$"{rune.Name}: knockdown", Colors.Green);
		}
	}
}
