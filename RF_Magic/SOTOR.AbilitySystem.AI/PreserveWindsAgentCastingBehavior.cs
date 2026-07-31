using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class PreserveWindsAgentCastingBehavior : AbstractAgentCastingBehavior
{
	private List<Axis> _axisListForPreserve => AgentCastingBehaviorConfiguration.UtilityByType[typeof(PreserveWindsAgentCastingBehavior)](this);

	public PreserveWindsAgentCastingBehavior(Agent agent, AbilityTemplate abilityTemplate, int abilityIndex)
		: base(agent, abilityTemplate, abilityIndex)
	{
	}

	public override void Execute()
	{
	}

	protected override float CalculateUtility(Target target)
	{
		if (target.Formation == null && target.TacticalPosition == null)
		{
			return 0f;
		}
		return _axisListForPreserve.GeometricMean(target);
	}
}
