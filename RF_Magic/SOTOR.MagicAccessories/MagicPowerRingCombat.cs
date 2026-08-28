using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SandBox.GameComponents;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.MagicAccessories;

public static class MagicPowerRingCombat
{
	private sealed class WinterSpeedState
	{
		public bool IsMount;
		public float BasePrimary;
		public float AppliedPrimary;
		public float BaseCombat;
		public float AppliedCombat;
	}

	private static bool _reflectingMirrorDamage;
	private static readonly Dictionary<Agent, WinterSpeedState> _winterSpeedStates = new Dictionary<Agent, WinterSpeedState>();

	public static void ClearWinterStates()
	{
		_winterSpeedStates.Clear();
	}

	public static void TryPatchLegacyFireTick(Harmony harmony)
	{
		try
		{
			Type type = AccessTools.TypeByName("RealmsForgotten.RFEffects.MagicEffectsBehavior");
			MethodInfo fireTick = AccessTools.Method(type, "FireTick");
			if (fireTick != null)
			{
				harmony.Patch(fireTick, transpiler: new HarmonyMethod(typeof(MagicPowerRingCombat), nameof(TranspileLegacyFireTick)));
			}
			Type damageModelType = AccessTools.TypeByName("RealmsForgotten.Models.RFAgentApplyDamageModel");
			MethodInfo applyDamage = AccessTools.Method(damageModelType, "ApplyGeneralDamageModifiers");
			if (applyDamage != null)
			{
				harmony.Patch(applyDamage, postfix: new HarmonyMethod(typeof(MagicPowerRingCombat), nameof(ApplyLegacyFireDamagePostfix)));
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("Legacy fire-ring immunity patch skipped: " + ex.Message);
		}
	}

	private static void ApplyLegacyFireDamagePostfix(ref float __result, in AttackInformation attackInformation)
	{
		if (__result > 0f && IsLegacyAnoritFire(in attackInformation) &&
			MagicAccessoryService.GetEquippedPowerRing(attackInformation.VictimAgent?.GetHero(), MagicPowerRingEffect.Fire) != null)
		{
			__result = 0f;
		}
	}

	private static IEnumerable<CodeInstruction> TranspileLegacyFireTick(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo replacement = AccessTools.Method(typeof(MagicPowerRingCombat), nameof(RegisterLegacyFireBlow));
		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(Agent) && method.Name == nameof(Agent.RegisterBlow))
			{
				instruction.opcode = OpCodes.Call;
				instruction.operand = replacement;
			}
			yield return instruction;
		}
	}

	private static void RegisterLegacyFireBlow(Agent victim, Blow blow, ref AttackCollisionData collisionData)
	{
		if (MagicAccessoryService.GetEquippedPowerRing(victim?.GetHero(), MagicPowerRingEffect.Fire) == null)
		{
			victim.RegisterBlow(blow, in collisionData);
		}
	}

	private static bool IsLegacyAnoritFire(in AttackInformation attackInformation)
	{
		string itemId = attackInformation.AttackerWeapon.Item?.StringId;
		return itemId != null && itemId.IndexOf("anorit_fire", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static float ApplyMagicMirrorDamage(float damage, in AttackInformation attack, MagicRuneData rune)
	{
		if (_reflectingMirrorDamage || damage <= 0f || attack.AttackerAgent == null || attack.VictimAgent == null)
		{
			return damage;
		}

		MagicAccessoryData ring = MagicAccessoryService.GetEquippedPowerRing(attack.VictimAgent.GetHero(), MagicPowerRingEffect.Mirror);
		if ((rune == null || rune.Effect != MagicRuneEffect.Mirror) && ring == null)
		{
			return damage;
		}

		bool useRing = ring != null && (rune == null || ring.PrimaryValue > rune.PrimaryValue ||
			(ring.PrimaryValue == rune.PrimaryValue && ring.SecondaryValue > rune.SecondaryValue));
		float reduction = useRing ? ring.PrimaryValue : rune.PrimaryValue;
		float reflection = useRing ? ring.SecondaryValue : rune.SecondaryValue;
		string name = useRing ? ring.Name : rune.Name;
		float before = damage;
		damage *= Math.Max(0f, 1f - reduction / 100f);
		MagicRuneCombatFeedback.Report(attack.VictimAgent, MagicRuneEffect.Mirror, "magic_damage_reduction",
			$"{name}: reduced incoming spell damage calculation by {reduction:0}%", Colors.Cyan);
		int reflected = (int)(before * reflection / 100f);
		if (reflected <= 0)
		{
			return damage;
		}

		try
		{
			_reflectingMirrorDamage = true;
			float actual = SotorDamageHelper.ApplyReflectedDamage(attack.AttackerAgent, reflected, attack.VictimAgent);
			if (actual > 0f)
			{
				MagicRuneCombatFeedback.Report(attack.VictimAgent, MagicRuneEffect.Mirror, "magic_reflection",
					$"{name}: reflected {actual:0} magic damage", Colors.Cyan);
			}
		}
		finally
		{
			_reflectingMirrorDamage = false;
		}
		return damage;
	}

	[HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "ApplyGeneralDamageModifiers")]
	[HarmonyPriority(Priority.Last)]
	private static class EarthRingDamagePatch
	{
		[HarmonyPostfix]
		private static void Postfix(ref float __result, in AttackInformation attackInformation)
		{
			try
			{
				if (__result <= 0f)
				{
					return;
				}
				bool legacyFire = IsLegacyAnoritFire(in attackInformation);
				if (legacyFire &&
					MagicAccessoryService.GetEquippedPowerRing(attackInformation.VictimAgent?.GetHero(), MagicPowerRingEffect.Fire) != null)
				{
					__result = 0f;
					return;
				}
				if (legacyFire || SotorDamageHelper.InSpellBlow ||
					attackInformation.AttackerAgent == attackInformation.VictimAgent ||
					MagicAccessoryService.GetEquippedPowerRing(attackInformation.VictimAgent?.GetHero(), MagicPowerRingEffect.Earth) == null)
				{
					return;
				}
				__result *= 0.75f;
			}
			catch (Exception ex)
			{
				SotorLog.Warn("EarthRingDamagePatch failed: " + ex.Message);
			}
		}
	}

	[HarmonyPatch(typeof(SandboxAgentStatCalculateModel), "UpdateAgentStats")]
	[HarmonyPriority(Priority.Last)]
	private static class WinterRingSpeedPatch
	{
		[HarmonyPrefix]
		private static void Prefix(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			if (agent == null || agentDrivenProperties == null || !_winterSpeedStates.TryGetValue(agent, out WinterSpeedState state))
			{
				return;
			}
			if (state.IsMount)
			{
				if (Math.Abs(agentDrivenProperties.MountSpeed - state.AppliedPrimary) < 0.0001f)
				{
					agentDrivenProperties.MountSpeed = state.BasePrimary;
				}
			}
			else
			{
				if (Math.Abs(agentDrivenProperties.MaxSpeedMultiplier - state.AppliedPrimary) < 0.0001f)
				{
					agentDrivenProperties.MaxSpeedMultiplier = state.BasePrimary;
				}
				if (Math.Abs(agentDrivenProperties.CombatMaxSpeedMultiplier - state.AppliedCombat) < 0.0001f)
				{
					agentDrivenProperties.CombatMaxSpeedMultiplier = state.BaseCombat;
				}
			}
			_winterSpeedStates.Remove(agent);
		}

		[HarmonyPostfix]
		private static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			try
			{
				Mission mission = Mission.Current;
				if (agentDrivenProperties == null || mission == null || agent?.Team == null || Hero.MainHero == null)
				{
					return;
				}

				// Cenas de conversa/barter (ex.: chegada do mensageiro a distancia)
				// spawnam agentes SEM estrutura de times: Team gerenciado existe mas o
				// MBTeam nativo e invalido e IsEnemyOf estoura NRE por dentro — crash
				// no loading da conversa, capturado no debugger do autor (2026-08-19).
				// Anel de Winter e efeito de combate; fora de combate, nada a fazer.
				if (mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
				{
					return;
				}

				// Idioma vanilla: Team pode existir com MBTeam nativo INVALIDO (spawns
				// durante loading, modo ainda StartUp) e IsEnemyOf estoura NRE por
				// dentro. Validar os DOIS lados antes (o proprio vanilla faz
				// `!team.MBTeam.IsValid || !IsEnemyOf(team)`).
				if (mission.PlayerTeam == null || !mission.PlayerTeam.IsValid || !agent.Team.IsValid ||
					!agent.Team.IsEnemyOf(mission.PlayerTeam) ||
					MagicAccessoryService.GetEquippedPowerRing(Hero.MainHero, MagicPowerRingEffect.Winter) == null)
				{
					return;
				}
			WinterSpeedState state = new WinterSpeedState { IsMount = agent.IsMount };
			if (agent.IsMount)
			{
				state.BasePrimary = agentDrivenProperties.MountSpeed;
				agentDrivenProperties.MountSpeed = state.BasePrimary * 0.8f;
				state.AppliedPrimary = agentDrivenProperties.MountSpeed;
			}
				else
				{
					state.BasePrimary = agentDrivenProperties.MaxSpeedMultiplier;
					state.BaseCombat = agentDrivenProperties.CombatMaxSpeedMultiplier;
					agentDrivenProperties.MaxSpeedMultiplier = state.BasePrimary * 0.8f;
					agentDrivenProperties.CombatMaxSpeedMultiplier = state.BaseCombat * 0.8f;
					state.AppliedPrimary = agentDrivenProperties.MaxSpeedMultiplier;
					state.AppliedCombat = agentDrivenProperties.CombatMaxSpeedMultiplier;
				}
				_winterSpeedStates[agent] = state;
			}
			catch (Exception ex)
			{
				// Rede final: efeito de anel jamais derruba um spawn de agente.
				SotorLog.Warn("WinterRingSpeedPatch.Postfix falhou: " + ex.Message);
			}
		}
	}
}
