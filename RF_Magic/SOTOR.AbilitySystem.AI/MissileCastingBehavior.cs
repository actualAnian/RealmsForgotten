using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class MissileCastingBehavior : AbstractAgentCastingBehavior
{
	public MissileCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex)
		: base(agent, template, abilityIndex)
	{
		Hysteresis = 0.1f;
	}

	protected override Target UpdateTarget(Target target)
	{
		Formation formation = CurrentTarget.Formation;
		if (formation == null || formation.CountOfUnitsWithoutDetachedOnes < 1)
		{
			return target;
		}
		if (formation.CountOfUnitsWithoutDetachedOnes > 10)
		{
			Agent randomAgent = CommonAIFunctions.GetRandomAgent(formation);
			if (randomAgent != null)
			{
				target.Agent = randomAgent;
				Vec3 selectedWorldPosition = randomAgent.Position + ComputeSpellAngleVelocityCorrection(randomAgent.Position, randomAgent.Velocity);
				target.SelectedWorldPosition = selectedWorldPosition;
			}
		}
		else
		{
			Agent medianAgent = formation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: false, formation.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false));
			if (medianAgent != null)
			{
				target.Agent = medianAgent;
				// [RF-B] Este ramo so trocava o Agent e deixava SelectedWorldPosition
				// como estava. Como GetPositionPrioritizeCalculated PREFERE
				// SelectedWorldPosition, uma formacao que encolheu para 10 unidades ou
				// menos continuava sendo mirada na posicao antiga — de outro alvo, de
				// segundos atras. Enquanto a rotacao do projetil era ignorada isso nao
				// aparecia; agora que a mira e obedecida, apareceria como tiro no vazio.
				target.SelectedWorldPosition = medianAgent.Position
					+ ComputeSpellAngleVelocityCorrection(medianAgent.Position, medianAgent.Velocity);
			}
		}
		return target;
	}

	protected override bool HaveLineOfSightToTarget(Target target)
	{
		Vec3 positionPrioritizeCalculated = target.GetPositionPrioritizeCalculated();
		if (positionPrioritizeCalculated == Vec3.Invalid)
		{
			return false;
		}
		positionPrioritizeCalculated.z += 0.75f;
		Vec3 closestPoint = Agent.Position;
		float num = closestPoint.Distance(positionPrioritizeCalculated);
		if (num < AbilityTemplate.MinDistance || num > AbilityTemplate.MaxDistance)
		{
			return false;
		}
		Vec3 vec = Agent.Position + new Vec3(0f, 0f, Agent.GetEyeGlobalHeight());
		float collisionDistance = 0f;
		Agent agent;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			agent = Mission.Current.RayCastForClosestAgent(vec, positionPrioritizeCalculated, Agent.Index, 0.25f, out var _);
			Mission.Current.Scene.RayCastForClosestEntityOrTerrain(vec, positionPrioritizeCalculated, out collisionDistance, out closestPoint, out var _, 0.25f);
		}
		// [RF-A] artefato ILSpy: o decompilador emitiu um unico `if` de cinco termos
		// misturando guarda de queima-roupa com guarda de fogo amigo, o que torna a
		// intencao ilegivel e ja me levou a reescrever ERRADO uma vez (a inversao
		// De Morgan passou a PERMITIR o tiro justamente quando ha aliado perto do
		// alvo). Reescrito como duas recusas explicitas, cada uma dizendo o que
		// impede o disparo.

		// 1) Aliado na linha de tiro, perto do alvo.
		if (agent != null && !agent.IsEnemyOf(Agent)
			&& agent.GetChestGlobalPosition().Distance(positionPrioritizeCalculated) < 4f)
		{
			return false;
		}

		// 2) O raio bateu em algo ANTES do alvo (parede, terreno): o feitico
		//    explodiria no obstaculo. Tolerancia de 0.3m para o proprio alvo.
		if (!float.IsNaN(collisionDistance)
			&& Math.Abs(collisionDistance - positionPrioritizeCalculated.Distance(vec)) >= 0.3f)
		{
			return false;
		}

		return true;
	}
}
