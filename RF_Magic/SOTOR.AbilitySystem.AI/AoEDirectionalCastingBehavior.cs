using SOTOR.Extensions;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class AoEDirectionalCastingBehavior : AbstractAgentCastingBehavior
{
	public AoEDirectionalCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex)
		: base(agent, template, abilityIndex)
	{
		Hysteresis = 0.35f;
		base.TacticalBehavior = new DirectionalAoETacticalBehavior(agent, agent.GetComponent<WizardAIComponent>(), this);
	}

	public override void Execute()
	{
		Vec3? vec = (base.TacticalBehavior as DirectionalAoETacticalBehavior)?.CastingPosition;
		if (!vec.HasValue || !(Agent.Position.AsVec2.Distance(vec.Value.AsVec2) > 6f))
		{
			base.Execute();
		}
	}

	protected override float CalculateUtility(Target target)
	{
		if (CommonAIStateFunctions.CanAgentMoveFreely(Agent))
		{
			Ability ability = Agent.GetAbility(AbilityIndex);
			if (ability != null && !ability.IsOnCooldown())
			{
				return base.CalculateUtility(target);
			}
		}
		return 0f;
	}
}
