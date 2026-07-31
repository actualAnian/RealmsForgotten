using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class WizardAIComponent : HumanAIComponent
{
	private const float EvalInterval = 3f;

	private float _dtSinceLastOccasional;

	public AbstractAgentCastingBehavior CurrentCastingBehavior;

	private List<IAgentBehavior> _availableCastingBehaviors;

	public List<IAgentBehavior> AvailableCastingBehaviors => _availableCastingBehaviors ?? (_availableCastingBehaviors = AgentCastingBehaviorConfiguration.PrepareCastingBehaviors(Agent).Cast<IAgentBehavior>().ToList());

	/// <summary>
	/// [RF-B] Antecipa a proxima revisao de decisao. Chamado quando o feitico
	/// escolhido se revela impossivel (mana), para o agente nao passar o resto da
	/// janela de 3s martelando um pedido que nunca vai passar.
	///
	/// Reavaliar e barato perto de ficar parado, mas nao e de graca: o intervalo
	/// minimo evita que um mago sem mana nenhuma re-pontue todo o repertorio a
	/// cada frame.
	/// </summary>
	private const float MinReevalInterval = 0.4f;

	public void RequestImmediateReevaluation()
	{
		if (_dtSinceLastOccasional < EvalInterval - MinReevalInterval)
		{
			_dtSinceLastOccasional = EvalInterval - MinReevalInterval;
		}
	}

	public WizardAIComponent(Agent agent)
		: base(agent)
	{
		_dtSinceLastOccasional = (float)(agent.Index % 30) / 30f * 3f;
		foreach (HumanAIComponent item in (from c in agent.Components.OfType<HumanAIComponent>()
			where c != this
			select c).ToList())
		{
			agent.RemoveComponent(item);
		}
	}

	public override void OnTick(float dt)
	{
		if (!Agent.IsPaused)
		{
			_dtSinceLastOccasional += dt;
			if (_dtSinceLastOccasional >= EvalInterval)
			{
				TickOccasionally();
			}
			FiringOrder? firingOrder = Agent?.Formation?.FiringOrder;
			if (firingOrder.HasValue && firingOrder.Value.OrderType == OrderType.HoldFire)
			{
				CurrentCastingBehavior?.Terminate();
				CurrentCastingBehavior?.TacticalBehavior?.Terminate();
			}
			else
			{
				CurrentCastingBehavior?.TacticalBehavior?.Execute();
				CurrentCastingBehavior?.Execute();
			}
		}
	}

	private void TickOccasionally()
	{
		_dtSinceLastOccasional = 0f;
		CurrentCastingBehavior = DetermineBehavior(AvailableCastingBehaviors, CurrentCastingBehavior);
	}

	private AbstractAgentCastingBehavior DetermineBehavior(List<IAgentBehavior> available, AbstractAgentCastingBehavior current)
	{
		BehaviorOption behaviorOption = DecisionManager.EvaluateCastingBehaviors(available);
		if (behaviorOption == null)
		{
			return current;
		}
		if (behaviorOption.Behavior != current)
		{
			current?.Terminate();
			current?.TacticalBehavior?.Terminate();
		}
		if (behaviorOption.Behavior is AbstractAgentCastingBehavior abstractAgentCastingBehavior)
		{
			abstractAgentCastingBehavior.CurrentTarget = behaviorOption.Target;
			return abstractAgentCastingBehavior;
		}
		return null;
	}
}
