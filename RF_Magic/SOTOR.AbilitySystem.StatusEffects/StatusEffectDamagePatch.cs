using System;
using HarmonyLib;
using SOTOR.Extensions;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.StatusEffects;

[HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "ApplyGeneralDamageModifiers")]
public static class StatusEffectDamagePatch
{
	public static void Postfix(ref float __result, in AttackInformation attackInformation, in AttackCollisionData collisionData)
	{
		try
		{
			float num = __result;
			if (num <= 0f)
			{
				return;
			}
			Agent attackerAgent = attackInformation.AttackerAgent;
			Agent victimAgent = attackInformation.VictimAgent;
			if (attackerAgent == null || victimAgent == null || attackerAgent == victimAgent)
			{
				return;
			}
			MissionWeapon attackerWeapon = attackInformation.AttackerWeapon;
			if (!attackerWeapon.IsEmpty && attackerWeapon.Item?.StringId == "sotor_amber_javelin")
			{
				float spellcraftDamageFactorFor = SotorDamageHelper.GetSpellcraftDamageFactorFor(attackerAgent);
				if (spellcraftDamageFactorFor != 1f)
				{
					__result *= spellcraftDamageFactorFor;
				}
				if (__result > 0f && victimAgent.IsEnemyOf(attackerAgent))
				{
					Hero hero = attackerAgent.GetHero();
					if (hero != null)
					{
						SotorSpellcraftHelper.GrantAbilityOutcomeXp(hero, (int)__result / 5, singleTarget: true);
					}
				}
				return;
			}
			AttackTypeMask attackTypeMask = SotorResistanceHelper.ChannelForWeapon(collisionData.IsMissile);
			float amp;
			float resist;
			float ward;
			float damageFactor = SotorResistanceHelper.GetDamageFactor(attackerAgent, victimAgent, attackTypeMask, DamageType.Physical, out amp, out resist, out ward);
			if (damageFactor == 1f)
			{
				return;
			}
			__result = num * damageFactor;
			SotorLog.Debug($"StatusEffect damage mod: '{attackerAgent.Name}' -> '{victimAgent.Name}' ({attackTypeMask}) " + $"amp={amp:0.00} resist={resist:0.00} ward={ward:0.00} | {num:0.0} -> {__result:0.0}");
			if (collisionData.IsMissile)
			{
				return;
			}
			float num2 = victimAgent.GetComponent<StatusEffectComponent>()?.GetThorns() ?? 0f;
			if (num2 > 0f && __result > 0f)
			{
				int num3 = (int)(__result * num2);
				if (num3 > 0)
				{
					SotorDamageHelper.ApplyReflectedDamage(attackerAgent, num3, victimAgent);
					SotorLog.Debug($"Fire-Cloak thorns: '{victimAgent.Name}' reflects {num2:0.00}x -> {num3} Fire to '{attackerAgent.Name}'.");
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("StatusEffectDamagePatch.Postfix failed: " + ex.Message);
		}
	}
}
