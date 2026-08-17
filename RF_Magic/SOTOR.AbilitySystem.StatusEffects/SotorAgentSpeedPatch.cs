using System;
using HarmonyLib;
using SandBox.GameComponents;
using TaleWorlds.MountAndBlade;
using SOTOR.MagicAccessories;

namespace SOTOR.AbilitySystem.StatusEffects;

[HarmonyPatch(typeof(SandboxAgentStatCalculateModel), "UpdateAgentStats")]
public static class SotorAgentSpeedPatch
{
	public static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		try
		{
			if (agent == null || agentDrivenProperties == null || !agent.IsActive())
			{
				return;
			}
			StatusEffectComponent component = agent.GetComponent<StatusEffectComponent>();
			if (component != null)
			{
				float movementSpeedModifier = component.GetMovementSpeedModifier();
				float attackSpeedModifier = component.GetAttackSpeedModifier();
				if (movementSpeedModifier != 0f)
				{
					float num = Math.Max(0f, 1f + movementSpeedModifier);
					agentDrivenProperties.MaxSpeedMultiplier *= num;
					agentDrivenProperties.CombatMaxSpeedMultiplier *= num;
				}
				if (attackSpeedModifier != 0f)
				{
					float num2 = Math.Max(0f, 1f + attackSpeedModifier);
					agentDrivenProperties.SwingSpeedMultiplier *= num2;
					agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= num2;
					agentDrivenProperties.ReloadSpeed *= num2;
				}
			}
			if (MagicRuneService.TryGetForWieldedWeapon(agent, out MagicRuneData rune))
			{
				float primary = Math.Max(0f, rune.PrimaryValue) / 100f;
				float secondary = Math.Max(0f, rune.SecondaryValue) / 100f;
				switch (rune.Effect)
				{
				case MagicRuneEffect.KeenEdge:
					agentDrivenProperties.SwingSpeedMultiplier *= 1f + secondary;
					agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= 1f + secondary;
					break;
				case MagicRuneEffect.Precision:
					agentDrivenProperties.WeaponInaccuracy *= 1f - primary;
					break;
				case MagicRuneEffect.Wind:
					agentDrivenProperties.MissileSpeedMultiplier *= 1f + primary;
					agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= 1f + secondary;
					break;
				case MagicRuneEffect.Windlass:
					agentDrivenProperties.ReloadSpeed *= 1f + primary;
					break;
				case MagicRuneEffect.FarSight:
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 1f - primary;
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 1f - primary;
					break;
				case MagicRuneEffect.Lightness:
					agentDrivenProperties.WeaponsEncumbrance *= 1f - primary;
					agentDrivenProperties.HandlingMultiplier *= 1f + secondary;
					break;
				}
			}
			float frostMultiplier = MagicRuneMissionLogic.GetFrostMultiplier(agent);
			if (frostMultiplier < 1f)
			{
				agentDrivenProperties.MaxSpeedMultiplier *= frostMultiplier;
				agentDrivenProperties.CombatMaxSpeedMultiplier *= frostMultiplier;
				agentDrivenProperties.SwingSpeedMultiplier *= frostMultiplier;
				agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= frostMultiplier;
				agentDrivenProperties.ReloadSpeed *= frostMultiplier;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorAgentSpeedPatch failed: " + ex.Message);
		}
	}
}
