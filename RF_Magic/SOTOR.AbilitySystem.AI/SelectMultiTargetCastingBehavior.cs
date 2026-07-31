using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class SelectMultiTargetCastingBehavior : AoETargetedCastingBehavior
{
	public SelectMultiTargetCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex)
		: base(agent, template, abilityIndex)
	{
		Hysteresis = 0.1f;
	}
}
