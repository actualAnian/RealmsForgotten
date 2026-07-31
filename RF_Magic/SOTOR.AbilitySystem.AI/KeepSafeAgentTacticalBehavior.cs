using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class KeepSafeAgentTacticalBehavior : AbstractAgentTacticalBehavior
{
	public KeepSafeAgentTacticalBehavior(Agent agent, HumanAIComponent aiComponent)
		: base(agent, aiComponent)
	{
	}

	public override void Tick()
	{
		BehaviorComponent behaviorComponent = Agent.Formation?.AI?.ActiveBehavior;
		if (Agent.Team != null && Agent.Team.GeneralAgent == Agent && Agent.Team.HasTeamAi && behaviorComponent != null && behaviorComponent.GetType() == typeof(BehaviorCharge))
		{
			Agent.Formation.AI.SetBehaviorWeight<BehaviorCharge>(0f);
		}
	}

	public override void Terminate()
	{
	}

	public override void ApplyBehaviorParams()
	{
		OrderType? movementOrderType = GetMovementOrderType();
		if (!movementOrderType.HasValue || (movementOrderType.Value != OrderType.FollowMe && movementOrderType.Value != OrderType.FollowEntity))
		{
			AIComponent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.DefaultDetached);
		}
	}

	public override void SetCurrentTarget(Target target)
	{
	}
}
