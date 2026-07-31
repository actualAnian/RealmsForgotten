using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public abstract class AbstractAgentTacticalBehavior : IAgentBehavior
{
	protected HumanAIComponent AIComponent;

	protected Agent Agent;

	protected AbstractAgentTacticalBehavior(Agent agent, HumanAIComponent aiComponent)
	{
		Agent = agent;
		AIComponent = aiComponent;
	}

	protected OrderType? GetMovementOrderType()
	{
		Formation formation = Agent?.Formation;
		if (formation == null)
		{
			return null;
		}
		return formation.GetReadonlyMovementOrderReference().OrderType;
	}

	public void Execute()
	{
		ApplyBehaviorParams();
		Tick();
	}

	public abstract void Tick();

	public abstract void Terminate();

	public abstract void ApplyBehaviorParams();

	public abstract void SetCurrentTarget(Target target);

	public List<BehaviorOption> CalculateUtility()
	{
		return new List<BehaviorOption>();
	}
}
