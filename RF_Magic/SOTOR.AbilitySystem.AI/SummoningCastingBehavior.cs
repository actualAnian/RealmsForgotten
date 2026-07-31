using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class SummoningCastingBehavior : AbstractAgentCastingBehavior
{
	public SummoningCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex)
		: base(agent, template, abilityIndex)
	{
		Hysteresis = 0.1f;
	}

	protected override Target UpdateTarget(Target target)
	{
		target.SelectedWorldPosition = Agent.Position + Agent.LookDirection * 2f;
		return target;
	}
}
