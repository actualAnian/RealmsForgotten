using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class AoETargetedCastingBehavior : MissileCastingBehavior
{
	public AoETargetedCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex)
		: base(agent, template, abilityIndex)
	{
	}

	protected override bool HaveLineOfSightToTarget(Target target)
	{
		AbilityEffectType abilityEffectType = AbilityTemplate.AbilityEffectType;
		if (abilityEffectType == AbilityEffectType.Vortex || abilityEffectType == AbilityEffectType.Heal || abilityEffectType == AbilityEffectType.Augment || abilityEffectType == AbilityEffectType.Hex || abilityEffectType == AbilityEffectType.Bombardment)
		{
			Vec3 positionPrioritizeCalculated = target.GetPositionPrioritizeCalculated();
			if (positionPrioritizeCalculated == Vec3.Invalid)
			{
				return false;
			}
			float num = Agent.Position.Distance(positionPrioritizeCalculated);
			if (num >= AbilityTemplate.MinDistance)
			{
				return num <= AbilityTemplate.MaxDistance;
			}
			return false;
		}
		return base.HaveLineOfSightToTarget(target);
	}
}
