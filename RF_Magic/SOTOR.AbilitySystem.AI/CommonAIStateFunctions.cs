using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public static class CommonAIStateFunctions
{
	public static bool CanAgentMoveFreely(Agent agent)
	{
		Formation formation = agent?.Formation;
		if (formation == null)
		{
			return false;
		}
		OrderType orderType = formation.GetReadonlyMovementOrderReference().OrderType;
		if (orderType == OrderType.Charge || orderType == OrderType.ChargeWithTarget)
		{
			return true;
		}
		return (formation.AI?.ActiveBehavior)?.GetType().Name.Contains("Skirmish") ?? false;
	}
}
