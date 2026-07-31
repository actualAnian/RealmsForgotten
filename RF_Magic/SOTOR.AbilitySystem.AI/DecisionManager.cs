using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace SOTOR.AbilitySystem.AI;

public static class DecisionManager
{
	public static BehaviorOption EvaluateCastingBehaviors(List<IAgentBehavior> behaviors)
	{
		return behaviors.SelectMany((IAgentBehavior behavior) => behavior.CalculateUtility()).MaxBy((BehaviorOption option) => option.Target.UtilityValue);
	}
}
