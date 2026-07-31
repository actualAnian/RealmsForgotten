using System;
using HarmonyLib;
using SandBox.GameComponents;
using TaleWorlds.MountAndBlade;

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
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorAgentSpeedPatch failed: " + ex.Message);
		}
	}
}
