using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class SelectSingleTargetCastingBehavior : AoETargetedCastingBehavior
{
	public SelectSingleTargetCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex)
		: base(agent, template, abilityIndex)
	{
		Hysteresis = 0.1f;
	}

	protected override Target UpdateTarget(Target target)
	{
		if (AbilityTemplate.AbilityTargetType == AbilityTargetType.Self)
		{
			target.Agent = Agent;
			return target;
		}
		return base.UpdateTarget(target);
	}
}
