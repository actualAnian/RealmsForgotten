using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SOTOR.Extensions;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public abstract class AbstractAgentCastingBehavior : IAgentBehavior
{
	private WizardAIComponent _component;

	public Agent Agent;

	protected float Hysteresis = 0.2f;

	public readonly AbilityTemplate AbilityTemplate;

	protected readonly int AbilityIndex;

	private readonly List<Axis> _axisList;

	public Target CurrentTarget = new Target();

	public List<BehaviorOption> LatestScores { get; private set; }

	public AbstractAgentTacticalBehavior TacticalBehavior { get; protected set; }

	public WizardAIComponent Component => _component ?? (_component = Agent.GetComponent<WizardAIComponent>());

	protected AbstractAgentCastingBehavior(Agent agent, AbilityTemplate abilityTemplate, int abilityIndex)
	{
		Agent = agent;
		AbilityIndex = abilityIndex;
		AbilityTemplate = abilityTemplate;
		_axisList = AgentCastingBehaviorConfiguration.UtilityByType[GetType()](this);
		TacticalBehavior = new KeepSafeAgentTacticalBehavior(Agent, Agent.GetComponent<WizardAIComponent>());
	}

	public virtual void Execute()
	{
		Ability ability = Agent.GetAbility(AbilityIndex);
		if (ability == null || ability.IsOnCooldown())
		{
			return;
		}
		// [RF-B] O LOOP TRAVADO.
		//
		// Execute roda a cada frame; a decisao so e revista a cada 3s
		// (WizardAIComponent.EvalInterval). Quando o feitico escolhido nao pode ser
		// pago, TryCast falha, nao inicia cooldown, e o agente reenvia o mesmo
		// pedido dezenas de vezes por segundo ate a proxima revisao — parado, sem
		// tentar nenhuma das outras magias que ele PODE pagar.
		//
		// Medido em log: 184 tentativas bloqueadas por "Not enough Winds of Magic"
		// contra 23 lancamentos bem-sucedidos na mesma batalha. Um unico agente
		// martelou RfCosmicGhost por 2,5s seguidos.
		//
		// Aqui a impossibilidade vira gatilho: pede revisao imediata, e a proxima
		// avaliacao escolhe algo que caiba na mana (CalculateUtility ja devolve 0
		// para o que nao pode ser lancado) ou cai em PreserveWinds e espera quieto.
		if (!ability.CanCast(Agent, out var _))
		{
			Component?.RequestImmediateReevaluation();
			return;
		}
		DefaultBattleMissionAgentSpawnLogic missionBehavior = Mission.Current.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>(); // [RF-A] 1.4.7: classe concreta renomeada
		if (missionBehavior == null || Traverse.Create(missionBehavior).Field("_spawningReinforcements").GetValue() as bool? != true)
		{
			CurrentTarget = UpdateTarget(CurrentTarget);
			if (HaveLineOfSightToTarget(CurrentTarget))
			{
				Agent.SelectAbility(AbilityIndex);
				CastSpellAtCurrentTarget();
			}
		}
	}

	public virtual void Terminate()
	{
	}

	protected virtual Target UpdateTarget(Target target)
	{
		return target;
	}

	protected virtual bool HaveLineOfSightToTarget(Target target)
	{
		return IsTargetWithinAbilityRange(target);
	}

	protected bool IsTargetWithinAbilityRange(Target target)
	{
		Vec3 positionPrioritizeCalculated = target.GetPositionPrioritizeCalculated();
		if (positionPrioritizeCalculated == Vec3.Invalid)
		{
			return false;
		}
		// [RF-B] o original so testava o teto de distancia. Sem piso, a IA lancava
		// feitico de AREA a queima-roupa e se pegava na propria explosao. O piso e o
		// maior entre MinDistance do template e o raio + 1m de folga.
		float distance = Agent.Position.Distance(positionPrioritizeCalculated);
		float safeMin = Math.Max(AbilityTemplate.MinDistance, AbilityTemplate.Radius + 1f);
		if (distance < safeMin)
		{
			return false;
		}
		return distance <= AbilityTemplate.MaxDistance;
	}

	protected virtual void CastSpellAtCurrentTarget()
	{
		// [RF-B] Entrega a mira da IA antes do lancamento. Sem isto, GetSpawnFrame
		// nao tem para onde apontar (o Crosshair so existe para o jogador) e todo
		// feitico de area detona em cima do proprio conjurador. Ver
		// Ability.SetAiAimPosition.
		Ability ability = Agent.GetAbility(AbilityIndex);
		if (ability != null)
		{
			ability.SetAiAimPosition(CurrentTarget.GetPositionPrioritizeCalculated());
		}
		Agent.TryCastCurrentAbility(out var _);
	}

	protected Vec3 ComputeSpellAngleVelocityCorrection(Vec3 targetPosition, Vec3 targetVelocity)
	{
		AbilityEffectType abilityEffectType = AbilityTemplate.AbilityEffectType;
		float num = ((abilityEffectType != AbilityEffectType.Vortex && abilityEffectType != AbilityEffectType.Heal && abilityEffectType != AbilityEffectType.Augment && abilityEffectType != AbilityEffectType.Hex && abilityEffectType != AbilityEffectType.Bombardment) ? ((AbilityTemplate.BaseMovementSpeed != 0f) ? (targetPosition.Distance(Agent.Position) / AbilityTemplate.BaseMovementSpeed) : AbilityTemplate.CastTime) : AbilityTemplate.CastTime);
		return targetVelocity * num;
	}

	public virtual List<BehaviorOption> CalculateUtility()
	{
		LatestScores = AgentCastingBehaviorConfiguration.FindTargets(Agent, AbilityTemplate).Select(delegate(Target target)
		{
			target.UtilityValue = CalculateUtility(target);
			return new BehaviorOption
			{
				Target = target,
				Behavior = this,
				UtilityValue = target.UtilityValue
			};
		}).ToList();
		return LatestScores;
	}

	protected virtual float CalculateUtility(Target target)
	{
		Ability ability = Agent.GetAbility(AbilityIndex);
		if (ability == null || ability.IsOnCooldown() || !ability.CanCast(Agent, out var _) || (target.Formation == null && target.TacticalPosition == null))
		{
			return 0f;
		}
		float num = ((Component != null && Component.CurrentCastingBehavior == this && target.Formation == CurrentTarget.Formation) ? Hysteresis : 0f);
		return _axisList.GeometricMean(target) + num;
	}

	public void SetCurrentTarget(Target target)
	{
		CurrentTarget = target;
	}
}
